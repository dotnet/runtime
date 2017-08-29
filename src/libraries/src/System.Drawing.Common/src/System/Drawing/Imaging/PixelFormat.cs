// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /*
     * In-memory pixel data formats:
     *  bits 0-7 = format index
     *  bits 8-15 = pixel size (in bits)
     *  bits 16-23 = flags
     *  bits 24-31 = reserved
     */

    // If you modify this file, please update Image.GetColorDepth()

    /// <summary>
    /// Specifies the format of the color data for each pixel in the image.
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>
        /// Specifies that pixel data contains color indexed values which means they are an index to colors in the
        /// system color table, as opposed to individual color values.
        /// </summary>
        Indexed = 0x00010000,
        /// <summary>
        /// Specifies that pixel data contains GDI colors.
        /// </summary>
        Gdi = 0x00020000,
        /// <summary>
        /// Specifies that pixel data contains alpha values that are not pre-multiplied.
        /// </summary>
        Alpha = 0x00040000,
        /// <summary>
        /// Specifies that pixel format contains pre-multiplied alpha values.
        /// </summary>
        PAlpha = 0x00080000, // What's this?
        Extended = 0x00100000,
        Canonical = 0x00200000,
        /// <summary>
        /// Specifies that pixel format is undefined.
        /// </summary>
        Undefined = 0,
        /// <summary>
        /// Specifies that pixel format is a don't care.
        /// </summary>
        DontCare = 0,
        // makes it into devtools, we can change this.
        /// <summary>
        /// Specifies that pixel format is 1 bit per pixel indexed color. The color table therefore has two colors in it.
        /// </summary>
        Format1bppIndexed = 1 | (1 << 8) | (int)Indexed | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 4 bits per pixel indexed color. The color table therefore has 16 colors in it.
        /// </summary>
        Format4bppIndexed = 2 | (4 << 8) | (int)Indexed | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 8 bits per pixel indexed color. The color table therefore has 256 colors in it.
        /// </summary>
        Format8bppIndexed = 3 | (8 << 8) | (int)Indexed | (int)Gdi,
        Format16bppGrayScale = 4 | (16 << 8) | (int)Extended,
        /// <summary>
        /// Specifies that pixel format is 16 bits per pixel. The color information specifies 65536 shades of gray.
        /// </summary>                              
        Format16bppRgb555 = 5 | (16 << 8) | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 16 bits per pixel. The color information specifies 32768 shades of color of
        /// which 5 bits are red, 5 bits are green and 5 bits are blue.
        /// </summary>
        Format16bppRgb565 = 6 | (16 << 8) | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 16 bits per pixel. The color information specifies 32768 shades of color of
        /// which 5 bits are red, 5 bits are green, 5 bits are blue and 1 bit is alpha.
        /// </summary>
        Format16bppArgb1555 = 7 | (16 << 8) | (int)Alpha | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 24 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 8 bits are red, 8 bits are green and 8 bits are blue.
        /// </summary>
        Format24bppRgb = 8 | (24 << 8) | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 24 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 8 bits are red, 8 bits are green and 8 bits are blue.
        /// </summary>
        Format32bppRgb = 9 | (32 << 8) | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 32 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 8 bits are red, 8 bits are green and 8 bits are blue. The 8 additional bits are alpha bits.
        /// </summary>
        Format32bppArgb = 10 | (32 << 8) | (int)Alpha | (int)Gdi | (int)Canonical,
        /// <summary>
        /// Specifies that pixel format is 32 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 8 bits are red, 8 bits are green and 8 bits are blue. The 8 additional bits are pre-multiplied alpha bits.
        /// </summary>
        Format32bppPArgb = 11 | (32 << 8) | (int)Alpha | (int)PAlpha | (int)Gdi,
        /// <summary>
        /// Specifies that pixel format is 48 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 8 bits are red, 8 bits are green and 8 bits are blue. The 8 additional bits are alpha bits.
        /// </summary>
        Format48bppRgb = 12 | (48 << 8) | (int)Extended,
        /// <summary>
        /// Specifies pixel format is 64 bits per pixel. The color information specifies 16777216 shades of color of
        /// which 16 bits are red, 16 bits are green and 16 bits are blue. The 16 additional bits are alpha bits.
        /// </summary>
        Format64bppArgb = 13 | (64 << 8) | (int)Alpha | (int)Canonical | (int)Extended,
        /// <summary>
        /// Specifies that pixel format is 64 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 16 bits are red, 16 bits are green and 16 bits are blue. The 16 additional bits are pre-multiplied
        /// alpha bits.
        /// </summary>
        Format64bppPArgb = 14 | (64 << 8) | (int)Alpha | (int)PAlpha | (int)Extended,

        /// <summary>
        /// Specifies that pixel format is 64 bits per pixel. The color information specifies 16777216 shades of color
        /// of which 16 bits are red, 16 bits are green and 16 bits are blue. The 16 additional bits are alpha bits.
        /// </summary>
        Max = 15,
    }
}

