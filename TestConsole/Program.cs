using Minimage;
using System;
using System.Collections;
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
        

        internal async Task<FilePairStage3> Transform(PngQuant com)
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

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args);
            AsyncMain(null).GetAwaiter().GetResult();
        }

        static async Task AsyncMain(string[] args)
        {

            PngQuant pngQuant = new PngQuant();
            Stopwatch sw = new Stopwatch();
            byte[] files = File.ReadAllBytes("assets/amazed.png");
            Parallel.For(0,1, i => {
                for (int i2 = 0; i2 < 1; i2++)
                {
                    byte[] transformed = pngQuant.CompressTest(files, i*16+1, i2*16+1);
                    File.WriteAllBytesAsync($"test/{i}-{i2}.png", transformed);
                }
            });

            Console.WriteLine("Elapsed={0}", sw.Elapsed);
            Console.WriteLine("DONE!");
            Console.ReadLine();
            await AsyncMain(args);
        }

        static async Task AsyncMain2(string[] args) 
        {
            string from = "assets";
            string to = "compressed";

            PngQuant pngQuant = new PngQuant();
            Stopwatch sw = new Stopwatch();

            IEnumerable<Task<FilePairStage2>> files = Directory.GetFiles(Path.Combine(from))
                .AsParallel()
                .Select( s=> new FilePairStage1(s, Path.Combine(to,Path.GetRelativePath(from,s))))
                .Select( s => s.Read());
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

        static async Task<byte[]> ReadStdin()
        {
            using(var stream = new MemoryStream())
            {
                Stream input = Console.OpenStandardInput();
                await input.CopyToAsync(stream);
                input.Close();
                return stream.ToArray();
            }
        }

        static async Task WriteStdout(byte[] output)
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
