using System;
using System.Collections.Generic;
using System.IO;
using AuroraLip.Common;

namespace AuroraLip.Compression.Formats
{
    /// <summary>
    /// LZSS implementation as in Pokemon XD Gale of Darkness, which uses a different magic value.
    /// </summary>
    public class GSLZSS : LZSS
    {
        public override string Magic { get; } = "LZSS";

        public override bool IsMatch(Stream stream, in string extension = "")
        {
            return stream.MatchString(Magic);
        }

        public override byte[] Decompress(in byte[] Data)
        {
            if (!IsMatch(in Data))
                throw new InvalidIdentifierException();

            uint decompressedSize = ((uint)Data[4] << 24) | ((uint)Data[5] << 16) | ((uint)Data[6] << 8) | (uint)Data[7];
            uint compressedSize = ((uint)Data[8] << 24) | ((uint)Data[9] << 16) | ((uint)Data[10] << 8) | (uint)Data[11];
            if (Data.Length != compressedSize)
                throw new Exception("compressed size mismatch");

            List<byte> outdata = DecompressRaw(Data, 0x10);

            if (decompressedSize != outdata.Count)
                throw new Exception($"Size mismatch: got {outdata.Count} bytes after decompression, expected {decompressedSize}.\n");

            return outdata.ToArray();
        }
    }
}
