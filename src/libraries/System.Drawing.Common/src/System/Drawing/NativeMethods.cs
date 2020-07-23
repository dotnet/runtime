// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal class NativeMethods
    {
        internal static HandleRef NullHandleRef => new HandleRef(null, IntPtr.Zero);

        public const int MAX_PATH = 260;
        internal const int SM_REMOTESESSION = 0x1000;

        internal const int  DIB_RGB_COLORS = 0;
        internal const int BI_BITFIELDS = 3;
        internal const int BI_RGB = 0;
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PALETTEENTRY
        {
            public byte peRed;
            public byte peGreen;
            public byte peBlue;
            public byte peFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }
    }
}
