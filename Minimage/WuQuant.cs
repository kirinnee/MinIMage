using MimeDetective;
using nQuant;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Minimage
{
    public class WuQuant : Compressor
    {
        public WuQuant():base ( new string[] {"image/png", "image/jpg" })
        {

        }

        public override Task<byte[]> CompressImplementation(byte[] stream)
        {
            ImageFormat saveFormat = stream.GetFileType().Mime == "image/png" ? ImageFormat.Png : ImageFormat.Jpeg;

            var quantizer = new WuQuantizer();
            Bitmap bmp;
            using (var ms = new MemoryStream(stream))
            {
                bmp = new Bitmap(ms);
                using (var quantized = quantizer.QuantizeImage(bmp, 10, 70, null, 256))
                {
                    using (var outStream = new MemoryStream())
                    {
                        quantized.Save(outStream, saveFormat);
                        return Task.FromResult(outStream.ToArray());
                    }
                }
            }
        }
    }
}
