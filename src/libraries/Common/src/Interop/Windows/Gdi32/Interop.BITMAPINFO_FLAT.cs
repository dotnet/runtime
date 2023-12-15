// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        internal const int BITMAPINFO_MAX_COLORSIZE = 256;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct BITMAPINFO_FLAT
        {
            public int bmiHeader_biSize; // = sizeof(BITMAPINFOHEADER)
            public int bmiHeader_biWidth;
            public int bmiHeader_biHeight;
            public short bmiHeader_biPlanes;
            public short bmiHeader_biBitCount;
            public int bmiHeader_biCompression;
            public int bmiHeader_biSizeImage;
            public int bmiHeader_biXPelsPerMeter;
            public int bmiHeader_biYPelsPerMeter;
            public int bmiHeader_biClrUsed;
            public int bmiHeader_biClrImportant;

            public fixed byte bmiColors[BITMAPINFO_MAX_COLORSIZE * 4]; // RGBQUAD structs... Blue-Green-Red-Reserved, repeat...
        }
    }
}
