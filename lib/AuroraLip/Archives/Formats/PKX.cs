using AuroraLip.Common;
using System;
using System.IO;

namespace AuroraLip.Archives.Formats
{
    // Genius Senority (Pokémon XD Gale of Darkness) PKX file (pokémon and some models?)
    public class PKX : Archive, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public PKX() { }

        public PKX(string filename) : base(filename) { }

        public PKX(Stream stream, string filename = null) : base(stream, filename) { }

        public bool IsMatch(Stream stream, in string extension = "")
            => extension.ToLower() == ".pkx";

        protected override void Read(Stream stream)
        {
            uint archive_size = stream.ReadUInt32(Endian.Big);
            uint header_04 = stream.ReadUInt32(Endian.Big);
            uint header_08 = stream.ReadUInt32(Endian.Big);
            uint header_0c = stream.ReadUInt32(Endian.Big);
            uint n_entries = stream.ReadUInt32(Endian.Big); // Unknown what they are
            uint header_14 = stream.ReadUInt32(Endian.Big);
            uint header_18 = stream.ReadUInt16(Endian.Big);
            uint header_1a = stream.ReadUInt16(Endian.Big);

            if (header_1a == 0x0c)
            {
                uint archive_begin = 0x84 + n_entries * 208;
                archive_begin = (archive_begin + 31) & ~(uint)31; // Round to next 32-byte boundary
                archive_begin = (archive_begin + header_08 + 31) & ~(uint)31;

                Root = new ArchiveDirectory() { OwnerArchive = this };
                ArchiveFile Sub = new ArchiveFile() { Parent = Root, Name = "thing.gsscene" };
                stream.Seek(archive_begin, SeekOrigin.Begin);
                Sub.FileData = new SubStream(stream, archive_size);
                Root.Items.Add(Sub.Name, Sub);
            }
            else
            {
                throw new NotImplementedException($"Unknown header value {header_1a}");
            }
        }

        protected override void Write(Stream ArchiveFile)
        {
            throw new NotImplementedException();
        }

    }
}
