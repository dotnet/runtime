// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.NativeMethods..ctor()")]

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
                         SPI_GETICONTITLELOGFONT = 0x001F,
                         SPI_GETNONCLIENTMETRICS = 41,
                         DEFAULT_GUI_FONT = 17;

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAPINFO_FLAT
        {
            public int bmiHeader_biSize;// = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
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

        [StructLayout(LayoutKind.Sequential)]
        internal class NONCLIENTMETRICS
        {
            public int cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS));
            public int iBorderWidth;
            public int iScrollWidth;
            public int iScrollHeight;
            public int iCaptionWidth;
            public int iCaptionHeight;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
            public SafeNativeMethods.LOGFONT lfCaptionFont;
            public int iSmCaptionWidth;
            public int iSmCaptionHeight;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
            public SafeNativeMethods.LOGFONT lfSmCaptionFont;
            public int iMenuWidth;
            public int iMenuHeight;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
            public SafeNativeMethods.LOGFONT lfMenuFont;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
            public SafeNativeMethods.LOGFONT lfStatusFont;
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            [MarshalAs(UnmanagedType.Struct)]
#pragma warning restore CS0618
            public SafeNativeMethods.LOGFONT lfMessageFont;
        }
    }
}
