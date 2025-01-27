﻿using AuroraLip.Common;
using AuroraLip.Texture.J3D;
using System;
using System.IO;
using static AuroraLip.Texture.J3D.JUtility;

namespace AuroraLip.Texture.Formats
{
    public class GCT0 : JUTTexture, IMagicIdentify, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public string Magic => magic;

        private const string magic = "GCT0";

        public bool IsMatch(Stream stream, in string extension = "")
            => stream.MatchString(magic);

        protected override void Read(Stream stream)
        {
            if (!stream.MatchString(magic))
                throw new InvalidIdentifierException(Magic);

            GXImageFormat Format = (GXImageFormat)stream.ReadUInt32(Endian.Big);
            ushort Width = stream.ReadUInt16(Endian.Big);
            ushort Height = stream.ReadUInt16(Endian.Big);
            byte unkflag = (byte)stream.ReadByte(); //Flag?
            _ = stream.ReadInt24(Endian.Big);
            uint ImgOffset = stream.ReadUInt32(Endian.Big);
            _ = stream.ReadUInt64(Endian.Big);
            ushort unkmip = stream.ReadUInt16(Endian.Big); //mips?
            _ = stream.ReadUInt16(Endian.Big);
            uint unknown = stream.ReadUInt32(Endian.Big); //202

            // we calculate the mips
            int mips = JUtility.GetMipmapsFromSize(Format, (int)(stream.Length - ImgOffset), Width, Height);

            // Palette are not supported?
            if (JUtility.IsPaletteFormat(Format))
            {
                throw new PaletteException($"{nameof(GCT0)} does not support palette formats.");
            }

            stream.Seek(ImgOffset, SeekOrigin.Begin);
            Add(new TexEntry(stream, null, Format, GXPaletteFormat.IA8, 0, Width, Height, mips)
            {
                LODBias = 0,
                MagnificationFilter = GXFilterMode.Nearest,
                MinificationFilter = GXFilterMode.Nearest,
                WrapS = GXWrapMode.CLAMP,
                WrapT = GXWrapMode.CLAMP,
                EnableEdgeLOD = false,
                MinLOD = 0,
                MaxLOD = mips
            });
        }

        protected override void Write(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
