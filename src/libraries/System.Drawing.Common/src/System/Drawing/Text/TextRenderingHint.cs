// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Text
{
    public enum TextRenderingHint
    {
        // Glyph with system default rendering hint
        SystemDefault = 0,

        // Glyph bitmap with hinting
        SingleBitPerPixelGridFit,

        // Glyph bitmap without hinting
        SingleBitPerPixel,

        //Anti-aliasing with hinting
        AntiAliasGridFit,

        // Glyph anti-alias bitmap without hinting
        AntiAlias,

        // Glyph CT bitmap with hinting
        ClearTypeGridFit
    }
}
