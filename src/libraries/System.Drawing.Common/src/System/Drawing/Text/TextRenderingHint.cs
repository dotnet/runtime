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
        SystemDefault = 0,        // Glyph with system default rendering hint
        SingleBitPerPixelGridFit, // Glyph bitmap with hinting
        SingleBitPerPixel,        // Glyph bitmap without hinting
        AntiAliasGridFit,         // Anti-aliasing with hinting
        AntiAlias,                // Glyph anti-alias bitmap without hinting
        ClearTypeGridFit          // Glyph CT bitmap with hinting
    }
}

