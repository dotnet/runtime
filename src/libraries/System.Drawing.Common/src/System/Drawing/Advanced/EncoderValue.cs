// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * EncoderParameter Value Type
     */
    /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue"]/*' />
    /// <devdoc>
    ///    <para>
    ///       The EncoderValue enum.
    ///    </para>
    /// </devdoc>
    public enum EncoderValue
    {
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.ColorTypeCMYK"]/*' />
        /// <devdoc>
        ///    Specifies the CMYK color space.
        /// </devdoc>
        ColorTypeCMYK,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.ColorTypeYCCK"]/*' />
        /// <devdoc>
        ///    Specifies the YCCK color space.
        /// </devdoc>
        ColorTypeYCCK,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.CompressionLZW"]/*' />
        /// <devdoc>
        ///    Specifies the LZW compression method.
        /// </devdoc>
        CompressionLZW,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.CompressionCCITT3"]/*' />
        /// <devdoc>
        ///    For a TIFF image, specifies the CCITT3 compression method.
        /// </devdoc>
        CompressionCCITT3,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.CompressionCCITT4"]/*' />
        /// <devdoc>
        ///    For a TIFF image, specifies the CCITT4 compression method.
        /// </devdoc>
        CompressionCCITT4,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.CompressionRle"]/*' />
        /// <devdoc>
        ///    For a TIFF image, specifies the RLE compression method.
        /// </devdoc>
        CompressionRle,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.CompressionNone"]/*' />
        /// <devdoc>
        ///    For a TIFF image, specifies no compression.
        /// </devdoc>
        CompressionNone,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.ScanMethodInterlaced"]/*' />
        /// <devdoc>
        ///    Specifies interlaced mode.
        /// </devdoc>
        ScanMethodInterlaced,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.ScanMethodNonInterlaced"]/*' />
        /// <devdoc>
        ///    Specifies non-interlaced mode.
        /// </devdoc>
        ScanMethodNonInterlaced,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.VersionGif87"]/*' />
        /// <devdoc>
        ///    For a GIF image, specifies version 87.
        /// </devdoc>
        VersionGif87,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.VersionGif89"]/*' />
        /// <devdoc>
        ///    For a GIF images, specifies version 89a.
        /// </devdoc>
        VersionGif89,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.RenderProgressive"]/*' />
        /// <devdoc>
        ///    Specifies progressive mode.
        /// </devdoc>
        RenderProgressive,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.RenderNonProgressive"]/*' />
        /// <devdoc>
        ///    Specifies non-progressive mode.
        /// </devdoc>
        RenderNonProgressive,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.TransformRotate90"]/*' />
        /// <devdoc>
        ///    For a JPEG image, specifies lossless 90-degree clockwise rotation.
        /// </devdoc>
        TransformRotate90,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.TransformRotate180"]/*' />
        /// <devdoc>
        ///    For a JPEG image, specifies lossless 180-degree rotation.
        /// </devdoc>
        TransformRotate180,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.TransformRotate270"]/*' />
        /// <devdoc>
        ///    For a JPEG image, specifies lossless 270-degree clockwise rotation.
        /// </devdoc>
        TransformRotate270,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.TransformFlipHorizontal"]/*' />
        /// <devdoc>
        ///    For a JPEG image, specifies a lossless horizontal flip.
        /// </devdoc>
        TransformFlipHorizontal,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.TransformFlipVertical"]/*' />
        /// <devdoc>
        ///    For a JPEG image, specifies a lossless vertical flip.
        /// </devdoc>
        TransformFlipVertical,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.MultiFrame"]/*' />
        /// <devdoc>
        ///    Specifies multiframe encoding.
        /// </devdoc>
        MultiFrame,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.LastFrame"]/*' />
        /// <devdoc>
        ///    Specifies the last frame of a multi-frame image.
        /// </devdoc>
        LastFrame,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.Flush"]/*' />
        /// <devdoc>
        ///    Specifies that the encoder object is to be closed. 
        /// </devdoc>
        Flush,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.FrameDimensionTime"]/*' />
        /// <devdoc>
        ///    For a GIF image, specifies the time frame dimension.
        /// </devdoc>
        FrameDimensionTime,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.FrameDimensionResolution"]/*' />
        /// <devdoc>
        ///    Specifies the resolution frame dimension.
        /// </devdoc>
        FrameDimensionResolution,
        /// <include file='doc\EncoderValue.uex' path='docs/doc[@for="EncoderValue.FrameDimensionPage"]/*' />
        /// <devdoc>
        ///    For a TIFF image, specifies the page frame dimension
        /// </devdoc>
        FrameDimensionPage
    }
}
