// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Drawing
{
    internal static partial class SafeNativeMethods
    {
        // We make this a nested class so that we don't have to initialize GDI+ to access SafeNativeMethods (mostly gdi/user32).
        internal static partial class Gdip
        {
            private static readonly IntPtr s_initToken;
            private const string ThreadDataSlotName = "system.drawing.threaddata";

            static Gdip()
            {
                Debug.Assert(s_initToken == IntPtr.Zero, "GdiplusInitialization: Initialize should not be called more than once in the same domain!");

                PlatformInitialize();

                StartupInput input = StartupInput.GetDefault();

                // GDI+ ref counts multiple calls to Startup in the same process, so calls from multiple
                // domains are ok, just make sure to pair each w/GdiplusShutdown
                int status = GdiplusStartup(out s_initToken, ref input, out StartupOutput output);
                CheckStatus(status);
            }

            /// <summary>
            /// Returns true if GDI+ has been started, but not shut down
            /// </summary>
            internal static bool Initialized => s_initToken != IntPtr.Zero;

            /// <summary>
            /// This property will give us back a hashtable we can use to store all of our static brushes and pens on
            /// a per-thread basis. This way we can avoid 'object in use' crashes when different threads are
            /// referencing the same drawing object.
            /// </summary>
            internal static IDictionary ThreadData
            {
                get
                {
                    LocalDataStoreSlot slot = Thread.GetNamedDataSlot(ThreadDataSlotName);
                    IDictionary threadData = (IDictionary)Thread.GetData(slot);
                    if (threadData == null)
                    {
                        threadData = new Hashtable();
                        Thread.SetData(slot, threadData);
                    }

                    return threadData;
                }
            }

            // Used to ensure static constructor has run.
            internal static void DummyFunction()
            {
            }

            //----------------------------------------------------------------------------------------
            // Status codes
            //----------------------------------------------------------------------------------------
            internal const int Ok = 0;
            internal const int GenericError = 1;
            internal const int InvalidParameter = 2;
            internal const int OutOfMemory = 3;
            internal const int ObjectBusy = 4;
            internal const int InsufficientBuffer = 5;
            internal const int NotImplemented = 6;
            internal const int Win32Error = 7;
            internal const int WrongState = 8;
            internal const int Aborted = 9;
            internal const int FileNotFound = 10;
            internal const int ValueOverflow = 11;
            internal const int AccessDenied = 12;
            internal const int UnknownImageFormat = 13;
            internal const int FontFamilyNotFound = 14;
            internal const int FontStyleNotFound = 15;
            internal const int NotTrueTypeFont = 16;
            internal const int UnsupportedGdiplusVersion = 17;
            internal const int GdiplusNotInitialized = 18;
            internal const int PropertyNotFound = 19;
            internal const int PropertyNotSupported = 20;

            internal static void CheckStatus(int status)
            {
                if (status != Ok)
                    throw StatusException(status);
            }

            internal static Exception StatusException(int status)
            {
                Debug.Assert(status != Ok, "Throwing an exception for an 'Ok' return code");

                switch (status)
                {
                    case GenericError:
                        return new ExternalException(SR.GdiplusGenericError, E_FAIL);
                    case InvalidParameter:
                        return new ArgumentException(SR.GdiplusInvalidParameter);
                    case OutOfMemory:
                        return new OutOfMemoryException(SR.GdiplusOutOfMemory);
                    case ObjectBusy:
                        return new InvalidOperationException(SR.GdiplusObjectBusy);
                    case InsufficientBuffer:
                        return new OutOfMemoryException(SR.GdiplusInsufficientBuffer);
                    case NotImplemented:
                        return new NotImplementedException(SR.GdiplusNotImplemented);
                    case Win32Error:
                        return new ExternalException(SR.GdiplusGenericError, E_FAIL);
                    case WrongState:
                        return new InvalidOperationException(SR.GdiplusWrongState);
                    case Aborted:
                        return new ExternalException(SR.GdiplusAborted, E_ABORT);
                    case FileNotFound:
                        return new FileNotFoundException(SR.GdiplusFileNotFound);
                    case ValueOverflow:
                        return new OverflowException(SR.GdiplusOverflow);
                    case AccessDenied:
                        return new ExternalException(SR.GdiplusAccessDenied, E_ACCESSDENIED);
                    case UnknownImageFormat:
                        return new ArgumentException(SR.GdiplusUnknownImageFormat);
                    case PropertyNotFound:
                        return new ArgumentException(SR.GdiplusPropertyNotFoundError);
                    case PropertyNotSupported:
                        return new ArgumentException(SR.GdiplusPropertyNotSupportedError);

                    case FontFamilyNotFound:
                        Debug.Fail("We should be special casing FontFamilyNotFound so we can provide the font name");
                        return new ArgumentException(SR.Format(SR.GdiplusFontFamilyNotFound, "?"));

                    case FontStyleNotFound:
                        Debug.Fail("We should be special casing FontStyleNotFound so we can provide the font name");
                        return new ArgumentException(SR.Format(SR.GdiplusFontStyleNotFound, "?", "?"));

                    case NotTrueTypeFont:
                        Debug.Fail("We should be special casing NotTrueTypeFont so we can provide the font name");
                        return new ArgumentException(SR.GdiplusNotTrueTypeFont_NoName);

                    case UnsupportedGdiplusVersion:
                        return new ExternalException(SR.GdiplusUnsupportedGdiplusVersion, E_FAIL);

                    case GdiplusNotInitialized:
                        return new ExternalException(SR.GdiplusNotInitialized, E_FAIL);
                }

                return new ExternalException($"{SR.GdiplusUnknown} [{status}]", E_UNEXPECTED);
            }
        }

        public const int ERROR_CANCELLED = 1223;

        public const int
        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        E_NOTIMPL = unchecked((int)0x80004001),
        E_ABORT = unchecked((int)0x80004004),
        E_FAIL = unchecked((int)0x80004005),
        E_ACCESSDENIED = unchecked((int)0x80070005),
        GMEM_MOVEABLE = 0x0002,
        GMEM_ZEROINIT = 0x0040,
        DM_IN_BUFFER = 8,
        DM_OUT_BUFFER = 2,
        DT_PLOTTER = 0,
        DT_RASPRINTER = 2,
        TECHNOLOGY = 2,
        DC_PAPERS = 2,
        DC_PAPERSIZE = 3,
        DC_BINS = 6,
        DC_DUPLEX = 7,
        DC_BINNAMES = 12,
        DC_ENUMRESOLUTIONS = 13,
        DC_PAPERNAMES = 16,
        DC_ORIENTATION = 17,
        DC_COPIES = 18,
        PD_ALLPAGES = 0x00000000,
        PD_SELECTION = 0x00000001,
        PD_PAGENUMS = 0x00000002,
        PD_CURRENTPAGE = 0x00400000,
        PD_RETURNDEFAULT = 0x00000400,
        DI_NORMAL = 0x0003,
        IMAGE_ICON = 1,
        IDI_APPLICATION = 32512,
        IDI_HAND = 32513,
        IDI_QUESTION = 32514,
        IDI_EXCLAMATION = 32515,
        IDI_ASTERISK = 32516,
        IDI_WINLOGO = 32517,
        IDI_WARNING = 32515,
        IDI_ERROR = 32513,
        IDI_INFORMATION = 32516,
        PLANES = 14,
        BITSPIXEL = 12,
        LOGPIXELSX = 88,
        LOGPIXELSY = 90,
        PHYSICALWIDTH = 110,
        PHYSICALHEIGHT = 111,
        PHYSICALOFFSETX = 112,
        PHYSICALOFFSETY = 113,
        VERTRES = 10,
        HORZRES = 8,
        DM_ORIENTATION = 0x00000001,
        DM_PAPERSIZE = 0x00000002,
        DM_PAPERLENGTH = 0x00000004,
        DM_PAPERWIDTH = 0x00000008,
        DM_COPIES = 0x00000100,
        DM_DEFAULTSOURCE = 0x00000200,
        DM_PRINTQUALITY = 0x00000400,
        DM_COLOR = 0x00000800,
        DM_DUPLEX = 0x00001000,
        DM_YRESOLUTION = 0x00002000,
        DM_COLLATE = 0x00008000,
        DMORIENT_PORTRAIT = 1,
        DMORIENT_LANDSCAPE = 2,
        DMPAPER_LETTER = 1,
        DMPAPER_LETTERSMALL = 2,
        DMPAPER_TABLOID = 3,
        DMPAPER_LEDGER = 4,
        DMPAPER_LEGAL = 5,
        DMPAPER_STATEMENT = 6,
        DMPAPER_EXECUTIVE = 7,
        DMPAPER_A3 = 8,
        DMPAPER_A4 = 9,
        DMPAPER_A4SMALL = 10,
        DMPAPER_A5 = 11,
        DMPAPER_B4 = 12,
        DMPAPER_B5 = 13,
        DMPAPER_FOLIO = 14,
        DMPAPER_QUARTO = 15,
        DMPAPER_10X14 = 16,
        DMPAPER_11X17 = 17,
        DMPAPER_NOTE = 18,
        DMPAPER_ENV_9 = 19,
        DMPAPER_ENV_10 = 20,
        DMPAPER_ENV_11 = 21,
        DMPAPER_ENV_12 = 22,
        DMPAPER_ENV_14 = 23,
        DMPAPER_CSHEET = 24,
        DMPAPER_DSHEET = 25,
        DMPAPER_ESHEET = 26,
        DMPAPER_ENV_DL = 27,
        DMPAPER_ENV_C5 = 28,
        DMPAPER_ENV_C3 = 29,
        DMPAPER_ENV_C4 = 30,
        DMPAPER_ENV_C6 = 31,
        DMPAPER_ENV_C65 = 32,
        DMPAPER_ENV_B4 = 33,
        DMPAPER_ENV_B5 = 34,
        DMPAPER_ENV_B6 = 35,
        DMPAPER_ENV_ITALY = 36,
        DMPAPER_ENV_MONARCH = 37,
        DMPAPER_ENV_PERSONAL = 38,
        DMPAPER_FANFOLD_US = 39,
        DMPAPER_FANFOLD_STD_GERMAN = 40,
        DMPAPER_FANFOLD_LGL_GERMAN = 41,
        DMPAPER_ISO_B4 = 42,
        DMPAPER_JAPANESE_POSTCARD = 43,
        DMPAPER_9X11 = 44,
        DMPAPER_10X11 = 45,
        DMPAPER_15X11 = 46,
        DMPAPER_ENV_INVITE = 47,
        DMPAPER_RESERVED_48 = 48,
        DMPAPER_RESERVED_49 = 49,
        DMPAPER_LETTER_EXTRA = 50,
        DMPAPER_LEGAL_EXTRA = 51,
        DMPAPER_TABLOID_EXTRA = 52,
        DMPAPER_A4_EXTRA = 53,
        DMPAPER_LETTER_TRANSVERSE = 54,
        DMPAPER_A4_TRANSVERSE = 55,
        DMPAPER_LETTER_EXTRA_TRANSVERSE = 56,
        DMPAPER_A_PLUS = 57,
        DMPAPER_B_PLUS = 58,
        DMPAPER_LETTER_PLUS = 59,
        DMPAPER_A4_PLUS = 60,
        DMPAPER_A5_TRANSVERSE = 61,
        DMPAPER_B5_TRANSVERSE = 62,
        DMPAPER_A3_EXTRA = 63,
        DMPAPER_A5_EXTRA = 64,
        DMPAPER_B5_EXTRA = 65,
        DMPAPER_A2 = 66,
        DMPAPER_A3_TRANSVERSE = 67,
        DMPAPER_A3_EXTRA_TRANSVERSE = 68,

        // WINVER >= 0x0500
        DMPAPER_DBL_JAPANESE_POSTCARD = 69, /* Japanese Double Postcard 200 x 148 mm */
        DMPAPER_A6 = 70,  /* A6 105 x 148 mm                 */
        DMPAPER_JENV_KAKU2 = 71,  /* Japanese Envelope Kaku #2       */
        DMPAPER_JENV_KAKU3 = 72,  /* Japanese Envelope Kaku #3       */
        DMPAPER_JENV_CHOU3 = 73,  /* Japanese Envelope Chou #3       */
        DMPAPER_JENV_CHOU4 = 74,  /* Japanese Envelope Chou #4       */
        DMPAPER_LETTER_ROTATED = 75,  /* Letter Rotated 11 x 8 1/2 11 in */
        DMPAPER_A3_ROTATED = 76,  /* A3 Rotated 420 x 297 mm         */
        DMPAPER_A4_ROTATED = 77,  /* A4 Rotated 297 x 210 mm         */
        DMPAPER_A5_ROTATED = 78,  /* A5 Rotated 210 x 148 mm         */
        DMPAPER_B4_JIS_ROTATED = 79,  /* B4 (JIS) Rotated 364 x 257 mm   */
        DMPAPER_B5_JIS_ROTATED = 80,  /* B5 (JIS) Rotated 257 x 182 mm   */
        DMPAPER_JAPANESE_POSTCARD_ROTATED = 81, /* Japanese Postcard Rotated 148 x 100 mm */
        DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED = 82, /* Double Japanese Postcard Rotated 148 x 200 mm */
        DMPAPER_A6_ROTATED = 83,  /* A6 Rotated 148 x 105 mm         */
        DMPAPER_JENV_KAKU2_ROTATED = 84,  /* Japanese Envelope Kaku #2 Rotated */
        DMPAPER_JENV_KAKU3_ROTATED = 85,  /* Japanese Envelope Kaku #3 Rotated */
        DMPAPER_JENV_CHOU3_ROTATED = 86,  /* Japanese Envelope Chou #3 Rotated */
        DMPAPER_JENV_CHOU4_ROTATED = 87,  /* Japanese Envelope Chou #4 Rotated */
        DMPAPER_B6_JIS = 88,  /* B6 (JIS) 128 x 182 mm           */
        DMPAPER_B6_JIS_ROTATED = 89,  /* B6 (JIS) Rotated 182 x 128 mm   */
        DMPAPER_12X11 = 90,  /* 12 x 11 in                      */
        DMPAPER_JENV_YOU4 = 91,  /* Japanese Envelope You #4        */
        DMPAPER_JENV_YOU4_ROTATED = 92,  /* Japanese Envelope You #4 Rotated*/
        DMPAPER_P16K = 93,  /* PRC 16K 146 x 215 mm            */
        DMPAPER_P32K = 94,  /* PRC 32K 97 x 151 mm             */
        DMPAPER_P32KBIG = 95,  /* PRC 32K(Big) 97 x 151 mm        */
        DMPAPER_PENV_1 = 96,  /* PRC Envelope #1 102 x 165 mm    */
        DMPAPER_PENV_2 = 97,  /* PRC Envelope #2 102 x 176 mm    */
        DMPAPER_PENV_3 = 98,  /* PRC Envelope #3 125 x 176 mm    */
        DMPAPER_PENV_4 = 99,  /* PRC Envelope #4 110 x 208 mm    */
        DMPAPER_PENV_5 = 100, /* PRC Envelope #5 110 x 220 mm    */
        DMPAPER_PENV_6 = 101, /* PRC Envelope #6 120 x 230 mm    */
        DMPAPER_PENV_7 = 102, /* PRC Envelope #7 160 x 230 mm    */
        DMPAPER_PENV_8 = 103, /* PRC Envelope #8 120 x 309 mm    */
        DMPAPER_PENV_9 = 104, /* PRC Envelope #9 229 x 324 mm    */
        DMPAPER_PENV_10 = 105, /* PRC Envelope #10 324 x 458 mm   */
        DMPAPER_P16K_ROTATED = 106, /* PRC 16K Rotated                 */
        DMPAPER_P32K_ROTATED = 107, /* PRC 32K Rotated                 */
        DMPAPER_P32KBIG_ROTATED = 108, /* PRC 32K(Big) Rotated            */
        DMPAPER_PENV_1_ROTATED = 109, /* PRC Envelope #1 Rotated 165 x 102 mm */
        DMPAPER_PENV_2_ROTATED = 110, /* PRC Envelope #2 Rotated 176 x 102 mm */
        DMPAPER_PENV_3_ROTATED = 111, /* PRC Envelope #3 Rotated 176 x 125 mm */
        DMPAPER_PENV_4_ROTATED = 112, /* PRC Envelope #4 Rotated 208 x 110 mm */
        DMPAPER_PENV_5_ROTATED = 113, /* PRC Envelope #5 Rotated 220 x 110 mm */
        DMPAPER_PENV_6_ROTATED = 114, /* PRC Envelope #6 Rotated 230 x 120 mm */
        DMPAPER_PENV_7_ROTATED = 115, /* PRC Envelope #7 Rotated 230 x 160 mm */
        DMPAPER_PENV_8_ROTATED = 116, /* PRC Envelope #8 Rotated 309 x 120 mm */
        DMPAPER_PENV_9_ROTATED = 117, /* PRC Envelope #9 Rotated 324 x 229 mm */
        DMPAPER_PENV_10_ROTATED = 118, /* PRC Envelope #10 Rotated 458 x 324 mm */

        DMPAPER_LAST = DMPAPER_PENV_10_ROTATED,

        DMBIN_UPPER = 1,
        DMBIN_LOWER = 2,
        DMBIN_MIDDLE = 3,
        DMBIN_MANUAL = 4,
        DMBIN_ENVELOPE = 5,
        DMBIN_ENVMANUAL = 6,
        DMBIN_AUTO = 7,
        DMBIN_TRACTOR = 8,
        DMBIN_SMALLFMT = 9,
        DMBIN_LARGEFMT = 10,
        DMBIN_LARGECAPACITY = 11,
        DMBIN_CASSETTE = 14,
        DMBIN_FORMSOURCE = 15,
        DMBIN_LAST = 15,
        DMBIN_USER = 256,
        DMRES_DRAFT = -1,
        DMRES_LOW = -2,
        DMRES_MEDIUM = -3,
        DMRES_HIGH = -4,
        DMCOLOR_MONOCHROME = 1,
        DMCOLOR_COLOR = 2,
        DMDUP_SIMPLEX = 1,
        DMDUP_VERTICAL = 2,
        DMDUP_HORIZONTAL = 3,

        DMCOLLATE_FALSE = 0,
        DMCOLLATE_TRUE = 1,
        PRINTER_ENUM_LOCAL = 0x00000002,
        PRINTER_ENUM_CONNECTIONS = 0x00000004,
        SM_CXICON = 11,
        SM_CYICON = 12,
        DEFAULT_CHARSET = 1;

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateCompatibleBitmap(HandleRef hDC, int width, int height);

        [DllImport(ExternDll.Gdi32)]
        public static extern int GetDIBits(HandleRef hdc, HandleRef hbm, int arg1, int arg2, IntPtr arg3, ref NativeMethods.BITMAPINFO_FLAT bmi, int arg5);

        [DllImport(ExternDll.Gdi32)]
        public static extern uint GetPaletteEntries(HandleRef hpal, int iStartIndex, int nEntries, byte[] lppe);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateDIBSection(HandleRef hdc, ref NativeMethods.BITMAPINFO_FLAT bmi, int iUsage, ref IntPtr ppvBits, IntPtr hSection, int dwOffset);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int StartDoc(HandleRef hDC, DOCINFO lpDocInfo);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int StartPage(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int EndPage(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int AbortDoc(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int EndDoc(HandleRef hDC);

        [DllImport(ExternDll.Comdlg32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PrintDlg([In, Out] PRINTDLG lppd);

        [DllImport(ExternDll.Comdlg32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PrintDlg([In, Out] PRINTDLGX86 lppd);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int DeviceCapabilities(string pDevice, string pPort, short fwCapabilities, IntPtr pOutput, IntPtr /*DEVMODE*/ pDevMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int EnumPrinters(int flags, string name, int level, IntPtr pPrinterEnum/*buffer*/,
                                              int cbBuf, out int pcbNeeded, out int pcReturned);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr /*HDC*/ ResetDC(HandleRef hDC, HandleRef /*DEVMODE*/ lpDevMode);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetClipRgn(HandleRef hDC, HandleRef hRgn);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int SelectClipRgn(HandleRef hDC, HandleRef hRgn);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int AddFontResourceEx(string lpszFilename, int fl, IntPtr pdv);

        public static int AddFontFile(string fileName)
        {
            return AddFontResourceEx(fileName, /*FR_PRIVATE*/ 0x10, IntPtr.Zero);
        }

        internal static IntPtr SaveClipRgn(IntPtr hDC)
        {
            IntPtr hTempRgn = CreateRectRgn(0, 0, 0, 0);
            IntPtr hSaveRgn = IntPtr.Zero;
            try
            {
                int result = GetClipRgn(new HandleRef(null, hDC), new HandleRef(null, hTempRgn));
                if (result > 0)
                {
                    hSaveRgn = hTempRgn;
                    hTempRgn = IntPtr.Zero;
                }
            }
            finally
            {
                if (hTempRgn != IntPtr.Zero)
                {
                    DeleteObject(new HandleRef(null, hTempRgn));
                }
            }

            return hSaveRgn;
        }

        internal static void RestoreClipRgn(IntPtr hDC, IntPtr hRgn)
        {
            try
            {
                SelectClipRgn(new HandleRef(null, hDC), new HandleRef(null, hRgn));
            }
            finally
            {
                if (hRgn != IntPtr.Zero)
                {
                    DeleteObject(new HandleRef(null, hRgn));
                }
            }
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int ExtEscape(HandleRef hDC, int nEscape, int cbInput, ref int inData, int cbOutput, [Out] out int outData);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int ExtEscape(HandleRef hDC, int nEscape, int cbInput, byte[] inData, int cbOutput, [Out] out int outData);

        public const int QUERYESCSUPPORT = 8, CHECKJPEGFORMAT = 4119, CHECKPNGFORMAT = 4120;

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int IntersectClipRect(HandleRef hDC, int x1, int y1, int x2, int y2);

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, EntryPoint = "GlobalAlloc", CharSet = CharSet.Auto)]
        public static extern IntPtr IntGlobalAlloc(int uFlags, UIntPtr dwBytes); // size should be 32/64bits compatible

        public static IntPtr GlobalAlloc(int uFlags, uint dwBytes)
        {
            return IntGlobalAlloc(uFlags, new UIntPtr(dwBytes));
        }

        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_PROC_NOT_FOUND = 127;

        [StructLayout(LayoutKind.Sequential)]
        public class ENHMETAHEADER
        {
            /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.
            /// Extreme care should be taken if changing the layout of the corresponding managed
            /// structures to minimize the risk of buffer overruns.  The affected managed classes
            /// are the following: ENHMETAHEADER, MetaHeader, MetafileHeaderWmf, MetafileHeaderEmf.
            public int iType;
            public int nSize = 40; // ndirect.DllLib.sizeOf( this )
            // rclBounds was a by-value RECTL structure
            public int rclBounds_left;
            public int rclBounds_top;
            public int rclBounds_right;
            public int rclBounds_bottom;
            // rclFrame was a by-value RECTL structure
            public int rclFrame_left;
            public int rclFrame_top;
            public int rclFrame_right;
            public int rclFrame_bottom;
            public int dSignature;
            public int nVersion;
            public int nBytes;
            public int nRecords;
            public short nHandles;
            public short sReserved;
            public int nDescription;
            public int offDescription;
            public int nPalEntries;
            // szlDevice was a by-value SIZE structure
            public int szlDevice_cx;
            public int szlDevice_cy;
            // szlMillimeters was a by-value SIZE structure
            public int szlMillimeters_cx;
            public int szlMillimeters_cy;
            public int cbPixelFormat;
            public int offPixelFormat;
            public int bOpenGL;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DOCINFO
        {
            public int cbSize = 20;
            public string lpszDocName;
            public string lpszOutput;
            public string lpszDatatype;
            public int fwType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class PRINTDLG
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hDevMode;
            public IntPtr hDevNames;
            public IntPtr hDC;
            public int Flags;
            public short nFromPage;
            public short nToPage;
            public short nMinPage;
            public short nMaxPage;
            public short nCopies;
            public IntPtr hInstance;
            public IntPtr lCustData;
            public IntPtr lpfnPrintHook;
            public IntPtr lpfnSetupHook;
            public string lpPrintTemplateName;
            public string lpSetupTemplateName;
            public IntPtr hPrintTemplate;
            public IntPtr hSetupTemplate;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        public class PRINTDLGX86
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hDevMode;
            public IntPtr hDevNames;
            public IntPtr hDC;
            public int Flags;
            public short nFromPage;
            public short nToPage;
            public short nMinPage;
            public short nMaxPage;
            public short nCopies;
            public IntPtr hInstance;
            public IntPtr lCustData;
            public IntPtr lpfnPrintHook;
            public IntPtr lpfnSetupHook;
            public string lpPrintTemplateName;
            public string lpSetupTemplateName;
            public IntPtr hPrintTemplate;
            public IntPtr hSetupTemplate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONINFO
        {
            public uint fIcon;
            public uint xHotspot;
            public uint yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

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

        [StructLayout(LayoutKind.Sequential)]
        public class BITMAPINFOHEADER
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct LOGFONT
        {
            private const int LF_FACESIZE = 32;

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
            private fixed char _lfFaceName[LF_FACESIZE];
            public Span<char> lfFaceName
            {
                get { fixed (char* c = _lfFaceName) { return new Span<char>(c, LF_FACESIZE); } }
            }

            public override string ToString()
            {
                return
                    "lfHeight=" + lfHeight + ", " +
                    "lfWidth=" + lfWidth + ", " +
                    "lfEscapement=" + lfEscapement + ", " +
                    "lfOrientation=" + lfOrientation + ", " +
                    "lfWeight=" + lfWeight + ", " +
                    "lfItalic=" + lfItalic + ", " +
                    "lfUnderline=" + lfUnderline + ", " +
                    "lfStrikeOut=" + lfStrikeOut + ", " +
                    "lfCharSet=" + lfCharSet + ", " +
                    "lfOutPrecision=" + lfOutPrecision + ", " +
                    "lfClipPrecision=" + lfClipPrecision + ", " +
                    "lfQuality=" + lfQuality + ", " +
                    "lfPitchAndFamily=" + lfPitchAndFamily + ", " +
                    "lfFaceName=" + lfFaceName.ToString();
            }
        }

        // https://devblogs.microsoft.com/oldnewthing/20101018-00/?p=12513
        // https://devblogs.microsoft.com/oldnewthing/20120720-00/?p=7083

        // Needs to be packed to 2 to get ICONDIRENTRY to follow immediately after idCount.
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct ICONDIR
        {
            // Must be 0
            public ushort idReserved;
            // Must be 1
            public ushort idType;
            // Count of entries
            public ushort idCount;
            // First entry (anysize array)
            public ICONDIRENTRY idEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONDIRENTRY
        {
            // Width and height are 1 - 255 or 0 for 256
            public byte bWidth;
            public byte bHeight;
            public byte bColorCount;
            public byte bReserved;
            public ushort wPlanes;
            public ushort wBitCount;
            public uint dwBytesInRes;
            public uint dwImageOffset;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
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
            public string dmFormName;
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

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = CharSet.Auto)]
        internal static extern int IntDeleteObject(HandleRef hObject);

        public static int DeleteObject(HandleRef hObject)
        {
            return IntDeleteObject(hObject);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SelectObject(HandleRef hdc, HandleRef obj);

        [DllImport(ExternDll.User32, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe IntPtr CreateIconFromResourceEx(
            byte* pbIconBits,
            uint cbIconBits,
            bool fIcon,
            int dwVersion,
            int csDesired,
            int cyDesired,
            int flags);

        [DllImport(ExternDll.Shell32, CharSet = CharSet.Unicode)]
        public static extern unsafe IntPtr ExtractAssociatedIcon(HandleRef hInst, char* iconPath, ref int index);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadIcon(HandleRef hInst, IntPtr iconId);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern bool DestroyIcon(HandleRef hIcon);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);

        // GetObject stuff
        [DllImport(ExternDll.Gdi32, SetLastError = true)]
        public static extern int GetObject(HandleRef hObject, int nSize, ref BITMAP bm);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetObject(HandleRef hObject, int nSize, ref LOGFONT lf);

        public static unsafe int GetObject(HandleRef hObject, ref LOGFONT lp)
            => GetObject(hObject, sizeof(LOGFONT), ref lp);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern bool GetIconInfo(HandleRef hIcon, ref ICONINFO info);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool DrawIconEx(HandleRef hDC, int x, int y, HandleRef hIcon, int width, int height, int iStepIfAniCursor, HandleRef hBrushFlickerFree, int diFlags);
    }
}
