using Minimage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestConsole
{

    public class FilePairStage1
    {
        internal string From;
        internal string To;

        public FilePairStage1(string from, string to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        internal async Task<FilePairStage2> Read()
        {
            byte[] bytes = await File.ReadAllBytesAsync(From);
            return new FilePairStage2()
            {
                From = bytes,
                To = To
            };
        }

    }

    public class FilePairStage2
    {
        internal byte[] From;
        internal string To;


        internal async Task<FilePairStage3> Transform(Compressor com)
        {
            byte[] bytes = await com.Compress(From);
            return new FilePairStage3()
            {
                From = From,
                ToPath = To,
                To = bytes
            };
        }
    }

    public class FilePairStage3
    {
        internal byte[] From;
        internal byte[] To;
        internal string ToPath;

        internal Task Write()
        {
            return File.WriteAllBytesAsync(ToPath, To);
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine(args);
            AsyncMain(null).GetAwaiter().GetResult();
        }

        private static async Task AsyncMain(string[] args)
        {
            string from = "assets";
            string to = "compressed";

            Compressor pngQuant = new PngQuant( new PngQuantOptions()
            {
                QualityMinMax = (5,10)
            });
            Stopwatch sw = new Stopwatch();

            IEnumerable<Task<FilePairStage2>> files = Directory.GetFiles(Path.Combine(from))
                .AsParallel()
                .Select(s => new FilePairStage1(s, Path.Combine(to, Path.GetRelativePath(from, s))))
                .Select(s => s.Read());
            FilePairStage2[] stage2s = await Task.WhenAll(files);
            sw.Start();
            IEnumerable<Task<FilePairStage3>> bytes = stage2s.AsParallel().Select(s => s.Transform(pngQuant));
            FilePairStage3[] stage3s = await Task.WhenAll(bytes);

            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            IEnumerable<Task> tasks = stage3s.AsParallel().Select(s => s.Write());
            await Task.WhenAll(tasks);
            Console.WriteLine("DONE!");

            sw.Stop();


            Console.ReadLine();

            await AsyncMain(args);
        }

        private static async Task<byte[]> ReadStdin()
        {
            using (var stream = new MemoryStream())
            {
                Stream input = Console.OpenStandardInput();
                await input.CopyToAsync(stream);
                input.Close();
                return stream.ToArray();
            }
        }

        private static async Task WriteStdout(byte[] output)
        {
            using (var stream = new MemoryStream(output))
            {
                var stdout = Console.OpenStandardOutput();
                await stream.CopyToAsync(stdout);
                stream.Close();
            }
        }
    }
}
