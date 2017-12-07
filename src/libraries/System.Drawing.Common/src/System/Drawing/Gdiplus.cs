// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Internal;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Drawing.Internal;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Security;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;

[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+BITMAP.bmBits")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+DIBSECTION.dshSection")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip.initToken")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupInput.DebugEventCallback")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupOutput.hook")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupOutput.unhook")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ICONINFO.hbmColor")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ICONINFO.hbmMask")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.hwnd")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.lParam")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.wParam")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+OBJECTHEADER.pInfo")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PICTDESC.union1")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDC")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDevMode")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDevNames")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hInstance")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hPrintTemplate")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hSetupTemplate")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hwndOwner")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.lCustData")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDC")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDevMode")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDevNames")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hInstance")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hPrintTemplate")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hSetupTemplate")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hwndOwner")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.lCustData")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+StreamConsts..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+POINT..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+LOGPEN..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+DIBSECTION..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Ole..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+CommonHandles..ctor()")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+CommonHandles")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+ENHMETAHEADER")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+StreamConsts")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+Ole")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+Gdip")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ENHMETAHEADER..ctor()")]

namespace System.Drawing
{
    internal partial class SafeNativeMethods
    {
        // We make this a nested class so that we don't have to initialize GDI+ to access SafeNativeMethods (mostly gdi/user32).
        internal partial class Gdip
        {
            private static readonly TraceSwitch s_gdiPlusInitialization = new TraceSwitch("GdiPlusInitialization", "Tracks GDI+ initialization and teardown");

            private static IntPtr s_initToken;
            private const string ThreadDataSlotName = "system.drawing.threaddata";

            static Gdip()
            {
                Debug.Assert(s_initToken == IntPtr.Zero, "GdiplusInitialization: Initialize should not be called more than once in the same domain!");
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Initialize GDI+ [" + AppDomain.CurrentDomain.FriendlyName + "]");
                Debug.Indent();

                s_gdipModule = LoadNativeLibrary();
                LoadSharedFunctionPointers();
                PlatformInitialize();

                StartupInput input = StartupInput.GetDefault();
                StartupOutput output;

                // GDI+ ref counts multiple calls to Startup in the same process, so calls from multiple
                // domains are ok, just make sure to pair each w/GdiplusShutdown
                int status = GdiplusStartup(out s_initToken, ref input, out output);
                CheckStatus(status);

                Debug.Unindent();

                // Sync to event for handling shutdown
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.ProcessExit += new EventHandler(OnProcessExit);

                // Also sync to DomainUnload for non-default domains since they will not get a ProcessExit if
                // they are unloaded prior to ProcessExit (and this object's static fields are scoped to AppDomains, 
                // so we must cleanup on AppDomain shutdown)
                if (!currentDomain.IsDefaultAppDomain())
                {
                    currentDomain.DomainUnload += new EventHandler(OnProcessExit);
                }
            }

            /// <summary>
            /// Returns true if GDI+ has been started, but not shut down
            /// </summary>
            private static bool Initialized => s_initToken != IntPtr.Zero;

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

            // Clean up thread data
            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ClearThreadData()
            {
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Releasing TLS data");
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot(ThreadDataSlotName);
                Thread.SetData(slot, null);
            }

            /// <summary>
            /// Shutsdown GDI+
            /// </summary>            
            private static void Shutdown()
            {
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Shutdown GDI+ [" + AppDomain.CurrentDomain.FriendlyName + "]");
                Debug.Indent();

                if (Initialized)
                {
                    Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Not already shutdown");

                    ClearThreadData();

                    // Due to conditions at shutdown, we can't be sure all objects will be finalized here: e.g. a Global variable 
                    // in the application/domain may still be holding a GDI+ object. If so, calling GdiplusShutdown will free the GDI+ heap,
                    // causing AppVerifier exceptions due to active crit sections. 
                    // For now, we will simply not call shutdown, the resultant heap leak should occur most often during shutdown anyway. 
                    // If GDI+ moves their allocations to the standard heap we can revisit.

#if GDIP_SHUTDOWN
                    // Let any thread data collect and finalize before
                    // we tear down GDI+
                    //
                    Debug.WriteLineIf(GdiPlusInitialization.TraceVerbose, "Running garbage collector");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Shutdown GDI+
                    //
                    Debug.WriteLineIf(GdiPlusInitialization.TraceVerbose, "Instruct GDI+ to shutdown");

                    GdiplusShutdown(new HandleRef(null, initToken));
                    initToken = IntPtr.Zero;
#endif

                    // unhook our shutdown handlers as we do not need to shut down more than once
                    AppDomain currentDomain = AppDomain.CurrentDomain;
                    currentDomain.ProcessExit -= new EventHandler(OnProcessExit);
                    if (!currentDomain.IsDefaultAppDomain())
                    {
                        currentDomain.DomainUnload -= new EventHandler(OnProcessExit);
                    }
                }
                Debug.Unindent();
            }


            // When we get notification that the process/domain is terminating, we will
            // try to shutdown GDI+ if we haven't already.
            [PrePrepareMethod]
            private static void OnProcessExit(object sender, EventArgs e)
            {
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Process exited");
                Shutdown();
            }

            // Used to ensure static constructor has run.
            internal static void DummyFunction()
            {
            }

            //----------------------------------------------------------------------------------------                                                           
            // Initialization methods (GdiplusInit.h)
            //----------------------------------------------------------------------------------------

            internal static int GdipDeletePath(HandleRef path) => Initialized ? IntGdipDeletePath(path) : Ok;
            internal static int GdipDeletePathIter(HandleRef pathIter) => Initialized ? IntGdipDeletePathIter(pathIter) : Ok;
            internal static int GdipDeleteMatrix(HandleRef matrix) => Initialized ? IntGdipDeleteMatrix(matrix) : Ok;
            internal static int GdipDeleteRegion(HandleRef region) => Initialized ? IntGdipDeleteRegion(region) : Ok;
            internal static int GdipDeleteBrush(HandleRef brush) => Initialized ? IntGdipDeleteBrush(brush) : Ok;
            internal static int GdipDeletePen(HandleRef pen) => Initialized ? IntGdipDeletePen(pen) : Ok;
            internal static int GdipDeleteCustomLineCap(HandleRef customCap) => Initialized ? IntGdipDeleteCustomLineCap(customCap) : Ok;
            internal static int GdipDisposeImage(HandleRef image) => Initialized ? IntGdipDisposeImage(image) : Ok;
            internal static int GdipDisposeImageAttributes(HandleRef imageattr) => Initialized ? IntGdipDisposeImageAttributes(imageattr) : Ok;
            internal static int GdipDeleteGraphics(HandleRef graphics) => Initialized ? IntGdipDeleteGraphics(graphics) : Ok;
            internal static int GdipReleaseDC(HandleRef graphics, HandleRef hdc) => Initialized ? IntGdipReleaseDC(graphics, hdc) : Ok;
            internal static int GdipDeletePrivateFontCollection(ref IntPtr fontCollection)
            {
                if (!Initialized)
                {
                    fontCollection = IntPtr.Zero;
                    return Ok;
                }

                return IntGdipDeletePrivateFontCollection(ref fontCollection);
            }
            internal static int GdipDeleteFontFamily(HandleRef fontFamily) => Initialized ? IntGdipDeleteFontFamily(fontFamily) : Ok;
            internal static int GdipDeleteFont(HandleRef font) => Initialized ? IntGdipDeleteFont(font) : Ok;
            internal static int GdipDeleteStringFormat(HandleRef format) => Initialized ? IntGdipDeleteStringFormat(format) : Ok;

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
                {
                    throw StatusException(status);
                }
            }

            internal static Exception StatusException(int status)
            {
                Debug.Assert(status != Ok, "Throwing an exception for an 'Ok' return code");

                switch (status)
                {
                    case GenericError:
                        return new ExternalException(SR.Format(SR.GdiplusGenericError), E_FAIL);
                    case InvalidParameter:
                        return new ArgumentException(SR.Format(SR.GdiplusInvalidParameter));
                    case OutOfMemory:
                        return new OutOfMemoryException(SR.Format(SR.GdiplusOutOfMemory));
                    case ObjectBusy:
                        return new InvalidOperationException(SR.Format(SR.GdiplusObjectBusy));
                    case InsufficientBuffer:
                        return new OutOfMemoryException(SR.Format(SR.GdiplusInsufficientBuffer));
                    case NotImplemented:
                        return new NotImplementedException(SR.Format(SR.GdiplusNotImplemented));
                    case Win32Error:
                        return new ExternalException(SR.Format(SR.GdiplusGenericError), E_FAIL);
                    case WrongState:
                        return new InvalidOperationException(SR.Format(SR.GdiplusWrongState));
                    case Aborted:
                        return new ExternalException(SR.Format(SR.GdiplusAborted), E_ABORT);
                    case FileNotFound:
                        return new FileNotFoundException(SR.Format(SR.GdiplusFileNotFound));
                    case ValueOverflow:
                        return new OverflowException(SR.Format(SR.GdiplusOverflow));
                    case AccessDenied:
                        return new ExternalException(SR.Format(SR.GdiplusAccessDenied), E_ACCESSDENIED);
                    case UnknownImageFormat:
                        return new ArgumentException(SR.Format(SR.GdiplusUnknownImageFormat));
                    case PropertyNotFound:
                        return new ArgumentException(SR.Format(SR.GdiplusPropertyNotFoundError));
                    case PropertyNotSupported:
                        return new ArgumentException(SR.Format(SR.GdiplusPropertyNotSupportedError));

                    case FontFamilyNotFound:
                        Debug.Fail("We should be special casing FontFamilyNotFound so we can provide the font name");
                        return new ArgumentException(SR.Format(SR.GdiplusFontFamilyNotFound, "?"));

                    case FontStyleNotFound:
                        Debug.Fail("We should be special casing FontStyleNotFound so we can provide the font name");
                        return new ArgumentException(SR.Format(SR.GdiplusFontStyleNotFound, "?", "?"));

                    case NotTrueTypeFont:
                        Debug.Fail("We should be special casing NotTrueTypeFont so we can provide the font name");
                        return new ArgumentException(SR.Format(SR.GdiplusNotTrueTypeFont_NoName));

                    case UnsupportedGdiplusVersion:
                        return new ExternalException(SR.Format(SR.GdiplusUnsupportedGdiplusVersion), E_FAIL);

                    case GdiplusNotInitialized:
                        return new ExternalException(SR.Format(SR.GdiplusNotInitialized), E_FAIL);
                }

                return new ExternalException(SR.Format(SR.GdiplusUnknown), E_UNEXPECTED);
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert GpPointF* memory block to PointF[]
            //----------------------------------------------------------------------------------------
            internal static PointF[] ConvertGPPOINTFArrayF(IntPtr memory, int count)
            {
                if (memory == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(memory));
                }

                var points = new PointF[count];
                Type pointType = typeof(GPPOINTF);
                int size = Marshal.SizeOf(pointType);

                for (int index = 0; index < count; index++)
                {
                    var pt = (GPPOINTF)Marshal.PtrToStructure((IntPtr)((long)memory + index * size), pointType);
                    points[index] = new PointF(pt.X, pt.Y);
                }

                return points;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert GpPoint* memory block to Point[]
            //----------------------------------------------------------------------------------------
            internal static Point[] ConvertGPPOINTArray(IntPtr memory, int count)
            {
                if (memory == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(memory));
                }

                var points = new Point[count];
                Type pointType = typeof(GPPOINT);

                int size = Marshal.SizeOf(pointType);

                for (int index = 0; index < count; index++)
                {
                    var pt = (GPPOINT)Marshal.PtrToStructure((IntPtr)((long)memory + index * size), pointType);
                    points[index] = new Point(pt.X, pt.Y);
                }

                return points;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert PointF[] to native memory block GpPointF*
            //----------------------------------------------------------------------------------------
            internal static IntPtr ConvertPointToMemory(PointF[] points)
            {
                if (points == null)
                {
                    throw new ArgumentNullException(nameof(points));
                }

                int size = (int)Marshal.SizeOf(typeof(GPPOINTF));
                int count = points.Length;
                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (int index = 0; index < count; index++)
                {
                    Marshal.StructureToPtr(new GPPOINTF(points[index]), (IntPtr)(checked((long)memory + index * size)), false);
                }

                return memory;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert Point[] to native memory block GpPoint*
            //----------------------------------------------------------------------------------------
            internal static IntPtr ConvertPointToMemory(Point[] points)
            {
                if (points == null)
                {
                    throw new ArgumentNullException(nameof(points));
                }

                int size = Marshal.SizeOf(typeof(GPPOINT));
                int count = points.Length;
                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (int index = 0; index < count; index++)
                {
                    Marshal.StructureToPtr(new GPPOINT(points[index]), (IntPtr)(checked((long)memory + index * size)), false);
                }

                return memory;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert RectangleF[] to native memory block GpRectF*
            //----------------------------------------------------------------------------------------
            internal static IntPtr ConvertRectangleToMemory(RectangleF[] rect)
            {
                if (rect == null)
                {
                    throw new ArgumentNullException(nameof(rect));
                }

                int size = Marshal.SizeOf(typeof(GPRECTF));
                int count = rect.Length;
                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (int index = 0; index < count; index++)
                {
                    Marshal.StructureToPtr(new GPRECTF(rect[index]), (IntPtr)(checked((long)memory + index * size)), false);
                }

                return memory;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert Rectangle[] to native memory block GpRect*
            //----------------------------------------------------------------------------------------
            internal static IntPtr ConvertRectangleToMemory(Rectangle[] rect)
            {
                if (rect == null)
                {
                    throw new ArgumentNullException(nameof(rect));
                }

                int size = (int)Marshal.SizeOf(typeof(GPRECT));
                int count = rect.Length;
                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (int index = 0; index < count; index++)
                {
                    Marshal.StructureToPtr(new GPRECT(rect[index]), (IntPtr)(checked((long)memory + index * size)), false);
                }

                return memory;
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
        SRCCOPY = 0x00CC0020,
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
        SM_CXICON = 11,
        SM_CYICON = 12,
        DEFAULT_CHARSET = 1;

        public const int NOMIRRORBITMAP = unchecked((int)0x80000000); /* Do not Mirror the bitmap in this call */

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleBitmap", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateCompatibleBitmap(HandleRef hDC, int width, int height);

        public static IntPtr CreateCompatibleBitmap(HandleRef hDC, int width, int height)
        {
            return System.Internal.HandleCollector.Add(IntCreateCompatibleBitmap(hDC, width, height), CommonHandles.GDI);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int BitBlt(HandleRef hDC, int x, int y, int nWidth, int nHeight,
                                         HandleRef hSrcDC, int xSrc, int ySrc, int dwRop);

        [DllImport(ExternDll.Gdi32)]
        public static extern int GetDIBits(HandleRef hdc, HandleRef hbm, int arg1, int arg2, IntPtr arg3, ref NativeMethods.BITMAPINFO_FLAT bmi, int arg5);

        [DllImport(ExternDll.Gdi32)]
        public static extern uint GetPaletteEntries(HandleRef hpal, int iStartIndex, int nEntries, byte[] lppe);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateDIBSection", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateDIBSection(HandleRef hdc, ref NativeMethods.BITMAPINFO_FLAT bmi, int iUsage, ref IntPtr ppvBits, IntPtr hSection, int dwOffset);

        public static IntPtr CreateDIBSection(HandleRef hdc, ref NativeMethods.BITMAPINFO_FLAT bmi, int iUsage, ref IntPtr ppvBits, IntPtr hSection, int dwOffset)
        {
            return System.Internal.HandleCollector.Add(IntCreateDIBSection(hdc, ref bmi, iUsage, ref ppvBits, hSection, dwOffset), SafeNativeMethods.CommonHandles.GDI);
        }

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalFree(HandleRef handle);

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

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalLock(HandleRef handle);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr /*HDC*/ ResetDC(HandleRef hDC, HandleRef /*DEVMODE*/ lpDevMode);

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool GlobalUnlock(HandleRef handle);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateRectRgn", CharSet = CharSet.Auto)]
        private static extern IntPtr IntCreateRectRgn(int x1, int y1, int x2, int y2);

        public static IntPtr CreateRectRgn(int x1, int y1, int x2, int y2)
        {
            return System.Internal.HandleCollector.Add(IntCreateRectRgn(x1, y1, x2, y2), SafeNativeMethods.CommonHandles.GDI);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetClipRgn(HandleRef hDC, HandleRef hRgn);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int SelectClipRgn(HandleRef hDC, HandleRef hRgn);

        [SuppressMessage("Microsoft.Security", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
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
        
        static internal unsafe void ZeroMemory(byte* ptr, ulong length)
        {
            byte* end = ptr + length;
            while (ptr != end) *ptr++ = 0;
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
        public class ICONINFO
        {
            public int fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask = IntPtr.Zero;
            public IntPtr hbmColor = IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits = IntPtr.Zero;
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;

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
                    "lfFaceName=" + lfFaceName;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct ICONDIR
        {
            public short idReserved;
            public short idType;
            public short idCount;
            public ICONDIRENTRY idEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ICONDIRENTRY
        {
            public byte bWidth;
            public byte bHeight;
            public byte bColorCount;
            public byte bReserved;
            public short wPlanes;
            public short wBitCount;
            public int dwBytesInRes;
            public int dwImageOffset;
        }

        public class Ole
        {
            public const int PICTYPE_ICON = 3;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PICTDESC
        {
            internal int cbSizeOfStruct;
            public int picType;
            internal IntPtr union1;
            internal int union2;
            internal int union3;

            public static PICTDESC CreateIconPICTDESC(IntPtr hicon)
            {
                return new PICTDESC()
                {
                    cbSizeOfStruct = 12,
                    picType = Ole.PICTYPE_ICON,
                    union1 = hicon
                };
            }
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

        public sealed class CommonHandles
        {
            static CommonHandles()
            {
#if DEBUG
                // Setup the DebugHandleTracker
                DebugHandleTracker.Initialize();
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
#endif
            }

            /// <summary>
            /// Handle type for GDI objects.
            /// </summary>
            public static readonly int GDI = System.Internal.HandleCollector.RegisterType("GDI", 50, 500);

            /// <summary>
            /// Handle type for HDC's that count against the Win98 limit of five DC's. 
            /// HDC's which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
            /// </summary>
            public static readonly int HDC = System.Internal.HandleCollector.RegisterType("HDC", 100, 2); // wait for 2 dc's before collecting

            /// <summary>
            /// Handle type for icons.
            /// </summary>
            public static readonly int Icon = System.Internal.HandleCollector.RegisterType("Icon", 20, 500);

            /// <summary>
            /// Handle type for kernel objects.
            /// </summary>
            public static readonly int Kernel = System.Internal.HandleCollector.RegisterType("Kernel", 0, 1000);

#if DEBUG
            private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
            {
                DebugHandleTracker.CheckLeaks();
            }

            private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
            {
                DebugHandleTracker.CheckLeaks();
            }
#endif
        }

        public class StreamConsts
        {
            public const int STREAM_SEEK_SET = 0x0;
            public const int STREAM_SEEK_CUR = 0x1;
            public const int STREAM_SEEK_END = 0x2;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = CharSet.Auto)]
        internal static extern int IntDeleteObject(HandleRef hObject);

        public static int DeleteObject(HandleRef hObject)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hObject, CommonHandles.GDI);
            return IntDeleteObject(hObject);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SelectObject(HandleRef hdc, HandleRef obj);

        [DllImport(ExternDll.User32, SetLastError = true, EntryPoint = "CreateIconFromResourceEx")]
        private unsafe static extern IntPtr IntCreateIconFromResourceEx(byte* pbIconBits, int cbIconBits, bool fIcon, int dwVersion, int csDesired, int cyDesired, int flags);

        public unsafe static IntPtr CreateIconFromResourceEx(byte* pbIconBits, int cbIconBits, bool fIcon, int dwVersion, int csDesired, int cyDesired, int flags)
        {
            return System.Internal.HandleCollector.Add(IntCreateIconFromResourceEx(pbIconBits, cbIconBits, fIcon, dwVersion, csDesired, cyDesired, flags), SafeNativeMethods.CommonHandles.Icon);
        }

        [DllImport(ExternDll.Shell32, CharSet = CharSet.Auto, BestFitMapping = false, EntryPoint = "ExtractAssociatedIcon")]
        public unsafe static extern IntPtr IntExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index);

        public unsafe static IntPtr ExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index)
        {
            return System.Internal.HandleCollector.Add(IntExtractAssociatedIcon(hInst, iconPath, ref index), CommonHandles.Icon);
        }

        [DllImport(ExternDll.User32, SetLastError = true, EntryPoint = "LoadIcon", CharSet = CharSet.Auto)]
        private static extern IntPtr IntLoadIcon(HandleRef hInst, IntPtr iconId);

        public static IntPtr LoadIcon(HandleRef hInst, int iconId)
        {
            // We only use the case were the low word of the IntPtr is used a resource id but it still has to be an intptr.
            return IntLoadIcon(hInst, new IntPtr(iconId));
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "DestroyIcon", CharSet = CharSet.Auto)]
        private static extern bool IntDestroyIcon(HandleRef hIcon);

        public static bool DestroyIcon(HandleRef hIcon)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hIcon, SafeNativeMethods.CommonHandles.Icon);
            return IntDestroyIcon(hIcon);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "CopyImage", CharSet = CharSet.Auto)]
        private static extern IntPtr IntCopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);

        public static IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags)
        {
            int handleType;
            switch (uType)
            {
                case IMAGE_ICON:
                    handleType = CommonHandles.Icon;
                    break;
                default:
                    handleType = CommonHandles.GDI;
                    break;
            }
            return System.Internal.HandleCollector.Add(IntCopyImage(hImage, uType, cxDesired, cyDesired, fuFlags), handleType);
        }

        // GetObject stuff
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetObject(HandleRef hObject, int nSize, [In, Out] BITMAP bm);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetObject(HandleRef hObject, int nSize, [In, Out] LOGFONT lf);

        public static int GetObject(HandleRef hObject, LOGFONT lp)
        {
            return GetObject(hObject, Marshal.SizeOf(typeof(LOGFONT)), lp);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool GetIconInfo(HandleRef hIcon, [In, Out] ICONINFO info);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool DrawIconEx(HandleRef hDC, int x, int y, HandleRef hIcon, int width, int height, int iStepIfAniCursor, HandleRef hBrushFlickerFree, int diFlags);

#if CUSTOM_MARSHALING_ISTREAM
        [DllImport(ExternDll.Oleaut32, PreserveSig=false)]
        public static extern IPicture OleLoadPictureEx(
                                                        [return: MarshalAs(UnmanagedType.CustomMarshaler,MarshalType="StreamToIStreamMarshaler")] Stream pStream, 
                                                        int lSize, bool fRunmode, ref Guid refiid, int width, int height, int dwFlags);
                                                        
                                                        
#endif
        [DllImport(ExternDll.Oleaut32, PreserveSig = false)]
        public static extern IPicture OleCreatePictureIndirect(SafeNativeMethods.PICTDESC pictdesc, [In]ref Guid refiid, bool fOwn);

        [ComImport()]
        [Guid("7BF80980-BF32-101A-8BBB-00AA00300CAB")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPicture
        {
            IntPtr GetHandle();

            IntPtr GetHPal();

            [return: MarshalAs(UnmanagedType.I2)]
            short GetPictureType();

            int GetWidth();

            int GetHeight();

            void Render();

            void SetHPal([In] IntPtr phpal);

            IntPtr GetCurDC();

            void SelectPicture([In] IntPtr hdcIn,
                               [Out, MarshalAs(UnmanagedType.LPArray)] int[] phdcOut,
                               [Out, MarshalAs(UnmanagedType.LPArray)] int[] phbmpOut);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool GetKeepOriginalFormat();

            void SetKeepOriginalFormat([In, MarshalAs(UnmanagedType.Bool)] bool pfkeep);

            void PictureChanged();

            [PreserveSig]
            int SaveAsFile([In, MarshalAs(UnmanagedType.Interface)] UnsafeNativeMethods.IStream pstm,
                           [In] int fSaveMemCopy,
                           [Out] out int pcbSize);

            int GetAttributes();

            void SetHdc([In] IntPtr hdc);
        }
    }
}
