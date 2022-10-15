using AuroraLip.Common;
using System;
using System.IO;

namespace AuroraLip.Archives.Formats
{
    // Genius Senority (Pokemon XD Gale of Darkness) archive format
    public class GSFSYS : Archive, IMagicIdentify, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public string Magic => magic;

        private const string magic = "FSYS";

        public GSFSYS() { }

        public GSFSYS(string filename) : base(filename) { }

        public GSFSYS(Stream stream, string filename = null) : base(stream, filename) { }

        public bool IsMatch(Stream stream, in string extension = "")
            => stream.MatchString(magic);

        protected override void Read(Stream stream)
        {
            if (!stream.MatchString(magic))
                throw new InvalidIdentifierException(Magic);

            stream.Seek(0x4, SeekOrigin.Begin);
            uint header_unknown_04 = stream.ReadUInt32(Endian.Big);
            uint fsysid = stream.ReadUInt32(Endian.Big);
            uint num_files = stream.ReadUInt32(Endian.Big);

            stream.Seek(0x40, SeekOrigin.Begin);
            uint file_entries_offset = stream.ReadUInt32(Endian.Big);

            Root = new ArchiveDirectory() { OwnerArchive = this };

            for (uint i = 0; i < num_files; i++)
            {
                stream.Seek(file_entries_offset + i * 0x04, SeekOrigin.Begin);
                uint file_entry_offset = stream.ReadUInt32(Endian.Big);
                stream.Seek(file_entry_offset, SeekOrigin.Begin);

                uint file_id = stream.ReadUInt32(Endian.Big);
                uint contents_offset = stream.ReadUInt32(Endian.Big);
                uint size = stream.ReadUInt32(Endian.Big);
                uint flags = stream.ReadUInt32(Endian.Big);
                bool is_compressed = (flags & 0x80000000) != 0;
                uint flags2 = stream.ReadUInt32(Endian.Big);
                uint compressed_size = stream.ReadUInt32(Endian.Big); // If not compressed, equal to size
                uint file_unknown_18 = stream.ReadUInt32(Endian.Big);
                uint file_name_offset = stream.ReadUInt32(Endian.Big); // Internal original filename (with extension), may be 0
                uint filetype = stream.ReadUInt32(Endian.Big);
                uint name_offset = stream.ReadUInt32(Endian.Big); // Some name, usually shared between files for one thing, may be (null) or 0

                string combined_filename = i.ToString();

                bool try_file_name = name_offset == 0;
                if (name_offset != 0)
                {
                    stream.Seek(name_offset, SeekOrigin.Begin);
                    string name = stream.ReadString();
                    if (name != "(null)")
                        combined_filename += '_' + name;
                    else
                        try_file_name = true;
                }

                if (try_file_name && file_name_offset != 0)
                {
                    stream.Seek(file_name_offset, SeekOrigin.Begin);
                    string name = stream.ReadString();
                    combined_filename += '_' + name;
                }

                combined_filename += getExtensionByType(filetype);
                if (is_compressed)
                    combined_filename += ".gslzss";

                ArchiveFile Sub = new ArchiveFile() { Parent = Root, Name = combined_filename };

                stream.Seek(contents_offset, SeekOrigin.Begin);
                Sub.FileData = new SubStream(stream, compressed_size);
                Root.Items.Add(Sub.Name, Sub);
            }
        }

        protected override void Write(Stream ArchiveFile)
        {
            throw new NotImplementedException();
        }

        private String getExtensionByType(uint filetype)
        {
            if (filetype == 0x01) // Map/"Floor"
                return ".gsscene";
            if (filetype == 0x02) // Model
                return ".gsscene";
            if (filetype == 0x09)
                return ".gtx";
            if (filetype == 0x0F)
                return ".pkx";
            return $".{filetype}.bin";
        }
    }
}
