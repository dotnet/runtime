// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        internal const int QUERYESCSUPPORT = 8;
        internal const int CHECKJPEGFORMAT = 4119;
        internal const int CHECKPNGFORMAT = 4120;

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial IntPtr CreateCompatibleBitmap(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int width, int height);

        [LibraryImport(Libraries.Gdi32)]
        internal static partial int GetDIBits(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hbm, int arg1, int arg2, IntPtr arg3, ref BITMAPINFO_FLAT bmi, int arg5);

        [LibraryImport(Libraries.Gdi32)]
        internal static partial uint GetPaletteEntries(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hpal, int iStartIndex, int nEntries, byte[] lppe);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial IntPtr CreateDIBSection(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc, ref BITMAPINFO_FLAT bmi, int iUsage, ref IntPtr ppvBits, IntPtr hSection, int dwOffset);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "StartDocW", SetLastError = true)]
        internal static partial int StartDoc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, DOCINFO lpDocInfo);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int StartPage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int EndPage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int AbortDoc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int EndDoc(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "ResetDCW", SetLastError = true)]
        internal static partial IntPtr /*HDC*/ ResetDC(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef /*DEVMODE*/ lpDevMode);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "AddFontResourceExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int AddFontResourceEx(string lpszFilename, int fl, IntPtr pdv);

        internal static int AddFontFile(string fileName)
        {
            return AddFontResourceEx(fileName, /*FR_PRIVATE*/ 0x10, IntPtr.Zero);
        }

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int ExtEscape(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int nEscape, int cbInput, ref int inData, int cbOutput, out int outData);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int ExtEscape(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int nEscape, int cbInput, byte[] inData, int cbOutput, out int outData);

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial int IntersectClipRect(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int x1, int y1, int x2, int y2);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "GetObjectW", SetLastError = true)]
        internal static partial int GetObject(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, int nSize, ref BITMAP bm);

        [LibraryImport(Libraries.Gdi32, EntryPoint = "GetObjectW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetObject(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, int nSize, ref Interop.User32.LOGFONT lf);

        internal static unsafe int GetObject(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hObject, ref Interop.User32.LOGFONT lp)
            => GetObject(hObject, sizeof(Interop.User32.LOGFONT), ref lp);

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAP
        {
            public uint bmType;
            public uint bmWidth;
            public uint bmHeight;
            public uint bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public IntPtr bmBits;
        }

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

#if NET7_0_OR_GREATER
        [NativeMarshalling(typeof(Native))]
#endif
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal sealed class DOCINFO
        {
            internal int cbSize = 20;
            internal string? lpszDocName;
            internal string? lpszOutput;
            internal string? lpszDatatype;
            internal int fwType;

#if NET7_0_OR_GREATER
            [CustomTypeMarshaller(typeof(DOCINFO), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            internal struct Native
            {
                internal int cbSize;
                internal IntPtr lpszDocName;
                internal IntPtr lpszOutput;
                internal IntPtr lpszDatatype;
                internal int fwType;

                public Native(DOCINFO docInfo)
                {
                    cbSize = docInfo.cbSize;
                    lpszDocName = Marshal.StringToCoTaskMemAuto(docInfo.lpszDocName);
                    lpszOutput = Marshal.StringToCoTaskMemAuto(docInfo.lpszOutput);
                    lpszDatatype = Marshal.StringToCoTaskMemAuto(docInfo.lpszDatatype);
                    fwType = docInfo.fwType;
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(lpszDocName);
                    Marshal.FreeCoTaskMem(lpszOutput);
                    Marshal.FreeCoTaskMem(lpszDatatype);
                }
            }
#endif
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public sealed class DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;
            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string? dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmICCManufacturer;
            public int dmICCModel;
            public int dmPanningWidth;
            public int dmPanningHeight;


            public override string ToString()
            {
                return "[DEVMODE: "
                + "dmDeviceName=" + dmDeviceName
                + ", dmSpecVersion=" + dmSpecVersion
                + ", dmDriverVersion=" + dmDriverVersion
                + ", dmSize=" + dmSize
                + ", dmDriverExtra=" + dmDriverExtra
                + ", dmFields=" + dmFields
                + ", dmOrientation=" + dmOrientation
                + ", dmPaperSize=" + dmPaperSize
                + ", dmPaperLength=" + dmPaperLength
                + ", dmPaperWidth=" + dmPaperWidth
                + ", dmScale=" + dmScale
                + ", dmCopies=" + dmCopies
                + ", dmDefaultSource=" + dmDefaultSource
                + ", dmPrintQuality=" + dmPrintQuality
                + ", dmColor=" + dmColor
                + ", dmDuplex=" + dmDuplex
                + ", dmYResolution=" + dmYResolution
                + ", dmTTOption=" + dmTTOption
                + ", dmCollate=" + dmCollate
                + ", dmFormName=" + dmFormName
                + ", dmLogPixels=" + dmLogPixels
                + ", dmBitsPerPel=" + dmBitsPerPel
                + ", dmPelsWidth=" + dmPelsWidth
                + ", dmPelsHeight=" + dmPelsHeight
                + ", dmDisplayFlags=" + dmDisplayFlags
                + ", dmDisplayFrequency=" + dmDisplayFrequency
                + ", dmICMMethod=" + dmICMMethod
                + ", dmICMIntent=" + dmICMIntent
                + ", dmMediaType=" + dmMediaType
                + ", dmDitherType=" + dmDitherType
                + ", dmICCManufacturer=" + dmICCManufacturer
                + ", dmICCModel=" + dmICCModel
                + ", dmPanningWidth=" + dmPanningWidth
                + ", dmPanningHeight=" + dmPanningHeight
                + "]";
            }
        }
    }
}
