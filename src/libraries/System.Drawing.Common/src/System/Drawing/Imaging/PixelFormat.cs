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

    //
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    // XX                                                                         XX
    // XX  If you modify this file, please update Image.GetColorDepth()           XX
    // XX                                                                         XX
    // XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    //
    /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat"]/*' />
    /// <devdoc>
    ///    Specifies the format of the color data for
    ///    each pixel in the image.
    /// </devdoc>
    public enum PixelFormat
    {
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Indexed"]/*' />
        /// <devdoc>
        ///    Specifies that pixel data contains
        ///    color indexed values which means they are an index to colors in the system color
        ///    table, as opposed to individual color values.
        /// </devdoc>
        Indexed = 0x00010000,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Gdi"]/*' />
        /// <devdoc>
        ///    Specifies that pixel data contains GDI
        ///    colors.
        /// </devdoc>
        Gdi = 0x00020000,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Alpha"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel data contains alpha values that are
        ///       not pre-multiplied.
        ///    </para>
        /// </devdoc>
        Alpha = 0x00040000,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.PAlpha"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format contains pre-multipled alpha values.
        ///    </para>
        /// </devdoc>
        PAlpha = 0x00080000, // What's this?
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Extended"]/*' />
        /// <devdoc>
        ///    Specifies that
        /// </devdoc>
        Extended = 0x00100000,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Canonical"]/*' />
        /// <devdoc>
        ///    Specifies that
        /// </devdoc>
        Canonical = 0x00200000,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Undefined"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is undefined.
        ///    </para>
        /// </devdoc>
        Undefined = 0,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.DontCare"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is a don't care.
        ///    </para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        DontCare = 0,
        // makes it into devtools, we can change this.
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format1bppIndexed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies thatpixel format is 1 bit per pixel indexed
        ///       color. The color table therefore has two colors in it.
        ///    </para>
        /// </devdoc>
        Format1bppIndexed = 1 | (1 << 8) | (int)Indexed | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format4bppIndexed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 4 bits per pixel indexed
        ///       color. The color table therefore has 16 colors in it.
        ///    </para>
        /// </devdoc>
        Format4bppIndexed = 2 | (4 << 8) | (int)Indexed | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format8bppIndexed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 8 bits per pixel indexed
        ///       color. The color table therefore has 256 colors in it.
        ///    </para>
        /// </devdoc>
        Format8bppIndexed = 3 | (8 << 8) | (int)Indexed | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format16bppGrayScale"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Format16bppGrayScale = 4 | (16 << 8) | (int)Extended,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format16bppRgb555"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 16 bits per pixel. The
        ///       color information specifies 65536 shades of gray.
        ///    </para>
        /// </devdoc>                              
        Format16bppRgb555 = 5 | (16 << 8) | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format16bppRgb565"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 16 bits per pixel. The
        ///       color information specifies 32768 shades of color of which 5 bits are red, 5
        ///       bits are green and 5 bits are blue.
        ///    </para>
        /// </devdoc>
        Format16bppRgb565 = 6 | (16 << 8) | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format16bppArgb1555"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format
        ///       is 16 bits per pixel. The color information specifies 32768
        ///       shades of color of which 5 bits are red, 5 bits are green, 5
        ///       bits are blue and 1 bit is alpha.
        ///    </para>
        /// </devdoc>
        Format16bppArgb1555 = 7 | (16 << 8) | (int)Alpha | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format24bppRgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 24 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 8 bits are red, 8
        ///       bits are green and 8 bits are blue.
        ///    </para>
        /// </devdoc>
        Format24bppRgb = 8 | (24 << 8) | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format32bppRgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 24 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 8 bits are red, 8
        ///       bits are green and 8 bits are blue.
        ///    </para>
        /// </devdoc>
        Format32bppRgb = 9 | (32 << 8) | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format32bppArgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 32 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 8 bits are red, 8
        ///       bits are green and 8 bits are blue. The 8 additional bits are alpha bits.
        ///    </para>
        /// </devdoc>
        Format32bppArgb = 10 | (32 << 8) | (int)Alpha | (int)Gdi | (int)Canonical,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format32bppPArgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that
        ///       pixel format is 32 bits per pixel. The color information
        ///       specifies 16777216 shades of color of which 8 bits are red, 8 bits are
        ///       green and 8 bits are blue. The 8 additional bits are pre-multiplied alpha bits. .
        ///    </para>
        /// </devdoc>
        Format32bppPArgb = 11 | (32 << 8) | (int)Alpha | (int)PAlpha | (int)Gdi,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format48bppRgb"]/*' />
        /// <devdoc>
        ///    Specifies that pixel format is 48 bits per pixel.
        ///    The color information specifies 16777216 shades of color of which 8 bits are
        ///    red, 8 bits are green and 8 bits are blue. The 8 additional bits are alpha bits.
        /// </devdoc>
        Format48bppRgb = 12 | (48 << 8) | (int)Extended,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format64bppArgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies pixel format is 64 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 16 bits are red, 16
        ///       bits are green and 16 bits are blue. The 16 additional bits are alpha bits.
        ///    </para>
        /// </devdoc>
        Format64bppArgb = 13 | (64 << 8) | (int)Alpha | (int)Canonical | (int)Extended,
        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Format64bppPArgb"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 64 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 16 bits are red,
        ///       16 bits are green and 16 bits are blue. The 16 additional bits are
        ///       pre-multiplied alpha bits.
        ///    </para>
        /// </devdoc>
        Format64bppPArgb = 14 | (64 << 8) | (int)Alpha | (int)PAlpha | (int)Extended,

        /// <include file='doc\PixelFormat.uex' path='docs/doc[@for="PixelFormat.Max"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that pixel format is 64 bits per pixel. The
        ///       color information specifies 16777216 shades of color of which 16 bits are red,
        ///       16 bits are green and 16 bits are blue. The 16 additional bits are alpha bits.
        ///    </para>
        /// </devdoc>
        Max = 15,
    }
}

