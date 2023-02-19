﻿using AuroraLip.Common;
using AuroraLip.Compression;
using System;
using System.IO;

namespace NSMBWCompression
{
    /// <summary>
    /// LH compression algorithm base on (LZ77 + Huffman)
    /// Used in Mario Sports Mix and Newer Super Mario Bros
    /// </summary>
    public class LH : ICompression
    {
        public bool CanRead => false;

        public bool CanWrite => false;

        public bool IsMatch(Stream stream, in string extension = "")
        {
            return stream.ReadUInt8() == 64;
        }

        public byte[] Compress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public byte[] Decompress(in byte[] Data)
        {
            throw new NotImplementedException();
        }
    }
}