// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the attributes of the pixel data contained in an <see cref='Image'/> object.
    /// </summary>
    [Flags]
    public enum ImageFlags
    {
        /// <summary>
        /// There is no format information.
        /// </summary>
        None = 0,

        // Low-word: shared with SINKFLAG_x

        /// <summary>
        /// Pixel data is scalable.
        /// </summary>
        Scalable = 0x0001,
        /// <summary>
        /// Pixel data contains alpha information.
        /// </summary>
        HasAlpha = 0x0002,
        HasTranslucent = 0x0004,
        /// <summary>
        /// Pixel data is partially scalable, but there are some limitations.
        /// </summary>
        PartiallyScalable = 0x0008,

        // Low-word: color space definition

        /// <summary>
        /// Pixel data uses an RGB color space.
        /// </summary>
        ColorSpaceRgb = 0x0010,
        /// <summary>
        /// Pixel data uses a CMYK color space.
        /// </summary>
        ColorSpaceCmyk = 0x0020,
        /// <summary>
        /// Pixel data is grayscale.
        /// </summary>
        ColorSpaceGray = 0x0040,
        ColorSpaceYcbcr = 0x0080,
        ColorSpaceYcck = 0x0100,

        // Low-word: image size info

        HasRealDpi = 0x1000,
        HasRealPixelSize = 0x2000,

        // High-word

        ReadOnly = 0x00010000,
        Caching = 0x00020000
    }
}
