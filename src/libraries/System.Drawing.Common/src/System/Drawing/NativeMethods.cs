// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.NativeMethods..ctor()")]

namespace System.Drawing
{
    using System.Runtime.InteropServices;

    internal class NativeMethods
    {
        internal static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

        public enum RegionFlags
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3,
        }

        public const byte PC_NOCOLLAPSE = 0x04;


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

            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = BITMAPINFO_MAX_COLORSIZE * 4)]
            public byte[] bmiColors; // RGBQUAD structs... Blue-Green-Red-Reserved, repeat...
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class BITMAPINFOHEADER
        {
            public int biSize = 40;    // ndirect.DllLib.sizeOf( this );
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

        internal struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
            public byte rgbReserved;
#pragma warning restore 0414
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

        /* FxCop rule 'AvoidBuildingNonCallableCode' - Left here in case it is needed in the future.
        public static byte[] Win9xHalfTonePalette {
            get {

                               
                return new byte[] {
                      // The first 10 system colors
                       0x00, 0x00, 0x00, 0x00,            //      0  Sys Black, gray 0
                       0x80, 0x00, 0x00, 0x00,            //      1  Sys Dk Red
                       0x00, 0x80, 0x00, 0x00,            //      2  Sys Dk Green
                       0x80, 0x80, 0x00, 0x00,            //      3  Sys Dk Yellow
                       0x00, 0x00, 0x80, 0x00,            //      4  Sys Dk Blue
                       0x80, 0x00, 0x80, 0x00,            //      5  Sys Dk Violet
                       0x00, 0x80, 0x80, 0x00,            //      6  Sys Dk Cyan
                       0xC0, 0xC0, 0xC0, 0x00,            //      7  Sys Lt Gray, gray 192
                    
                      // The following two system entries are modified for the desktop.
                       0xC0, 0xDC, 0xC0, 0x00,            //      8  Sys 8 - VARIABLE
                       0xA6, 0xCA, 0xF0, 0x00,            //      9  Sys 9 - VARIABLE
                    
                      // Gray scale entries (dark)
                       0x04, 0x04, 0x04, PC_NOCOLLAPSE,   //     10  Gray  4
                       0x08, 0x08, 0x08, PC_NOCOLLAPSE,   //     11  Gray  8
                       0x0C, 0x0C, 0x0C, PC_NOCOLLAPSE,   //     12  Gray 12
                       0x11, 0x11, 0x11, PC_NOCOLLAPSE,   //     13  Gray 17
                       0x16, 0x16, 0x16, PC_NOCOLLAPSE,   //     14  Gray 22
                       0x1C, 0x1C, 0x1C, PC_NOCOLLAPSE,   //     15  Gray 28
                       0x22, 0x22, 0x22, PC_NOCOLLAPSE,   //     16  Gray 34
                       0x29, 0x29, 0x29, PC_NOCOLLAPSE,   //     17  Gray 41
                       0x55, 0x55, 0x55, PC_NOCOLLAPSE,   //     18  Gray 85
                       0x4D, 0x4D, 0x4D, PC_NOCOLLAPSE,   //     19  Gray 77
                       0x42, 0x42, 0x42, PC_NOCOLLAPSE,   //     20  Gray 66
                       0x39, 0x39, 0x39, PC_NOCOLLAPSE,   //     21  Gray 57
                      
                      // Custom app/OS entries
                       0xFF, 0x7C, 0x80, PC_NOCOLLAPSE,   //     22  Salmon
                       0xFF, 0x50, 0x50, PC_NOCOLLAPSE,   //     23  Red
                       0xD6, 0x00, 0x93, PC_NOCOLLAPSE,   //     24  Purple
                       0xCC, 0xEC, 0xFF, PC_NOCOLLAPSE,   //     25  Lt Blue
                       0xEF, 0xD6, 0xC6, PC_NOCOLLAPSE,   //     26  Win95 Tan
                       0xE7, 0xE7, 0xD6, PC_NOCOLLAPSE,   //     27  Win95 Tan
                       0xAD, 0xA9, 0x90, PC_NOCOLLAPSE,   //     28  Win95 Grayish
                    
                      // Halftone palette entries
                       0x33, 0x00, 0x00, PC_NOCOLLAPSE,   //     29  
                       0x66, 0x00, 0x00, PC_NOCOLLAPSE,   //     30
                       0x99, 0x00, 0x00, PC_NOCOLLAPSE,   //     31
                       0xCC, 0x00, 0x00, PC_NOCOLLAPSE,   //     32
                       0x00, 0x33, 0x00, PC_NOCOLLAPSE,   //     33
                       0x33, 0x33, 0x00, PC_NOCOLLAPSE,   //     34
                       0x66, 0x33, 0x00, PC_NOCOLLAPSE,   //     35
                       0x99, 0x33, 0x00, PC_NOCOLLAPSE,   //     36
                       0xCC, 0x33, 0x00, PC_NOCOLLAPSE,   //     37
                       0xFF, 0x33, 0x00, PC_NOCOLLAPSE,   //     38
                       0x00, 0x66, 0x00, PC_NOCOLLAPSE,   //     39
                       0x33, 0x66, 0x00, PC_NOCOLLAPSE,   //     40
                       0x66, 0x66, 0x00, PC_NOCOLLAPSE,   //     41
                       0x99, 0x66, 0x00, PC_NOCOLLAPSE,   //     42
                       0xCC, 0x66, 0x00, PC_NOCOLLAPSE,   //     43
                       0xFF, 0x66, 0x00, PC_NOCOLLAPSE,   //     44
                       0x00, 0x99, 0x00, PC_NOCOLLAPSE,   //     45
                       0x33, 0x99, 0x00, PC_NOCOLLAPSE,   //     46
                       0x66, 0x99, 0x00, PC_NOCOLLAPSE,   //     47
                       0x99, 0x99, 0x00, PC_NOCOLLAPSE,   //     48
                       0xCC, 0x99, 0x00, PC_NOCOLLAPSE,   //     49
                       0xFF, 0x99, 0x00, PC_NOCOLLAPSE,   //     50
                       0x00, 0xCC, 0x00, PC_NOCOLLAPSE,   //     51
                       0x33, 0xCC, 0x00, PC_NOCOLLAPSE,   //     52
                       0x66, 0xCC, 0x00, PC_NOCOLLAPSE,   //     53
                       0x99, 0xCC, 0x00, PC_NOCOLLAPSE,   //     54
                       0xCC, 0xCC, 0x00, PC_NOCOLLAPSE,   //     55
                       0xFF, 0xCC, 0x00, PC_NOCOLLAPSE,   //     56
                       0x66, 0xFF, 0x00, PC_NOCOLLAPSE,   //     57
                       0x99, 0xFF, 0x00, PC_NOCOLLAPSE,   //     58
                       0xCC, 0xFF, 0x00, PC_NOCOLLAPSE,   //     59
                       0x00, 0x00, 0x33, PC_NOCOLLAPSE,   //     60
                       0x33, 0x00, 0x33, PC_NOCOLLAPSE,   //     61
                       0x66, 0x00, 0x33, PC_NOCOLLAPSE,   //     62
                       0x99, 0x00, 0x33, PC_NOCOLLAPSE,   //     63
                       0xCC, 0x00, 0x33, PC_NOCOLLAPSE,   //     64
                       0xFF, 0x00, 0x33, PC_NOCOLLAPSE,   //     65
                       0x00, 0x33, 0x33, PC_NOCOLLAPSE,   //     66
                       0x33, 0x33, 0x33, PC_NOCOLLAPSE,   //     67  Gray 51
                       0x66, 0x33, 0x33, PC_NOCOLLAPSE,   //     68
                       0x99, 0x33, 0x33, PC_NOCOLLAPSE,   //     69
                       0xCC, 0x33, 0x33, PC_NOCOLLAPSE,   //     70
                       0xFF, 0x33, 0x33, PC_NOCOLLAPSE,   //     71
                       0x00, 0x66, 0x33, PC_NOCOLLAPSE,   //     72
                       0x33, 0x66, 0x33, PC_NOCOLLAPSE,   //     73
                       0x66, 0x66, 0x33, PC_NOCOLLAPSE,   //     74
                       0x99, 0x66, 0x33, PC_NOCOLLAPSE,   //     75
                       0xCC, 0x66, 0x33, PC_NOCOLLAPSE,   //     76
                       0xFF, 0x66, 0x33, PC_NOCOLLAPSE,   //     77
                       0x00, 0x99, 0x33, PC_NOCOLLAPSE,   //     78
                       0x33, 0x99, 0x33, PC_NOCOLLAPSE,   //     79
                       0x66, 0x99, 0x33, PC_NOCOLLAPSE,   //     80
                       0x99, 0x99, 0x33, PC_NOCOLLAPSE,   //     81
                       0xCC, 0x99, 0x33, PC_NOCOLLAPSE,   //     82
                       0xFF, 0x99, 0x33, PC_NOCOLLAPSE,   //     83
                       0x00, 0xCC, 0x33, PC_NOCOLLAPSE,   //     84
                       0x33, 0xCC, 0x33, PC_NOCOLLAPSE,   //     85
                       0x66, 0xCC, 0x33, PC_NOCOLLAPSE,   //     86
                       0x99, 0xCC, 0x33, PC_NOCOLLAPSE,   //     87
                       0xCC, 0xCC, 0x33, PC_NOCOLLAPSE,   //     88
                       0xFF, 0xCC, 0x33, PC_NOCOLLAPSE,   //     89
                       0x33, 0xFF, 0x33, PC_NOCOLLAPSE,   //     90
                       0x66, 0xFF, 0x33, PC_NOCOLLAPSE,   //     91
                       0x99, 0xFF, 0x33, PC_NOCOLLAPSE,   //     92
                       0xCC, 0xFF, 0x33, PC_NOCOLLAPSE,   //     93
                       0xFF, 0xFF, 0x33, PC_NOCOLLAPSE,   //     94
                       0x00, 0x00, 0x66, PC_NOCOLLAPSE,   //     95
                       0x33, 0x00, 0x66, PC_NOCOLLAPSE,   //     96
                       0x66, 0x00, 0x66, PC_NOCOLLAPSE,   //     97
                       0x99, 0x00, 0x66, PC_NOCOLLAPSE,   //     98
                       0xCC, 0x00, 0x66, PC_NOCOLLAPSE,   //     99
                       0xFF, 0x00, 0x66, PC_NOCOLLAPSE,   //    100
                       0x00, 0x33, 0x66, PC_NOCOLLAPSE,   //    101
                       0x33, 0x33, 0x66, PC_NOCOLLAPSE,   //    102
                       0x66, 0x33, 0x66, PC_NOCOLLAPSE,   //    103
                       0x99, 0x33, 0x66, PC_NOCOLLAPSE,   //    104
                       0xCC, 0x33, 0x66, PC_NOCOLLAPSE,   //    105
                       0xFF, 0x33, 0x66, PC_NOCOLLAPSE,   //    106
                       0x00, 0x66, 0x66, PC_NOCOLLAPSE,   //    107
                       0x33, 0x66, 0x66, PC_NOCOLLAPSE,   //    108
                       0x66, 0x66, 0x66, PC_NOCOLLAPSE,   //    109  Gray 102
                       0x99, 0x66, 0x66, PC_NOCOLLAPSE,   //    110
                       0xCC, 0x66, 0x66, PC_NOCOLLAPSE,   //    111
                       0x00, 0x99, 0x66, PC_NOCOLLAPSE,   //    112
                       0x33, 0x99, 0x66, PC_NOCOLLAPSE,   //    113
                       0x66, 0x99, 0x66, PC_NOCOLLAPSE,   //    114
                       0x99, 0x99, 0x66, PC_NOCOLLAPSE,   //    115
                       0xCC, 0x99, 0x66, PC_NOCOLLAPSE,   //    116
                       0xFF, 0x99, 0x66, PC_NOCOLLAPSE,   //    117
                       0x00, 0xCC, 0x66, PC_NOCOLLAPSE,   //    118
                       0x33, 0xCC, 0x66, PC_NOCOLLAPSE,   //    119
                       0x99, 0xCC, 0x66, PC_NOCOLLAPSE,   //    120
                       0xCC, 0xCC, 0x66, PC_NOCOLLAPSE,   //    121
                       0xFF, 0xCC, 0x66, PC_NOCOLLAPSE,   //    122
                       0x00, 0xFF, 0x66, PC_NOCOLLAPSE,   //    123
                       0x33, 0xFF, 0x66, PC_NOCOLLAPSE,   //    124
                       0x99, 0xFF, 0x66, PC_NOCOLLAPSE,   //    125
                       0xCC, 0xFF, 0x66, PC_NOCOLLAPSE,   //    126
                       0xFF, 0x00, 0xCC, PC_NOCOLLAPSE,   //    127
                       0xCC, 0x00, 0xFF, PC_NOCOLLAPSE,   //    128
                       0x00, 0x99, 0x99, PC_NOCOLLAPSE,   //    129
                       0x99, 0x33, 0x99, PC_NOCOLLAPSE,   //    130
                       0x99, 0x00, 0x99, PC_NOCOLLAPSE,   //    131
                       0xCC, 0x00, 0x99, PC_NOCOLLAPSE,   //    132
                       0x00, 0x00, 0x99, PC_NOCOLLAPSE,   //    133
                       0x33, 0x33, 0x99, PC_NOCOLLAPSE,   //    134
                       0x66, 0x00, 0x99, PC_NOCOLLAPSE,   //    135
                       0xCC, 0x33, 0x99, PC_NOCOLLAPSE,   //    136
                       0xFF, 0x00, 0x99, PC_NOCOLLAPSE,   //    137
                       0x00, 0x66, 0x99, PC_NOCOLLAPSE,   //    138
                       0x33, 0x66, 0x99, PC_NOCOLLAPSE,   //    139
                       0x66, 0x33, 0x99, PC_NOCOLLAPSE,   //    140
                       0x99, 0x66, 0x99, PC_NOCOLLAPSE,   //    141
                       0xCC, 0x66, 0x99, PC_NOCOLLAPSE,   //    142
                       0xFF, 0x33, 0x99, PC_NOCOLLAPSE,   //    143
                       0x33, 0x99, 0x99, PC_NOCOLLAPSE,   //    144
                       0x66, 0x99, 0x99, PC_NOCOLLAPSE,   //    145
                       0x99, 0x99, 0x99, PC_NOCOLLAPSE,   //    146  Gray 153
                       0xCC, 0x99, 0x99, PC_NOCOLLAPSE,   //    147
                       0xFF, 0x99, 0x99, PC_NOCOLLAPSE,   //    148
                       0x00, 0xCC, 0x99, PC_NOCOLLAPSE,   //    149
                       0x33, 0xCC, 0x99, PC_NOCOLLAPSE,   //    150
                       0x66, 0xCC, 0x66, PC_NOCOLLAPSE,   //    151
                       0x99, 0xCC, 0x99, PC_NOCOLLAPSE,   //    152
                       0xCC, 0xCC, 0x99, PC_NOCOLLAPSE,   //    153
                       0xFF, 0xCC, 0x99, PC_NOCOLLAPSE,   //    154
                       0x00, 0xFF, 0x99, PC_NOCOLLAPSE,   //    155
                       0x33, 0xFF, 0x99, PC_NOCOLLAPSE,   //    156
                       0x66, 0xCC, 0x99, PC_NOCOLLAPSE,   //    157
                       0x99, 0xFF, 0x99, PC_NOCOLLAPSE,   //    158
                       0xCC, 0xFF, 0x99, PC_NOCOLLAPSE,   //    159
                       0xFF, 0xFF, 0x99, PC_NOCOLLAPSE,   //    160
                       0x00, 0x00, 0xCC, PC_NOCOLLAPSE,   //    161
                       0x33, 0x00, 0x99, PC_NOCOLLAPSE,   //    162
                       0x66, 0x00, 0xCC, PC_NOCOLLAPSE,   //    163
                       0x99, 0x00, 0xCC, PC_NOCOLLAPSE,   //    164
                       0xCC, 0x00, 0xCC, PC_NOCOLLAPSE,   //    165
                       0x00, 0x33, 0x99, PC_NOCOLLAPSE,   //    166
                       0x33, 0x33, 0xCC, PC_NOCOLLAPSE,   //    167
                       0x66, 0x33, 0xCC, PC_NOCOLLAPSE,   //    168
                       0x99, 0x33, 0xCC, PC_NOCOLLAPSE,   //    169
                       0xCC, 0x33, 0xCC, PC_NOCOLLAPSE,   //    170
                       0xFF, 0x33, 0xCC, PC_NOCOLLAPSE,   //    171
                       0x00, 0x66, 0xCC, PC_NOCOLLAPSE,   //    172
                       0x33, 0x66, 0xCC, PC_NOCOLLAPSE,   //    173
                       0x66, 0x66, 0x99, PC_NOCOLLAPSE,   //    174
                       0x99, 0x66, 0xCC, PC_NOCOLLAPSE,   //    175
                       0xCC, 0x66, 0xCC, PC_NOCOLLAPSE,   //    176
                       0xFF, 0x66, 0x99, PC_NOCOLLAPSE,   //    177
                       0x00, 0x99, 0xCC, PC_NOCOLLAPSE,   //    178
                       0x33, 0x99, 0xCC, PC_NOCOLLAPSE,   //    179
                       0x66, 0x99, 0xCC, PC_NOCOLLAPSE,   //    180
                       0x99, 0x99, 0xCC, PC_NOCOLLAPSE,   //    181
                       0xCC, 0x99, 0xCC, PC_NOCOLLAPSE,   //    182
                       0xFF, 0x99, 0xCC, PC_NOCOLLAPSE,   //    183
                       0x00, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    184
                       0x33, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    185
                       0x66, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    186
                       0x99, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    187
                       0xCC, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    188  Gray 204
                       0xFF, 0xCC, 0xCC, PC_NOCOLLAPSE,   //    189
                       0x00, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    190
                       0x33, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    191
                       0x66, 0xFF, 0x99, PC_NOCOLLAPSE,   //    192
                       0x99, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    193
                       0xCC, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    194
                       0xFF, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    195
                       0x33, 0x00, 0xCC, PC_NOCOLLAPSE,   //    196
                       0x66, 0x00, 0xFF, PC_NOCOLLAPSE,   //    197
                       0x99, 0x00, 0xFF, PC_NOCOLLAPSE,   //    198
                       0x00, 0x33, 0xCC, PC_NOCOLLAPSE,   //    199
                       0x33, 0x33, 0xFF, PC_NOCOLLAPSE,   //    200
                       0x66, 0x33, 0xFF, PC_NOCOLLAPSE,   //    201
                       0x99, 0x33, 0xFF, PC_NOCOLLAPSE,   //    202
                       0xCC, 0x33, 0xFF, PC_NOCOLLAPSE,   //    203
                       0xFF, 0x33, 0xFF, PC_NOCOLLAPSE,   //    204
                       0x00, 0x66, 0xFF, PC_NOCOLLAPSE,   //    205
                       0x33, 0x66, 0xFF, PC_NOCOLLAPSE,   //    206
                       0x66, 0x66, 0xCC, PC_NOCOLLAPSE,   //    207
                       0x99, 0x66, 0xFF, PC_NOCOLLAPSE,   //    208
                       0xCC, 0x66, 0xFF, PC_NOCOLLAPSE,   //    209
                       0xFF, 0x66, 0xCC, PC_NOCOLLAPSE,   //    210
                       0x00, 0x99, 0xFF, PC_NOCOLLAPSE,   //    211
                       0x33, 0x99, 0xFF, PC_NOCOLLAPSE,   //    212
                       0x66, 0x99, 0xFF, PC_NOCOLLAPSE,   //    213
                       0x99, 0x99, 0xFF, PC_NOCOLLAPSE,   //    214
                       0xCC, 0x99, 0xFF, PC_NOCOLLAPSE,   //    215
                       0xFF, 0x99, 0xFF, PC_NOCOLLAPSE,   //    216
                       0x00, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    217
                       0x33, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    218
                       0x66, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    219
                       0x99, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    220
                       0xCC, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    221
                       0xFF, 0xCC, 0xFF, PC_NOCOLLAPSE,   //    222
                       0x33, 0xFF, 0xFF, PC_NOCOLLAPSE,   //    223
                       0x66, 0xFF, 0xCC, PC_NOCOLLAPSE,   //    224
                       0x99, 0xFF, 0xFF, PC_NOCOLLAPSE,   //    225
                       0xCC, 0xFF, 0xFF, PC_NOCOLLAPSE,   //    226
                       0xFF, 0x66, 0x66, PC_NOCOLLAPSE,   //    227
                       0x66, 0xFF, 0x66, PC_NOCOLLAPSE,   //    228
                       0xFF, 0xFF, 0x66, PC_NOCOLLAPSE,   //    229
                       0x66, 0x66, 0xFF, PC_NOCOLLAPSE,   //    230
                       0xFF, 0x66, 0xFF, PC_NOCOLLAPSE,   //    231
                       0x66, 0xFF, 0xFF, PC_NOCOLLAPSE,   //    232
                      
                      // App custom colors
                       0xA5, 0x00, 0x21, PC_NOCOLLAPSE,   //    233  Brick red
                    
                      // Gray palette
                       0x5F, 0x5F, 0x5F, PC_NOCOLLAPSE,   //    234  Gray 95
                       0x77, 0x77, 0x77, PC_NOCOLLAPSE,   //    235  Gray 119
                       0x86, 0x86, 0x86, PC_NOCOLLAPSE,   //    236  Gray 134
                       0x96, 0x96, 0x96, PC_NOCOLLAPSE,   //    237  Gray 150
                       0xCB, 0xCB, 0xCB, PC_NOCOLLAPSE,   //    238  Gray 203
                       0xB2, 0xB2, 0xB2, PC_NOCOLLAPSE,   //    239  Gray 178
                       0xD7, 0xD7, 0xD7, PC_NOCOLLAPSE,   //    240  Gray 215
                       0xDD, 0xDD, 0xDD, PC_NOCOLLAPSE,   //    241  Gray 221
                       0xE3, 0xE3, 0xE3, PC_NOCOLLAPSE,   //    242  Gray 227
                       0xEA, 0xEA, 0xEA, PC_NOCOLLAPSE,   //    243  Gray 234
                       0xF1, 0xF1, 0xF1, PC_NOCOLLAPSE,   //    244  Gray 241
                       0xF8, 0xF8, 0xF8, PC_NOCOLLAPSE,   //    245  Gray 248
                    
                      // The last 10 system colors
                    
                      // The following two system entries are modified for the desktop.
                       0xFF, 0xFB, 0xF0, 0x00,            //    246  Sys 246 - VARIABLE
                       0xA0, 0xA0, 0xA4, 0x00,            //    247  Sys 247 - VARIABLE
                    
                       0x80, 0x80, 0x80, 0x00,            //    248  Sys Lt Gray, gray 128
                       0xFF, 0x00, 0x00, 0x00,            //    249  Sys Red
                       0x00, 0xFF, 0x00, 0x00,            //    250  Sys Green
                       0xFF, 0xFF, 0x00, 0x00,            //    251  Sys Yellow
                       0x00, 0x00, 0xFF, 0x00,            //    252  Sys Blue
                       0xFF, 0x00, 0xFF, 0x00,            //    253  Sys Violet
                       0x00, 0xFF, 0xFF, 0x00,            //    254  Sys Cyan
                       0xFF, 0xFF, 0xFF, 0x00,            //    255  Sys White, gray 255
                };
            }
        }*/
    }
}
