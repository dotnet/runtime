// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal partial class IntNativeMethods
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        public const int MaxTextLengthInWin9x = 8192;

        public const int
        DT_TOP = 0x00000000,
        DT_LEFT = 0x00000000,
        DT_CENTER = 0x00000001,
        DT_RIGHT = 0x00000002,
        DT_VCENTER = 0x00000004,
        DT_BOTTOM = 0x00000008,
        DT_WORDBREAK = 0x00000010,
        DT_SINGLELINE = 0x00000020,
        DT_EXPANDTABS = 0x00000040,
        DT_TABSTOP = 0x00000080,
        DT_NOCLIP = 0x00000100,
        DT_EXTERNALLEADING = 0x00000200,
        DT_CALCRECT = 0x00000400,
        DT_NOPREFIX = 0x00000800,
        DT_INTERNAL = 0x00001000,
        DT_EDITCONTROL = 0x00002000,
        DT_PATH_ELLIPSIS = 0x00004000,
        DT_END_ELLIPSIS = 0x00008000,
        DT_MODIFYSTRING = 0x00010000,
        DT_RTLREADING = 0x00020000,
        DT_WORD_ELLIPSIS = 0x00040000,
        DT_NOFULLWIDTHCHARBREAK = 0x00080000,
        DT_HIDEPREFIX = 0x00100000,
        DT_PREFIXONLY = 0x00200000,

        DIB_RGB_COLORS = 0,
        BI_BITFIELDS = 3,
        BI_RGB = 0,
        BITMAPINFO_MAX_COLORSIZE = 256,
        SPI_GETICONTITLELOGFONT = 0x001F,
        SPI_GETNONCLIENTMETRICS = 41,
        DEFAULT_GUI_FONT = 17,
        HOLLOW_BRUSH = 5,

        BITSPIXEL = 12,
        ALTERNATE = 1,
        WINDING = 2,

        SRCCOPY = 0x00CC0020,
        SRCPAINT = 0x00EE0086, /* dest = source OR dest           */
        SRCAND = 0x008800C6, /* dest = source AND dest          */
        SRCINVERT = 0x00660046, /* dest = source XOR dest          */
        SRCERASE = 0x00440328, /* dest = source AND (NOT dest )   */
        NOTSRCCOPY = 0x00330008, /* dest = (NOT source)             */
        NOTSRCERASE = 0x001100A6, /* dest = (NOT src) AND (NOT dest) */
        MERGECOPY = 0x00C000CA, /* dest = (source AND pattern)     */
        MERGEPAINT = 0x00BB0226, /* dest = (NOT source) OR dest     */
        PATCOPY = 0x00F00021, /* dest = pattern                  */
        PATPAINT = 0x00FB0A09, /* dest = DPSnoo                   */
        PATINVERT = 0x005A0049, /* dest = pattern XOR dest         */
        DSTINVERT = 0x00550009, /* dest = (NOT dest)               */
        BLACKNESS = 0x00000042, /* dest = BLACK                    */
        WHITENESS = 0x00FF0062, /* dest = WHITE                    */
        CAPTUREBLT = 0x40000000, /* Include layered windows */


        /* FONT WEIGHT (BOLD) VALUES */
        FW_DONTCARE = 0,
        FW_NORMAL = 400,
        FW_BOLD = 700,
        // some others...

        /* FONT CHARACTER SET */
        ANSI_CHARSET = 0,
        DEFAULT_CHARSET = 1,
        // plus others ....

        /* Font OutPrecision */
        OUT_DEFAULT_PRECIS = 0,
        OUT_TT_PRECIS = 4,
        OUT_TT_ONLY_PRECIS = 7,
        // some others...

        /* Font clip precision */
        CLIP_DEFAULT_PRECIS = 0,
        // some others...

        /* Font Quality */
        DEFAULT_QUALITY = 0,
        DRAFT_QUALITY = 1,
        PROOF_QUALITY = 2,
        NONANTIALIASED_QUALITY = 3,
        ANTIALIASED_QUALITY = 4,
        CLEARTYPE_QUALITY = 5,
        CLEARTYPE_NATURAL_QUALITY = 6,

        /* Object Definitions for GetCurrentObject() and others. */
        OBJ_PEN = 1,
        OBJ_BRUSH = 2,
        OBJ_DC = 3,
        OBJ_METADC = 4,
        //      OBJ_PAL       = 5,
        OBJ_FONT = 6,
        OBJ_BITMAP = 7,
        //      OBJ_REGION    = 8,
        //      OBJ_METAFILE  = 9,
        OBJ_MEMDC = 10,
        OBJ_EXTPEN = 11,
        OBJ_ENHMETADC = 12,
        //      OBJ_ENHMETAFILE = 13,
        //      OBJ_COLORSPACE = 14

        // Brush styles
        BS_SOLID = 0,
        BS_HATCHED = 2,
        // BS_PATTERN = 3,
        // some others...

        // Code page
        CP_ACP = 0, // ANSI


        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        FORMAT_MESSAGE_DEFAULT = FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM;
        // some others...

        public enum RegionFlags
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r)
            {
                left = r.Left;
                top = r.Top;
                right = r.Right;
                bottom = r.Bottom;
            }

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(
                    x,
                    y,
                    x + width,
                    y + height);
            }

            public Size Size
            {
                get
                {
                    return new Size(right - left, bottom - top);
                }
            }

            public System.Drawing.Rectangle ToRectangle()
            {
                return new Rectangle(
                    left,
                    top,
                    right - left,
                    bottom - top);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;

            public POINT()
            {
            }

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public System.Drawing.Point ToPoint()
            {
                return new System.Drawing.Point(x, y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DRAWTEXTPARAMS
        {
            private int _cbSize = Marshal.SizeOf(typeof(DRAWTEXTPARAMS));
            public int iTabLength;
            public int iLeftMargin;
            public int iRightMargin;

            /// <devdoc>
            ///     Receives the number of characters processed by DrawTextEx, including white-space characters. 
            ///     The number can be the length of the string or the index of the first line that falls below the drawing area. 
            ///     Note that DrawTextEx always processes the entire string if the DT_NOCLIP formatting flag is specified. 
            /// </devdoc>
            public int uiLengthDrawn;

            public DRAWTEXTPARAMS()
            {
            }
            public DRAWTEXTPARAMS(DRAWTEXTPARAMS original)
            {
                iLeftMargin = original.iLeftMargin;
                iRightMargin = original.iRightMargin;
                iTabLength = original.iTabLength;
            }

            public DRAWTEXTPARAMS(int leftMargin, int rightMargin)
            {
                iLeftMargin = leftMargin;
                iRightMargin = rightMargin;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class LOGBRUSH
        {
            public int lbStyle;
            public int lbColor;
            public int lbHatch;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;

            public LOGFONT()
            {
            }

            public LOGFONT(LOGFONT lf)
            {
                Debug.Assert(lf != null, "lf is null");

                lfHeight = lf.lfHeight;
                lfWidth = lf.lfWidth;
                lfEscapement = lf.lfEscapement;
                lfOrientation = lf.lfOrientation;
                lfWeight = lf.lfWeight;
                lfItalic = lf.lfItalic;
                lfUnderline = lf.lfUnderline;
                lfStrikeOut = lf.lfStrikeOut;
                lfCharSet = lf.lfCharSet;
                lfOutPrecision = lf.lfOutPrecision;
                lfClipPrecision = lf.lfClipPrecision;
                lfQuality = lf.lfQuality;
                lfPitchAndFamily = lf.lfPitchAndFamily;
                lfFaceName = lf.lfFaceName;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct TEXTMETRICA
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public byte tmFirstChar;
            public byte tmLastChar;
            public byte tmDefaultChar;
            public byte tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SIZE
        {
            public int cx;
            public int cy;

            public SIZE()
            {
            }

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }

            public System.Drawing.Size ToSize()
            {
                return new System.Drawing.Size(cx, cy);
            }
        }
    }
}
