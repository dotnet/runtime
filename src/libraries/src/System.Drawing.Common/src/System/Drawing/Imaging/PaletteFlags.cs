// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the type of color data in the system palette. The data can be color data with alpha, grayscale only,
    /// or halftone data.
    /// </summary>
    [Flags]
    public enum PaletteFlags
    {
        /// <summary>
        /// Specifies alpha data.
        /// </summary>
        HasAlpha = 0x0001,
        /// <summary>
        /// Specifies grayscale data.
        /// </summary>
        GrayScale = 0x0002,
        /// <summary>
        /// Specifies halftone data.
        /// </summary>
        Halftone = 0x0004
    }
}
