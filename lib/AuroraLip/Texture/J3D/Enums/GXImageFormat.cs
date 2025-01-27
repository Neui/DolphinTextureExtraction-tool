﻿namespace AuroraLip.Texture.J3D
{
    public static partial class JUtility
    {
        /// <summary>
        /// ImageFormat specifies how the data within the image is encoded.
        /// Included is a chart of how many bits per pixel there are, 
        /// the width/height of each block, how many bytes long the
        /// actual block is, and a description of the type of data stored.
        /// </summary>
        public enum GXImageFormat : byte
        {
            /// <summary>
            /// Greyscale - 4 bits/pixel (bpp) | Block Width: 8 | Block height: 8 | Block size: 32 bytes
            /// </summary>
            I4 = 0x00,
            /// <summary>
            /// Greyscale - 8 bits/pixel (bpp) | Block Width: 8 | Block height: 4 | Block size: 32 bytes
            /// </summary>
            I8 = 0x01,
            /// <summary>
            /// Greyscale + Alpha - 8 bits/pixel (bpp) | Block Width: 8 | Block height: 4 | Block size: 32 bytes
            /// </summary>
            IA4 = 0x02,
            /// <summary>
            /// Greyscale + Alpha - 16 bits/pixel (bpp) | Block Width: 4 | Block height: 4 | Block size: 32 bytes
            /// </summary>
            IA8 = 0x03,
            /// <summary>
            /// Colour - 16 bits/pixel (bpp) | Block Width: 4 | Block height: 4 | Block size: 32 bytes
            /// </summary>
            RGB565 = 0x04,
            /// <summary>
            /// Colour + Alpha - 16 bits/pixel (bpp) | Block Width: 4 | Block height: 4 | Block size: 32 bytes
            /// </summary>
            RGB5A3 = 0x05,
            /// <summary>
            /// Colour + Alpha - 32 bits/pixel (bpp) | Block Width: 4 | Block height: 4 | Block size: 64 bytes
            /// </summary>
            RGBA32 = 0x06,
            /// <summary>
            /// Palette - 4 bits/pixel (bpp) | Block Width: 8 | Block Height: 8 | Block size: 32 bytes
            /// </summary>
            C4 = 0x08,
            /// <summary>
            /// Palette - 8 bits/pixel (bpp) | Block Width: 8 | Block Height: 4 | Block size: 32 bytes
            /// </summary>
            C8 = 0x09,
            /// <summary>
            /// Palette - 14 bits/pixel (bpp) | Block Width: 4 | Block Height: 4 | Block size: 32 bytes
            /// </summary>
            C14X2 = 0x0A,
            /// <summary>
            /// Colour + Alpha (1 bit) - 4 bits/pixel (bpp) | Block Width: 8 | Block height: 8 | Block size: 32 bytes
            /// </summary>
            CMPR = 0x0E
        }
    }
}
