using MimeDetective;
using PnnQuant;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Minimage
{
    public class nQuant : Compressor
    {

        public nQuant():base (new string[] {"image/png", "image/jpeg" })
        {

        }

        public override Task<byte[]> CompressImplementation(byte[] stream)
        {
            ImageFormat saveFormat = stream.GetFileType().Mime == "image/png" ? ImageFormat.Png : ImageFormat.Jpeg;
            PnnQuantizer quantizer = new PnnLABQuantizer();
            Bitmap bitmap;
            using (var ms = new MemoryStream(stream))
            {
                bitmap = new Bitmap(ms);
                using (var dest = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format8bppIndexed))
                {
                    quantizer.QuantizeImage(bitmap, dest, 256, true);
                    using (var outStream = new MemoryStream())
                    {
                        dest.Save(outStream, saveFormat);
                        return Task.FromResult(outStream.ToArray());
                    }

                }

            }
        }
    }
}
