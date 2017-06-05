// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    /// <devdoc>
    ///    Specifies the the type of a GDI object.
    /// </devdoc>
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

        /*
        OBJ_PEN       = 1,
        OBJ_BRUSH     = 2,
        OBJ_FONT      = 6,
        OBJ_EXTPEN    = 11,
            
        OBJ_DC        = 3,
        OBJ_METADC    = 4,
        OBJ_MEMDC     = 10,
        OBJ_ENHMETADC = 12,
    */
    }
}
