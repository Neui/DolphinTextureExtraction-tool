using AuroraLip.Common;
using System;
using System.IO;
using static AuroraLip.Texture.J3D.JUtility;

namespace AuroraLip.Texture.Formats
{
    // Pokemon XD Gale of Darkness (Genius Sonority) texture format
    public class GTX : JUTTexture, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public GTX() { }

        public GTX(Stream stream) : base(stream) { }

        public GTX(string filepath) : base(filepath) { }

        public bool IsMatch(Stream stream, in string extension = "")
        {
            return extension.EndsWith("gtx", StringComparison.OrdinalIgnoreCase);
        }

        protected override void Read(Stream stream)
        {
            uint width = stream.ReadUInt16(Endian.Big);
            uint height = stream.ReadUInt16(Endian.Big);
            uint header_04 = stream.ReadUInt8();
            uint num_entries = stream.ReadUInt8(); // TODO: But only the first one is always used? Mipmaps?
            uint header_06 = stream.ReadUInt8();
            uint header_07 = stream.ReadUInt8();
            uint raw_tex_format = stream.ReadUInt32(Endian.Big);
            GXImageFormat format = RawToGXImageFormat(raw_tex_format);
            uint raw_tlut_format = stream.ReadUInt32(Endian.Big);
            uint wrap_s = stream.ReadUInt32(Endian.Big); // TODO
            uint wrap_t = stream.ReadUInt32(Endian.Big); // TODO
            uint header_18 = stream.ReadUInt32(Endian.Big);
            uint mag_filter = stream.ReadUInt32(Endian.Big); // TODO: Figure out how to use it
            uint min_filter = stream.ReadUInt32(Endian.Big); // TODO: Figure out how to use it
            uint header_24 = stream.ReadUInt32(Endian.Big);
            uint image_offset = stream.ReadUInt32(Endian.Big);

            stream.Seek(0x48, SeekOrigin.Begin);
            uint tlut_offset = stream.ReadUInt32(Endian.Big);

            byte[] palette_data = null;
            GXPaletteFormat palette_format = GXPaletteFormat.IA8;
            uint num_colours = 0;

            if (tlut_offset != 0)
            {
                if (raw_tex_format == 0)
                    num_colours = 0x10;
                else if (raw_tex_format == 1)
                    num_colours = 0x100;
                else if (raw_tex_format == 30)
                    num_colours = 0x400;
                else
                    throw new Exception($"Invalid GTX texture format {raw_tex_format} to be used with a palette");

                palette_format = RawToGXPaletteFormat(raw_tlut_format);

                stream.Seek(tlut_offset, SeekOrigin.Begin);
                palette_data = new byte[2 * num_colours];
                stream.Read(palette_data, 0, palette_data.Length);
            }

            stream.Seek(image_offset, SeekOrigin.Begin);
            TexEntry current = new TexEntry(stream, palette_data, format, palette_format, (int)num_colours, (int)width, (int)height, 0)
            {
                LODBias = 0,
                MagnificationFilter = GXFilterMode.Nearest,
                MinificationFilter = GXFilterMode.Nearest,
                WrapS = (GXWrapMode)wrap_s,
                WrapT = (GXWrapMode)wrap_t,
                EnableEdgeLOD = false,
                MinLOD = 0,
                MaxLOD = 0
            };
            Add(current);
        }

        protected override void Write(Stream stream)
        {
            throw new NotImplementedException();
        }

        private GXImageFormat RawToGXImageFormat(uint raw)
        {
            if (raw == 0x00)
                return GXImageFormat.C4;
            if (raw == 0x01)
                return GXImageFormat.C8;
            if (raw == 0x30)
                return GXImageFormat.C14X2;
            if (raw == 0x40)
                return GXImageFormat.I4;
            if (raw == 0x41)
                return GXImageFormat.IA4;
            if (raw == 0x42)
                return GXImageFormat.I8;
            if (raw == 0x43)
                return GXImageFormat.IA8;
            if (raw == 0x44)
                return GXImageFormat.RGB565;
            if (raw == 0x45)
                return GXImageFormat.RGBA32;
            if (raw == 0x90)
                return GXImageFormat.RGB5A3;
            if (raw == 0xa0 || raw == 0xa1 || raw == 0xa2 || raw == 0xa3)
                return GXImageFormat.I8;
            if (raw == 0xb0)
                return GXImageFormat.CMPR;
            if (raw == 0x90)
                return GXImageFormat.RGB5A3;
            throw new Exception($"Unknown GTX texture format: {raw}");
        }

        private GXPaletteFormat RawToGXPaletteFormat(uint raw)
        {
            if (raw == 0x01)
                return GXPaletteFormat.IA8;
            if (raw == 0x02)
                return GXPaletteFormat.RGB565;
            if (raw == 0x03)
                return GXPaletteFormat.RGB5A3;
            throw new Exception($"Unknown GTX palette format: {raw}");
        }
    }
}
