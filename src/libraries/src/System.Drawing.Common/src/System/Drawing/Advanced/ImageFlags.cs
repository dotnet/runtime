// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Color channel flag constants
     */
    /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags"]/*' />
    /// <devdoc>
    ///    Specifies the attributes of the pixel data
    ///    contained in an <see langword='Image'/> object.
    /// </devdoc>
    [Flags()]
    public enum ImageFlags
    {
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.None"]/*' />
        /// <devdoc>
        ///    There is no format information.
        /// </devdoc>
        None = 0,

        // Low-word: shared with SINKFLAG_x

        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.Scalable"]/*' />
        /// <devdoc>
        ///    Pixel data is scalable.
        /// </devdoc>
        Scalable = 0x0001,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.HasAlpha"]/*' />
        /// <devdoc>
        ///    Pixel data contains alpha information.
        /// </devdoc>
        HasAlpha = 0x0002,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.HasTranslucent"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HasTranslucent = 0x0004,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.PartiallyScalable"]/*' />
        /// <devdoc>
        ///    Pixel data is partially scalable, but there
        ///    are some limitations.
        /// </devdoc>
        PartiallyScalable = 0x0008,

        // Low-word: color space definition

        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ColorSpaceRgb"]/*' />
        /// <devdoc>
        ///    Pixel data uses an RGB color space.
        /// </devdoc>
        ColorSpaceRgb = 0x0010,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ColorSpaceCmyk"]/*' />
        /// <devdoc>
        ///    Pixel data uses a CMYK color space.
        /// </devdoc>
        ColorSpaceCmyk = 0x0020,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ColorSpaceGray"]/*' />
        /// <devdoc>
        ///    Pixel data is grayscale.
        /// </devdoc>
        ColorSpaceGray = 0x0040,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ColorSpaceYcbcr"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ColorSpaceYcbcr = 0x0080,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ColorSpaceYcck"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ColorSpaceYcck = 0x0100,

        // Low-word: image size info

        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.HasRealDpi"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HasRealDpi = 0x1000,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.HasRealPixelSize"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HasRealPixelSize = 0x2000,

        // High-word

        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.ReadOnly"]/*' />
        /// <devdoc>
        ///    Pixel data is read-only.
        /// </devdoc>
        ReadOnly = 0x00010000,
        /// <include file='doc\ImageFlags.uex' path='docs/doc[@for="ImageFlags.Caching"]/*' />
        /// <devdoc>
        ///    Pixel data can be cached for faster access.
        /// </devdoc>
        Caching = 0x00020000
    }
}
