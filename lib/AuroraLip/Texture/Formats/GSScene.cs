using AuroraLip.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using static AuroraLip.Texture.J3D.JUtility;

namespace AuroraLip.Texture.Formats
{
    /// <summary>
    /// Extracts textures from a Scene in Pokémon XD Gale of Darkness (developed by Genius Sonority, hence the GS prefix).
    /// TODO: Find out if other Genius Sonority uses this format (such as Pokémon Colosseum and maybe other GameCube games).
    /// </summary>
    public class GSScene : HSD
    {
        public string Extension => ".gsscene";

        public override bool IsMatch(Stream stream, in string extension = "")
            => extension.ToLower() == Extension;

        public GSScene() { }

        public GSScene(Stream stream) => Read(stream);

        protected override void Read(Stream stream)
        {
            ArchiveInfo archiveInfo = new ArchiveInfo();
            archiveInfo.Read(stream);

            uint scene_data_offset;
            if (!archiveInfo.PublicSymbols.TryGetValue("scene_data", out scene_data_offset))
            {
                throw new Exception("scene_data symbol not found");
            }

            Stream sceneStream = archiveInfo.DataStream;
            Scene scene = new Scene(sceneStream);
            sceneStream.Seek(scene_data_offset, SeekOrigin.Begin);

            uint model_array_offset = sceneStream.ReadUInt32(Endian.Big);
            if (model_array_offset == 0)
                return;

            sceneStream.Seek(model_array_offset, SeekOrigin.Begin);
            uint model_offset = sceneStream.ReadUInt32(Endian.Big);
            while (model_offset != 0)
            {
                sceneStream.Seek(model_offset, SeekOrigin.Begin);
                uint jobj_offset = sceneStream.ReadUInt32(Endian.Big);

                scene.ParseJObjs(jobj_offset);

                model_array_offset += 4;
                sceneStream.Seek(model_array_offset, SeekOrigin.Begin);
                model_offset = sceneStream.ReadUInt32(Endian.Big);
            }

            foreach (TexEntry tex_entry in scene.textures)
            {
                Add(tex_entry);
            }
        }
    }
}
