using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Minimage
{

    public class PngQuantOptions
    {
       public Tuple<int, int> MinMax = null;
           
    }

    public class PngQuant
    {
        public string[] MimeTypes { get; } = new string[] { "image/png" };
        private readonly ProcessStartInfo _info;
        private readonly Process pro;

        public PngQuant()
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                FileName = "pngquant",
                Arguments = "256 --speed 11",
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            _info = info;
            pro = Process.Start(info);
        }

        public async Task<byte[]> Compress(byte[] input)
        {
           
            using (MemoryStream outputStream = new MemoryStream())
            {
                await pro.StandardInput.BaseStream.WriteAsync(input, 0, input.Length);
                await pro.StandardInput.BaseStream.FlushAsync();
                await pro.StandardOutput.BaseStream.CopyToAsync(outputStream);
                await pro.StandardOutput.BaseStream.FlushAsync();
                byte[] output = outputStream.ToArray();
                return output;
            }
            
        }
    }
}
