// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        public enum ObjectType : int
        {
            OBJ_PEN = 1,
            OBJ_BRUSH = 2,
            OBJ_DC = 3,
            OBJ_METADC = 4,
            OBJ_PAL = 5,
            OBJ_FONT = 6,
            OBJ_BITMAP = 7,
            OBJ_REGION = 8,
            OBJ_METAFILE = 9,
            OBJ_MEMDC = 10,
            OBJ_EXTPEN = 11,
            OBJ_ENHMETADC = 12,
            OBJ_ENHMETAFILE = 13,
            OBJ_COLORSPACE = 14
        }
    }
}
