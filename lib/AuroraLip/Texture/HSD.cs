using AuroraLip.Common;
using AuroraLip.Texture.J3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using static AuroraLip.Texture.J3D.JUtility;

namespace AuroraLip.Texture.Formats
{
    /// <summary>
    /// Extract texture from an HSD archive and scene.
    /// This seems to come from a middleware called sysdolphin (HAL Laboratory), but the "Wrapper" format for the scene is called HSDArchive.
    /// </summary>
    /// <see href="https://wiki.raregamingdump.ca/index.php/sysdolphin"/>
    /// <see href="https://wiki.tockdom.com/wiki/HAL_DAT_(File_Format)"/>
    /// <see href="https://github.com/doldecomp/melee/blob/master/include/sysdolphin/baselib/"/>
    public abstract class HSD : JUTTexture, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public abstract bool IsMatch(Stream stream, in string extension = "");

        protected override void Read(Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Class for dealing with HSDArchives.
        /// Basically it contains a (big) file and pointers into that file for possibly various stuff such as the "entry point".
        /// Also contains relocation info because it contains pointers, that this implementation ignores because
        /// by default it is offset from the file, which is already "correct".
        /// </summary>
        public class ArchiveInfo
        {
            public Stream DataStream = null;
            public Dictionary<string, uint> PublicSymbols = new Dictionary<string, uint>();
            public Dictionary<string, uint> ExternSymbols = new Dictionary<string, uint>();
            public List<uint> Relocations = new List<uint>();
            public uint version;

            public virtual void Read(Stream stream)
            {
                uint file_size = stream.ReadUInt32(Endian.Big);
                uint data_size = stream.ReadUInt32(Endian.Big);
                uint nb_reloc = stream.ReadUInt32(Endian.Big);
                uint nb_public = stream.ReadUInt32(Endian.Big);
                uint nb_extern = stream.ReadUInt32(Endian.Big);
                uint version = stream.ReadUInt32(Endian.Big);
                uint padding_1 = stream.ReadUInt32(Endian.Big);
                uint padding_2 = stream.ReadUInt32(Endian.Big);

                if (file_size != stream.Length)
                {
                    throw new Exception($"Expected filesize {file_size} does not match the given filesize {stream.Length}");
                }

                uint data_offset = 0x20;
                uint relocation_offset = data_offset + data_size;
                uint public_offset = relocation_offset + nb_reloc * 4;
                uint extern_offset = public_offset + nb_public * 8;
                uint symbols_offset = extern_offset + nb_extern * 8;

                DataStream = new SubStream(stream, data_size);

                stream.Seek(data_size, SeekOrigin.Current);

                if (stream.Position != relocation_offset)
                    throw new Exception("Internal error, expected relocation position isn't what we have calculated before");

                for (int i = 0; i < nb_reloc; ++i)
                {
                    Relocations.Add(stream.ReadUInt32(Endian.Big));
                }

                if (stream.Position != public_offset)
                    throw new Exception("Internal error, expected public symbols position isn't what we have calculated before");

                for (int i = 0; i < nb_public; ++i)
                {
                    ReadSymbol(stream, PublicSymbols, symbols_offset);
                }

                if (stream.Position != extern_offset)
                    throw new Exception("Internal error, expected extern symbols position isn't what we have calculated before");

                for (int i = 0; i < nb_extern; ++i)
                {
                    ReadSymbol(stream, ExternSymbols, symbols_offset);
                }
            }

            protected void ReadSymbol(Stream stream, Dictionary<string, uint> dict, uint base_symbol_name_offset)
            {
                uint data_offset = stream.ReadUInt32(Endian.Big);
                uint symbol_offset = stream.ReadUInt32(Endian.Big);
                long prevPosition = stream.Position;
                stream.Seek(base_symbol_name_offset + symbol_offset, SeekOrigin.Begin);
                string symbol_name = stream.ReadString();
                dict.Add(symbol_name, data_offset);
                stream.Seek(prevPosition, SeekOrigin.Begin);

            }
        }

        /// <summary>
        /// The scene itself.
        /// It is a tree of joints, materials, textures, lights, camera objects et cetera.
        /// The "entry point"/the root seem to differ per game.
        /// TODO: Custom classes currently aren't implemented.
        /// </summary>
        public class Scene
        {
            protected HashSet<TextureKey> processed_textures = new HashSet<TextureKey>();
            public List<TexEntry> textures = new List<TexEntry>();
            public Stream stream;

            /// <summary>
            /// Flag for a JObj (joint) indicating the children node is re-used for rendering.
            /// Attributes of that child joint is not used, and neither is it treated as a list (next is ignored).
            /// </summary>
            public const uint JOBJ_FLAG_INSTANCE = 0x1000;
            public const uint JOBJ_FLAG_SPLINE = 0x4000;
            public const uint JOBJ_FLAG_PTCL = 0x20;

            protected class TextureKey
            {
                public uint Width = 0;
                public uint Height = 0;
                public GXImageFormat TextureFormat = GXImageFormat.I4;
                public GXPaletteFormat PaletteFormat = GXPaletteFormat.IA8;
                public uint TextureOffset = 0;
                public uint PaletteOffset = 0;

                public override bool Equals(object obj)
                {
                    return obj is TextureKey key &&
                           Width == key.Width &&
                           Height == key.Height &&
                           TextureFormat == key.TextureFormat &&
                           PaletteFormat == key.PaletteFormat &&
                           TextureOffset == key.TextureOffset &&
                           PaletteOffset == key.PaletteOffset;
                }

                public override int GetHashCode()
                {
                    int hashCode = -906676283;
                    hashCode = hashCode * -1521134295 + Width.GetHashCode();
                    hashCode = hashCode * -1521134295 + Height.GetHashCode();
                    hashCode = hashCode * -1521134295 + TextureFormat.GetHashCode();
                    hashCode = hashCode * -1521134295 + PaletteFormat.GetHashCode();
                    hashCode = hashCode * -1521134295 + TextureOffset.GetHashCode();
                    hashCode = hashCode * -1521134295 + PaletteOffset.GetHashCode();
                    return hashCode;
                }
            }

            public Scene(Stream stream)
            {
                this.stream = stream;
            }

            public virtual void ParseJObjs(uint jobj_offset)
            {
                Queue<uint> queue = new Queue<uint>();
                queue.Enqueue(jobj_offset);

                while (queue.Count != 0)
                {
                    jobj_offset = queue.Dequeue();
                    stream.Seek(jobj_offset, SeekOrigin.Begin);
                    uint class_name_offset = stream.ReadUInt32(Endian.Big);

                    if (class_name_offset != 0) throw new Exception();

                    uint flags = stream.ReadUInt32(Endian.Big);
                    bool is_instance = (flags & JOBJ_FLAG_INSTANCE) != 0; // Don't inspect the child nodes
                    bool is_spline = (flags & JOBJ_FLAG_SPLINE) != 0;
                    bool is_ptcl = (flags & JOBJ_FLAG_PTCL) != 0;
                    uint child_offset = stream.ReadUInt32(Endian.Big);
                    uint next_offset = stream.ReadUInt32(Endian.Big);
                    uint subobj_offset = stream.ReadUInt32(Endian.Big);
                    // Ignoring attributes not relevant to extracing textures

                    if (child_offset != 0 && !is_instance)
                    {
                        queue.Enqueue(child_offset);
                    }

                    if (next_offset != 0)
                    {
                        queue.Enqueue(next_offset);
                    }

                    if (subobj_offset != 0)
                    {
                        if (!is_ptcl && !is_spline)
                        {
                            ParseDObj(subobj_offset);
                        }
                        else if (!is_ptcl && is_spline)
                        {
                            //throw new NotImplementedException("spline suboject in jobj"); // TODO
                        }
                        else if (is_ptcl && !is_spline)
                        {
                            //throw new NotImplementedException("ptcl suboject in jobj"); // TODO
                        }
                        else if (is_ptcl && is_spline)
                        {
                            throw new Exception("Invalid combination: Can't be ptcl and spline at the same time");
                        }
                    }
                }
            }

            public virtual void ParseDObj(uint dobj_offset)
            {
                Queue<uint> queue = new Queue<uint>();
                queue.Enqueue(dobj_offset);

                while (queue.Count != 0)
                {
                    dobj_offset = queue.Dequeue();
                    stream.Seek(dobj_offset, SeekOrigin.Begin);
                    uint class_name_offset = stream.ReadUInt32(Endian.Big);
                    if (class_name_offset != 0) throw new Exception();

                    uint next_offset = stream.ReadUInt32(Endian.Big);
                    uint mobj_offset = stream.ReadUInt32(Endian.Big);
                    uint pobj_offset = stream.ReadUInt32(Endian.Big); // Contains mesh data, not interesting

                    if (next_offset != 0)
                    {
                        queue.Enqueue(next_offset);
                    }

                    if (mobj_offset != 0)
                    {
                        ParseMObj(mobj_offset);
                    }
                }
            }

            public virtual void ParseMObj(uint mobj_offset)
            {
                stream.Seek(mobj_offset, SeekOrigin.Begin);

                uint class_name_offset = stream.ReadUInt32(Endian.Big);
                if (class_name_offset != 0) throw new Exception();

                uint rendermode = stream.ReadUInt32(Endian.Big);
                uint tobj_offset = stream.ReadUInt32(Endian.Big);
                // Ignoring attributes not relevant to extracing textures

                if (tobj_offset != 0)
                {
                    ParseTObj(tobj_offset);
                }
            }

            public virtual void ParseTObj(uint tobj_offset)
            {
                Queue<uint> queue = new Queue<uint>();
                queue.Enqueue(tobj_offset);

                while (queue.Count != 0)
                {
                    tobj_offset = queue.Dequeue();
                    stream.Seek(tobj_offset, SeekOrigin.Begin);
                    uint class_name_offset = stream.ReadUInt32(Endian.Big);
                    uint next_offset = stream.ReadUInt32(Endian.Big);
                    int id = stream.ReadInt32(Endian.Big); // TODO: Maybe use this to re-identify textures?
                    uint src = stream.ReadUInt32(Endian.Big);
                    float rotation_x = stream.ReadSingle(Endian.Big);
                    float rotation_y = stream.ReadSingle(Endian.Big);
                    float rotation_z = stream.ReadSingle(Endian.Big);
                    float scale_x = stream.ReadSingle(Endian.Big);
                    float scale_y = stream.ReadSingle(Endian.Big);
                    float scale_z = stream.ReadSingle(Endian.Big);
                    float translation_x = stream.ReadSingle(Endian.Big);
                    float translation_y = stream.ReadSingle(Endian.Big);
                    float translation_z = stream.ReadSingle(Endian.Big);
                    uint wrap_s = stream.ReadUInt32(Endian.Big);
                    uint wrap_t = stream.ReadUInt32(Endian.Big);
                    uint repeat_s = stream.ReadUInt8();
                    uint repeat_t = stream.ReadUInt8();
                    uint padding = stream.ReadUInt16(Endian.Big);
                    uint flags = stream.ReadUInt32(Endian.Big);
                    float blending = stream.ReadSingle(Endian.Big);
                    uint mag_filter = stream.ReadUInt32(Endian.Big);
                    uint imagedesc_offset = stream.ReadUInt32(Endian.Big);
                    uint tlut_offset = stream.ReadUInt32(Endian.Big);
                    uint texloddesc_offset = stream.ReadUInt32(Endian.Big);
                    uint tobjtevdesc_offset = stream.ReadUInt32(Endian.Big);

                    if (next_offset != 0)
                    {
                        queue.Enqueue(next_offset);
                    }

                    if (imagedesc_offset == 0)
                    {
                        throw new Exception($"TObj@{tobj_offset} has missing texture information data");
                    }

                    stream.Seek(imagedesc_offset, SeekOrigin.Begin);
                    uint texture_data_offset = stream.ReadUInt32(Endian.Big);
                    uint texture_width = stream.ReadUInt16(Endian.Big);
                    uint texture_height = stream.ReadUInt16(Endian.Big);
                    uint texture_format_raw = stream.ReadUInt32(Endian.Big);
                    GXImageFormat texture_format = (GXImageFormat)texture_format_raw;
                    uint texture_mipmap = stream.ReadUInt32(Endian.Big); // TODO what is this exactly?
                    float texture_min_lod = stream.ReadSingle(Endian.Big);
                    float texture_max_lod = stream.ReadSingle(Endian.Big);

                    if (texture_width > 2048 || texture_height > 2048)
                    {
                        throw new Exception($"TObj@{tobj_offset} references a texture of size {texture_width}x{texture_height} which is bigger than the maximum 2048x2048");
                    }

                    uint palette_data_offset = 0;
                    uint palette_format_raw = 0;
                    GXPaletteFormat palette_format = (GXPaletteFormat)palette_format_raw;
                    uint palette_name = 0;
                    uint palette_n_entries = 0;

                    if (texture_format.IsPaletteFormat())
                    {
                        if (tlut_offset == 0)
                        {
                            throw new Exception($"TObj@{tobj_offset} requires palette (texture format: {texture_format}) but is missing");
                        }
                        else
                        {
                            stream.Seek(tlut_offset, SeekOrigin.Begin);
                            palette_data_offset = stream.ReadUInt32(Endian.Big);
                            palette_format_raw = stream.ReadUInt32(Endian.Big);
                            palette_format = (GXPaletteFormat)palette_format_raw;
                            palette_name = stream.ReadUInt32(Endian.Big);
                            palette_n_entries = stream.ReadUInt16(Endian.Big);

                            if (palette_n_entries > JUtility.GetMaxColours(texture_format))
                            {
                                throw new Exception($"TObj@{tobj_offset} has {palette_n_entries} palette entries, but is over the maximum for texture format {texture_format}");
                            }
                        }
                    }

                    TextureKey textureKey = new TextureKey();
                    textureKey.TextureFormat = texture_format;
                    textureKey.PaletteFormat = palette_format;
                    textureKey.Width = texture_width;
                    textureKey.Height = texture_width;
                    textureKey.TextureOffset = texture_data_offset;
                    textureKey.PaletteOffset = palette_data_offset;

                    if (!processed_textures.Contains(textureKey))
                    {
                        byte[] palette_data = null;
                        if (texture_format.IsPaletteFormat())
                        {
                            stream.Seek(palette_data_offset, SeekOrigin.Begin);
                            palette_data = new byte[palette_n_entries * 2];
                            stream.Read(palette_data, 0, palette_data.Length);
                        }

                        stream.Seek(texture_data_offset, SeekOrigin.Begin);
                        textures.Add(new TexEntry(stream, palette_data, texture_format, palette_format, (int)palette_n_entries, (int)texture_width, (int)texture_height)
                        {
                            WrapS = (GXWrapMode)wrap_s,
                            WrapT = (GXWrapMode)wrap_t,
                        });

                        processed_textures.Add(textureKey);
                    }
                }
            }
        }

        protected override void Write(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
