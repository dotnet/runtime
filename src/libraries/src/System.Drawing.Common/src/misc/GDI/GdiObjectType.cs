// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    /// <summary>
    /// Specifies the the type of a GDI object.
    /// </summary>
    internal enum GdiObjectType
    {
        Pen = 1,
        Brush = 2,
        DisplayDC = 3,
        MetafileDC = 4,
        Palette = 5,
        Font = 6,
        Bitmap = 7,
        Region = 8,
        Metafile = 9,
        MemoryDC = 10,
        ExtendedPen = 11,
        EnhancedMetafileDC = 12,
        EnhMetafile = 13,
        ColorSpace = 14
    }
}
