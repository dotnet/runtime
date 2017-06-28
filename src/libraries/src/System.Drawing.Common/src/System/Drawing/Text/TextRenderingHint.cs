// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    /// <summary>
    /// Specifies the quality of text rendering.
    /// </summary>
    public enum TextRenderingHint
    {
        /// <summary>
        /// Glyph with system default rendering hint.
        /// </summary>
        SystemDefault = 0,

        /// <summary>
        /// Glyph bitmap with hinting.
        /// </summary>
        SingleBitPerPixelGridFit,

        /// <summary>
        /// Glyph bitmap without hinting.
        /// </summary>
        SingleBitPerPixel,

        /// <summary>
        /// Anti-aliasing with hinting.
        /// </summary>
        AntiAliasGridFit,

        /// <summary>
        /// Glyph anti-alias bitmap without hinting.
        /// </summary>
        AntiAlias,

        /// <summary>
        /// Glyph CT bitmap with hinting.
        /// </summary>
        ClearTypeGridFit
    }
}
