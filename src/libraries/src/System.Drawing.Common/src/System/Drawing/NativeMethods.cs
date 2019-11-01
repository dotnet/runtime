// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal class NativeMethods
    {
        internal static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public const int MAX_PATH = 260;
        internal const int SM_REMOTESESSION = 0x1000;

        internal const int OBJ_DC = 3,
                         OBJ_METADC = 4,
                         OBJ_MEMDC = 10,
                         OBJ_ENHMETADC = 12,
                         DIB_RGB_COLORS = 0,
                         BI_BITFIELDS = 3,
                         BI_RGB = 0,
                         BITMAPINFO_MAX_COLORSIZE = 256,
                         DEFAULT_GUI_FONT = 17;

        internal const uint SPI_GETICONTITLELOGFONT = 0x001F;

        // Gets metrics associated with the nonclient area of nonminimized windows
        internal const uint SPI_GETNONCLIENTMETRICS = 41;

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAPINFO_FLAT
        {
            public int bmiHeader_biSize; // = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
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

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = BITMAPINFO_MAX_COLORSIZE * 4)]
            public byte[] bmiColors; // RGBQUAD structs... Blue-Green-Red-Reserved, repeat...
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class BITMAPINFOHEADER
        {
            public int biSize = 40;
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NONCLIENTMETRICS
        {
            public uint cbSize;
            public int iBorderWidth;
            public int iScrollWidth;
            public int iScrollHeight;
            public int iCaptionWidth;
            public int iCaptionHeight;
            public SafeNativeMethods.LOGFONT lfCaptionFont;
            public int iSmCaptionWidth;
            public int iSmCaptionHeight;
            public SafeNativeMethods.LOGFONT lfSmCaptionFont;
            public int iMenuWidth;
            public int iMenuHeight;
            public SafeNativeMethods.LOGFONT lfMenuFont;
            public SafeNativeMethods.LOGFONT lfStatusFont;
            public SafeNativeMethods.LOGFONT lfMessageFont;

            // This is supported on Windows vista and later
            public int iPaddedBorderWidth;
        }
    }
}
