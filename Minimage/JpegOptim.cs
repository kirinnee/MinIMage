using Medallion.Shell;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Minimage
{
    public class JpegOptimOptions
    {
        public int Quality = 75;
        public bool StripAll = true;
        public bool Overwrite = false;
        public bool Progressive = true;
        public bool Force = true;


        public string[] AsArgument()
        {
            string stripAll = StripAll ? "-s " : "";
            string overwrite = Overwrite ? "-o " : "";
            string quality = $"--max={Quality} ";
            string progress = Progressive ? "--all-progressive " : "--all-normal ";
            string force = Force ? "-f " : "";
            return (new string[] {
                "--stdin",
                "--stdout",
                stripAll,
                overwrite,
                quality,
                progress,
                force
            })
            .Where(s => s.Trim() != "")
            .Where(s => s != null)
            .ToArray();
            // {force}{progress}{quality}{overwrite}{stripAll}
        }
    }

    public class JpegOptim : Compressor
    {
        private readonly string[] _options;
        public JpegOptim(JpegOptimOptions options = null) : base(new string[] { "image/jpeg" })
        {
            options = options ?? new JpegOptimOptions();
            _options = options.AsArgument();
        }

        public override async Task<byte[]> CompressImplementation(byte[] input)
        {

            using (MemoryStream ms = new MemoryStream(input), output = new MemoryStream())
            {
                var command = Command.Run("jpegoptim", _options) < ms > output;
                await command.Task.ConfigureAwait(false);   
                return output.ToArray();
            }



        }

    }
}
