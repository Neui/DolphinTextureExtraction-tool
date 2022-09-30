using System;
using System.Collections.Generic;
using System.IO;
using AuroraLip.Common;

namespace AuroraLip.Compression.Formats
{

    /*
     * xdanieldzd & lue
     * Library for Decompressing LZSS files
     * https://github.com/xdanieldzd/N3DSCmbViewer/blob/master/N3DSCmbViewer/LZSS.cs
     * https://github.com/lue/MM3D/blob/master/src/lzs.cpp
     */

    /// <summary>
    /// LZSS Lempel–Ziv–Storer–Szymanski algorithm, a derivative of LZ77.
    /// </summary>
    public class LZSS : ICompression, IMagicIdentify
    {

        public virtual string Magic { get; } = "LzS";

        public int MagicOffset { get; } = 0;

        public bool CanWrite { get; } = false;

        public bool CanRead { get; } = true;

        public virtual byte[] Compress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] Decompress(in byte[] Data)
        {
            uint decompressedSize;

            if (IsMatch(in Data))
            {
                //string tag = Encoding.ASCII.GetString(Data, 0, 4);
                //uint unknown = BitConverter.ToUInt32(Data, 4);
                decompressedSize = BitConverter.ToUInt32(Data, 8);
                uint compressedSize = BitConverter.ToUInt32(Data, 12);
                if (Data.Length != compressedSize + 0x10) throw new Exception("compressed size mismatch");
            }
            else
            {
                decompressedSize = BitConverter.ToUInt32(Data, 0);
            }

            List<byte> outdata = DecompressRaw(Data, (ushort)(IsMatch(in Data) ? 16 : 4));

            if (decompressedSize != outdata.Count)
                throw new Exception($"Size mismatch: got {outdata.Count} bytes after decompression, expected {decompressedSize}.\n");

            return outdata.ToArray();
        }

        protected List<byte> DecompressRaw(in byte[] Data, ushort readidx)
        {
            List<byte> outdata = new List<byte>();
            byte[] BUFFER = new byte[4096];

            for (int i = 0; i < BUFFER.Length; i++) BUFFER[i] = 0;
            byte flags8 = 0;
            ushort writeidx = 0xFEE;
            uint fidx = 0x10;

            while (fidx < Data.Length)
            {
                flags8 = Data[fidx];
                fidx++;

                for (int i = 0; i < 8; i++)
                {
                    if ((flags8 & 1) != 0)
                    {
                        outdata.Add(Data[fidx]);
                        BUFFER[writeidx] = Data[fidx];
                        writeidx++; writeidx %= 4096;
                        fidx++;
                    }
                    else
                    {
                        readidx = Data[fidx];
                        fidx++;
                        readidx |= (ushort)((Data[fidx] & 0xF0) << 4);
                        for (int j = 0; j < (Data[fidx] & 0x0F) + 3; j++)
                        {
                            outdata.Add(BUFFER[readidx]);
                            BUFFER[writeidx] = BUFFER[readidx];
                            readidx++; readidx %= 4096;
                            writeidx++; writeidx %= 4096;
                        }
                        fidx++;
                    }
                    flags8 >>= 1;
                    if (fidx >= Data.Length) break;
                }
            }
            return outdata;
        }

        protected virtual bool IsMatch(in byte[] Data)
        {
            if (Data.Length <= 16)
                return false;

            uint i = 0;
            foreach (char c in Magic)
            {
                if (Data[i] != ((byte)c))
                    return false;
                i++;
            }

            return true;
        }

        public virtual bool IsMatch(Stream stream, in string extension = "")
        {
            if (stream.Length > 16 && stream.MatchString(Magic))
{
                stream.Position = 12;
                // compressed size match?
                uint compressedSize = BitConverter.ToUInt32(stream.Read(4), 0);
                return stream.Length == compressedSize + 0x10;

            }
            return false;

        }
    }
}
