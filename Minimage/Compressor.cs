using MimeDetective;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimage
{
    public abstract class Compressor
    {
        public string[] MimeTypes { get; } 

        protected Compressor(string[] mimeTypes)
        {
            MimeTypes = mimeTypes;
        }


        public Task<byte[]> Compress(byte[] stream)
        {
            if (!MimeTypes.Contains(stream.GetFileType().Mime)) return Task.FromResult(stream);
            return CompressImplementation(stream);
        }

        public abstract Task<byte[]> CompressImplementation(byte[] stream);

    }
}
