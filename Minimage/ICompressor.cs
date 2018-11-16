using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Minimage
{
    public interface ICompressor
    {
        Task<byte[]> Compress(byte[] input);

        string[] MimeTypes { get; } 
    }
}
