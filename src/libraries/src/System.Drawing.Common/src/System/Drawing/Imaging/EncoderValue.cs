// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// The EncoderValue enum.
    /// </summary>
    public enum EncoderValue
    {
        /// <summary>
        /// Specifies the CMYK color space.
        /// </summary>
        ColorTypeCMYK,
        /// <summary>
        /// Specifies the YCCK color space.
        /// </summary>
        ColorTypeYCCK,
        /// <summary>
        /// Specifies the LZW compression method.
        /// </summary>
        CompressionLZW,
        /// <summary>
        /// For a TIFF image, specifies the CCITT3 compression method.
        /// </summary>
        CompressionCCITT3,
        /// <summary>
        /// For a TIFF image, specifies the CCITT4 compression method.
        /// </summary>
        CompressionCCITT4,
        /// <summary>
        /// For a TIFF image, specifies the RLE compression method.
        /// </summary>
        CompressionRle,
        /// <summary>
        /// For a TIFF image, specifies no compression.
        /// </summary>
        CompressionNone,
        /// <summary>
        /// Specifies interlaced mode.
        /// </summary>
        ScanMethodInterlaced,
        /// <summary>
        /// Specifies non-interlaced mode.
        /// </summary>
        ScanMethodNonInterlaced,
        /// <summary>
        /// For a GIF image, specifies version 87.
        /// </summary>
        VersionGif87,
        /// <summary>
        /// For a GIF images, specifies version 89a.
        /// </summary>
        VersionGif89,
        /// <summary>
        /// Specifies progressive mode.
        /// </summary>
        RenderProgressive,
        /// <summary>
        /// Specifies non-progressive mode.
        /// </summary>
        RenderNonProgressive,
        /// <summary>
        /// For a JPEG image, specifies lossless 90-degree clockwise rotation.
        /// </summary>
        TransformRotate90,
        /// <summary>
        /// For a JPEG image, specifies lossless 180-degree rotation.
        /// </summary>
        TransformRotate180,
        /// <summary>
        /// For a JPEG image, specifies lossless 270-degree clockwise rotation.
        /// </summary>
        TransformRotate270,
        /// <summary>
        /// For a JPEG image, specifies a lossless horizontal flip.
        /// </summary>
        TransformFlipHorizontal,
        /// <summary>
        /// For a JPEG image, specifies a lossless vertical flip.
        /// </summary>
        TransformFlipVertical,
        /// <summary>
        /// Specifies multiframe encoding.
        /// </summary>
        MultiFrame,
        /// <summary>
        /// Specifies the last frame of a multi-frame image.
        /// </summary>
        LastFrame,
        /// <summary>
        /// Specifies that the encoder object is to be closed. 
        /// </summary>
        Flush,
        /// <summary>
        /// For a GIF image, specifies the time frame dimension.
        /// </summary>
        FrameDimensionTime,
        /// <summary>
        /// Specifies the resolution frame dimension.
        /// </summary>
        FrameDimensionResolution,
        /// <summary>
        /// For a TIFF image, specifies the page frame dimension
        /// </summary>
        FrameDimensionPage
    }
}
