// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Internal;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System;
using System.IO;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Internal;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Security.Permissions;
using System.Security;
using System.Runtime.ConstrainedExecution;
using System.Globalization;
using System.Runtime.Versioning;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+BITMAP.bmBits")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+DIBSECTION.dshSection")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip.initToken")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupInput.DebugEventCallback")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupOutput.hook")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Gdip+StartupOutput.unhook")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ICONINFO.hbmColor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ICONINFO.hbmMask")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.hwnd")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.lParam")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+MSG.wParam")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+OBJECTHEADER.pInfo")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PICTDESC.union1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDC")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDevMode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hDevNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hInstance")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hPrintTemplate")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hSetupTemplate")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.hwndOwner")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLG.lCustData")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDC")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDevMode")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hDevNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hInstance")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hPrintTemplate")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hSetupTemplate")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.hwndOwner")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "System.Drawing.SafeNativeMethods+PRINTDLGX86.lCustData")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+StreamConsts..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+POINT..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+LOGPEN..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+DIBSECTION..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+Ole..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+CommonHandles..ctor()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+CommonHandles")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+ENHMETAHEADER")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+StreamConsts")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+Ole")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "System.Drawing.SafeNativeMethods+Gdip")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Drawing.SafeNativeMethods+ENHMETAHEADER..ctor()")]

namespace System.Drawing
{
    [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
    internal class SafeNativeMethods
    {
        // we make this a nested class so that we don't have to initialize GDI+ to access SafeNativeMethods (mostly gdi/user32)
        [System.Security.SuppressUnmanagedCodeSecurityAttribute()]
        internal class Gdip
        {
            private static readonly TraceSwitch s_gdiPlusInitialization = new TraceSwitch("GdiPlusInitialization", "Tracks GDI+ initialization and teardown");

            private static IntPtr s_initToken;
            private const string ThreadDataSlotName = "system.drawing.threaddata";

            static Gdip()
            {
                Initialize();
            }

            /// <summary>
            /// Returns true if GDI+ has been started, but not shut down
            /// </summary>
            private static bool Initialized
            {
                get
                {
                    return s_initToken != IntPtr.Zero;
                }
            }

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
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private static void ClearThreadData()
            {
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Releasing TLS data");
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot(ThreadDataSlotName);
                Thread.SetData(slot, null);
            }

            /// <summary>
            /// Initializes GDI+
            /// This should only be called by our constructor (static), we do not expect multiple calls per domain
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals")]
            private static void Initialize()
            {
                Debug.Assert(s_initToken == IntPtr.Zero, "GdiplusInitialization: Initialize should not be called more than once in the same domain!");
                Debug.WriteLineIf(s_gdiPlusInitialization.TraceVerbose, "Initialize GDI+ [" + AppDomain.CurrentDomain.FriendlyName + "]");
                Debug.Indent();

                StartupInput input = StartupInput.GetDefault();
                StartupOutput output;

                // GDI+ ref counts multiple calls to Startup in the same process, so calls from multiple
                // domains are ok, just make sure to pair each w/GdiplusShutdown
                int status = GdiplusStartup(out s_initToken, ref input, out output);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                Debug.Unindent();

                // Sync to event for handling shutdown
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.ProcessExit += new EventHandler(SafeNativeMethods.Gdip.OnProcessExit);

                // Also sync to DomainUnload for non-default domains since they will not get a ProcessExit if
                // they are unloaded prior to ProcessExit (and this object's static fields are scoped to AppDomains, 
                // so we must cleanup on AppDomain shutdown)
                if (!currentDomain.IsDefaultAppDomain())
                {
                    currentDomain.DomainUnload += new EventHandler(SafeNativeMethods.Gdip.OnProcessExit);
                }
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
                    currentDomain.ProcessExit -= new EventHandler(SafeNativeMethods.Gdip.OnProcessExit);
                    if (!currentDomain.IsDefaultAppDomain())
                    {
                        currentDomain.DomainUnload -= new EventHandler(SafeNativeMethods.Gdip.OnProcessExit);
                    }
                }
                Debug.Unindent();
            }


            // When we get notification that the process/domain is terminating, we will
            // try to shutdown GDI+ if we haven't already.
            //
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

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern void GdiplusShutdown(HandleRef token);

            [StructLayout(LayoutKind.Sequential)]
            private struct StartupInput
            {
                public int GdiplusVersion;             // Must be 1

                // public DebugEventProc DebugEventCallback; // Ignored on free builds
                public IntPtr DebugEventCallback;

                public bool SuppressBackgroundThread;     // FALSE unless you're prepared to call 
                // the hook/unhook functions properly

                public bool SuppressExternalCodecs;       // FALSE unless you want GDI+ only to use
                // its internal image codecs.

                public static StartupInput GetDefault()
                {
                    StartupInput result = new StartupInput();
                    result.GdiplusVersion = 1;
                    // result.DebugEventCallback = null;
                    result.SuppressBackgroundThread = false;
                    result.SuppressExternalCodecs = false;
                    return result;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct StartupOutput
            {
                // The following 2 fields won't be used.  They were originally intended 
                // for getting GDI+ to run on our thread - however there are marshalling
                // dealing with function *'s and what not - so we make explicit calls
                // to gdi+ after the fact, via the GdiplusNotificationHook and 
                // GdiplusNotificationUnhook methods.
                public IntPtr hook;//not used
                public IntPtr unhook;//not used.
            }

            private enum DebugEventLevel
            {
                Fatal,
                Warning,
            }


            // private delegate void DebugEventProc(DebugEventLevel level, /* char* */ string message);

            //----------------------------------------------------------------------------------------                                                           
            // Path methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePath(int brushMode, out IntPtr path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePath2(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePath2I(HandleRef points, HandleRef types, int count, int brushMode, out IntPtr path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipClonePath(HandleRef path, out IntPtr clonepath);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeletePath", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeletePath(HandleRef path);
            internal static int GdipDeletePath(HandleRef path)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeletePath(path);
                return result;
            }


            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetPath(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPointCount(HandleRef path, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathTypes(HandleRef path, byte[] types, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathPoints(HandleRef path, HandleRef points, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathFillMode(HandleRef path, out int fillmode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathFillMode(HandleRef path, int fillmode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathData(HandleRef path, IntPtr pathData);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipStartPathFigure(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipClosePathFigure(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipClosePathFigures(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathMarker(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipClearPathMarkers(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipReversePath(HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathLastPoint(HandleRef path, GPPOINTF lastPoint);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathLine(HandleRef path, float x1, float y1, float x2,
                                                       float y2);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathLine2(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathArc(HandleRef path, float x, float y, float width,
                                                      float height, float startAngle,
                                                      float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathBezier(HandleRef path, float x1, float y1, float x2,
                                                         float y2, float x3, float y3, float x4,
                                                         float y4);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathBeziers(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurve(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurve2(HandleRef path, HandleRef memorypts, int count,
                                                         float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurve3(HandleRef path, HandleRef memorypts, int count,
                                                         int offset, int numberOfSegments,
                                                         float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathClosedCurve(HandleRef path, HandleRef memorypts,
                                                              int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathClosedCurve2(HandleRef path, HandleRef memorypts,
                                                               int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathRectangle(HandleRef path, float x, float y, float width,
                                                            float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathRectangles(HandleRef path, HandleRef rects, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathEllipse(HandleRef path, float x, float y,
                                                          float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathPie(HandleRef path, float x, float y, float width,
                                                      float height, float startAngle,
                                                      float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathPolygon(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathPath(HandleRef path, HandleRef addingPath, bool connect);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathString(HandleRef path, string s, int length,
                                                         HandleRef fontFamily, int style, float emSize,
                                                         ref GPRECTF layoutRect, HandleRef format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathStringI(HandleRef path, string s, int length,
                                                          HandleRef fontFamily, int style, float emSize,
                                                          ref GPRECT layoutRect, HandleRef format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathLineI(HandleRef path, int x1, int y1, int x2,
                                                        int y2);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathLine2I(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathArcI(HandleRef path, int x, int y, int width,
                                                       int height, float startAngle,
                                                       float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathBezierI(HandleRef path, int x1, int y1, int x2,
                                                          int y2, int x3, int y3, int x4,
                                                          int y4);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathBeziersI(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurveI(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurve2I(HandleRef path, HandleRef memorypts, int count,
                                                          float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathCurve3I(HandleRef path, HandleRef memorypts, int count,
                                                          int offset, int numberOfSegments,
                                                          float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathClosedCurveI(HandleRef path, HandleRef memorypts,
                                                               int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathClosedCurve2I(HandleRef path, HandleRef memorypts,
                                                                int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathRectangleI(HandleRef path, int x, int y, int width,
                                                             int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathRectanglesI(HandleRef path, HandleRef rects, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathEllipseI(HandleRef path, int x, int y,
                                                           int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathPieI(HandleRef path, int x, int y, int width,
                                                       int height, float startAngle,
                                                       float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipAddPathPolygonI(HandleRef path, HandleRef memorypts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFlattenPath(HandleRef path, HandleRef matrixfloat, float flatness);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipWidenPath(HandleRef path, HandleRef pen, HandleRef matrix, float flatness);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipWarpPath(HandleRef path, HandleRef matrix, HandleRef points, int count,
                                                    float srcX, float srcY, float srcWidth, float srcHeight,
                                                    WarpMode warpMode, float flatness);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformPath(HandleRef path, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathWorldBounds(HandleRef path, ref GPRECTF gprectf, HandleRef matrix, HandleRef pen);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisiblePathPoint(HandleRef path, float x, float y,
                                                              HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisiblePathPointI(HandleRef path, int x, int y,
                                                               HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsOutlineVisiblePathPoint(HandleRef path, float x, float y, HandleRef pen,
                                                                     HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsOutlineVisiblePathPointI(HandleRef path, int x, int y, HandleRef pen,
                                                                      HandleRef graphics, out int boolean);

            //----------------------------------------------------------------------------------------                                                           
            // GraphicsPath Enumeration methods
            //----------------------------------------------------------------------------------------
            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePathIter(out IntPtr pathIter, HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeletePathIter", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeletePathIter(HandleRef pathIter);
            internal static int GdipDeletePathIter(HandleRef pathIter)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeletePathIter(pathIter);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterNextSubpath(HandleRef pathIter, out int resultCount,
                                                               out int startIndex, out int endIndex, out bool isClosed);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterNextSubpathPath(HandleRef pathIter, out int resultCount,
                                                                   HandleRef path, out bool isClosed);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterNextPathType(HandleRef pathIter, out int resultCount,
                                                                out byte pathType, out int startIndex,
                                                                out int endIndex);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterNextMarker(HandleRef pathIter, out int resultCount,
                                                              out int startIndex, out int endIndex);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterNextMarkerPath(HandleRef pathIter, out int resultCount,
                                                                  HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterGetCount(HandleRef pathIter, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterGetSubpathCount(HandleRef pathIter, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterHasCurve(HandleRef pathIter, out bool hasCurve);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterRewind(HandleRef pathIter);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterEnumerate(HandleRef pathIter, out int resultCount,
                                                             IntPtr memoryPts, [In, Out] byte[] types, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPathIterCopyData(HandleRef pathIter, out int resultCount,
                                                            IntPtr memoryPts, [In, Out] byte[] types, int startIndex,
                                                            int endIndex);

            //----------------------------------------------------------------------------------------                                                           
            // Matrix methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMatrix(out IntPtr matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMatrix2(float m11, float m12,
                                                         float m21, float m22, float dx,
                                                         float dy, out IntPtr matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMatrix3(ref GPRECTF rect, HandleRef dstplg, out IntPtr matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMatrix3I(ref GPRECT rect, HandleRef dstplg, out IntPtr matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneMatrix(HandleRef matrix, out IntPtr cloneMatrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteMatrix", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteMatrix(HandleRef matrix);
            internal static int GdipDeleteMatrix(HandleRef matrix)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteMatrix(matrix);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetMatrixElements(HandleRef matrix, float m11,
                                                             float m12, float m21,
                                                             float m22, float dx, float dy);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyMatrix(HandleRef matrix, HandleRef matrix2, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateMatrix(HandleRef matrix, float offsetX,
                                                           float offsetY, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScaleMatrix(HandleRef matrix, float scaleX, float scaleY, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotateMatrix(HandleRef matrix, float angle, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipShearMatrix(HandleRef matrix, float shearX, float shearY, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipInvertMatrix(HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformMatrixPoints(HandleRef matrix, HandleRef pts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformMatrixPointsI(HandleRef matrix, HandleRef pts, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipVectorTransformMatrixPoints(HandleRef matrix, HandleRef pts,
                                                                       int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipVectorTransformMatrixPointsI(HandleRef matrix, HandleRef pts,
                                                                        int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMatrixElements(HandleRef matrix, IntPtr m);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsMatrixInvertible(HandleRef matrix, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsMatrixIdentity(HandleRef matrix, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsMatrixEqual(HandleRef matrix, HandleRef matrix2,
                                                         out int boolean);

            //----------------------------------------------------------------------------------------                                                           
            // Region methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegion(out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegionRect(ref GPRECTF gprectf, out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegionRectI(ref GPRECT gprect, out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegionPath(HandleRef path, out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegionRgnData(byte[] rgndata, int size, out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateRegionHrgn(HandleRef hRgn, out IntPtr region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneRegion(HandleRef region, out IntPtr cloneregion);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteRegion", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteRegion(HandleRef region);
            internal static int GdipDeleteRegion(HandleRef region)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteRegion(region);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetInfinite(HandleRef region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetEmpty(HandleRef region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCombineRegionRect(HandleRef region, ref GPRECTF gprectf, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCombineRegionRectI(HandleRef region, ref GPRECT gprect, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCombineRegionPath(HandleRef region, HandleRef path, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCombineRegionRegion(HandleRef region, HandleRef region2, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateRegion(HandleRef region, float dx, float dy);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateRegionI(HandleRef region, int dx, int dy);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformRegion(HandleRef region, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionBounds(HandleRef region, HandleRef graphics, ref GPRECTF gprectf);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionHRgn(HandleRef region, HandleRef graphics, out IntPtr hrgn);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsEmptyRegion(HandleRef region, HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsInfiniteRegion(HandleRef region, HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsEqualRegion(HandleRef region, HandleRef region2, HandleRef graphics,
                                                         out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionDataSize(HandleRef region, out int bufferSize);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionData(HandleRef region,
                                                         byte[] regionData,
                                                         int bufferSize,
                                                         out int sizeFilled);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRegionPoint(HandleRef region, float X, float Y,
                                                                HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRegionPointI(HandleRef region, int X, int Y,
                                                                 HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRegionRect(HandleRef region, float X, float Y,
                                                               float width, float height,
                                                               HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRegionRectI(HandleRef region, int X, int Y,
                                                                int width, int height,
                                                                HandleRef graphics, out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionScansCount(HandleRef region, out int count, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRegionScans(HandleRef region, IntPtr rects, out int count, HandleRef matrix);


            //----------------------------------------------------------------------------------------                                                           
            // Brush methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneBrush(HandleRef brush, out IntPtr clonebrush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteBrush", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteBrush(HandleRef brush);
            internal static int GdipDeleteBrush(HandleRef brush)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteBrush(brush);
                return result;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Hatch Brush
            //----------------------------------------------------------------------------------------
            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateHatchBrush(int hatchstyle, int forecol, int backcol, out IntPtr brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetHatchStyle(HandleRef brush, out int hatchstyle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetHatchForegroundColor(HandleRef brush, out int forecol);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetHatchBackgroundColor(HandleRef brush, out int backcol);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateTexture(HandleRef bitmap, int wrapmode, out IntPtr texture);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateTexture2(HandleRef bitmap, int wrapmode, float x,
                                                          float y, float width, float height,
                                                          out IntPtr texture);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateTextureIA(HandleRef bitmap, HandleRef imageAttrib,
                                                           float x, float y, float width, float height,
                                                           out IntPtr texture);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateTexture2I(HandleRef bitmap, int wrapmode, int x,
                                                           int y, int width, int height,
                                                           out IntPtr texture);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateTextureIAI(HandleRef bitmap, HandleRef imageAttrib,
                                                            int x, int y, int width, int height,
                                                            out IntPtr texture);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetTextureTransform(HandleRef brush, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetTextureTransform(HandleRef brush, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetTextureTransform(HandleRef brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyTextureTransform(HandleRef brush,
                                                                    HandleRef matrix,
                                                                    MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateTextureTransform(HandleRef brush,
                                                                     float dx,
                                                                     float dy,
                                                                     MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScaleTextureTransform(HandleRef brush,
                                                                 float sx,
                                                                 float sy,
                                                                 MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotateTextureTransform(HandleRef brush,
                                                                  float angle,
                                                                  MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetTextureWrapMode(HandleRef brush, int wrapMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetTextureWrapMode(HandleRef brush, out int wrapMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetTextureImage(HandleRef brush, out IntPtr image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateSolidFill(int color, out IntPtr brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetSolidFillColor(HandleRef brush, int color);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetSolidFillColor(HandleRef brush, out int color);

            //----------------------------------------------------------------------------------------                                                           
            // Linear Gradient Brush
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrush(GPPOINTF point1, GPPOINTF point2, int color1, int color2, int wrapMode, out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrushI(GPPOINT point1, GPPOINT point2, int color1, int color2, int wrapMode, out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrushFromRect(ref GPRECTF rect, int color1, int color2, int lineGradientMode, int wrapMode,
                                                                     out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrushFromRectI(ref GPRECT rect, int color1, int color2, int lineGradientMode, int wrapMode,
                                                                      out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrushFromRectWithAngle(ref GPRECTF rect, int color1, int color2, float angle, bool isAngleScaleable,
                                                                              int wrapMode, out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateLineBrushFromRectWithAngleI(ref GPRECT rect, int color1, int color2, float angle, bool isAngleScaleable,
                                                                               int wrapMode, out IntPtr lineGradient);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineColors(HandleRef brush, int color1, int color2);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineColors(HandleRef brush, int[] colors);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineRect(HandleRef brush, ref GPRECTF gprectf);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineGammaCorrection(HandleRef brush, out bool useGammaCorrection);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineGammaCorrection(HandleRef brush, bool useGammaCorrection);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineSigmaBlend(HandleRef brush, float focus, float scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineLinearBlend(HandleRef brush, float focus, float scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineBlendCount(HandleRef brush, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLinePresetBlendCount(HandleRef brush, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLinePresetBlend(HandleRef brush, IntPtr blend, IntPtr positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLinePresetBlend(HandleRef brush, HandleRef blend, HandleRef positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineWrapMode(HandleRef brush, int wrapMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineWrapMode(HandleRef brush, out int wrapMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetLineTransform(HandleRef brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyLineTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineTransform(HandleRef brush, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetLineTransform(HandleRef brush, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateLineTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScaleLineTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotateLineTransform(HandleRef brush, float angle, MatrixOrder order);

            //----------------------------------------------------------------------------------------                                                           
            // Path Gradient Brush
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePathGradient(HandleRef points, int count, int wrapMode, out IntPtr brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePathGradientI(HandleRef points, int count, int wrapMode, out IntPtr brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePathGradientFromPath(HandleRef path, out IntPtr brush);


            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientCenterColor(HandleRef brush,
                                                                      out int color);


            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientCenterColor(HandleRef brush,
                                                                      int color);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientSurroundColorsWithCount(HandleRef brush,
                                                                                  int[] color,
                                                                                  ref int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientSurroundColorsWithCount(HandleRef brush,
                                                                                  int[] argb,
                                                                                  ref int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientCenterPoint(HandleRef brush,
                                                                      GPPOINTF point);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientCenterPoint(HandleRef brush,
                                                                      GPPOINTF point);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientRect(HandleRef brush,
                                                               ref GPRECTF gprectf);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientPointCount(HandleRef brush,
                                                                     out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientSurroundColorCount(HandleRef brush,
                                                                             out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientBlendCount(HandleRef brush,
                                                                     out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientBlend(HandleRef brush,
                                                                IntPtr blend,
                                                                IntPtr positions,
                                                                int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientBlend(HandleRef brush,
                                                                HandleRef blend,
                                                                HandleRef positions,
                                                                int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientPresetBlendCount(HandleRef brush, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientPresetBlend(HandleRef brush, IntPtr blend,
                                                                      IntPtr positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientPresetBlend(HandleRef brush, HandleRef blend,
                                                                      HandleRef positions, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientSigmaBlend(HandleRef brush,
                                                                     float focus,
                                                                     float scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientLinearBlend(HandleRef brush,
                                                                      float focus,
                                                                      float scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientWrapMode(HandleRef brush,
                                                                   int wrapmode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientWrapMode(HandleRef brush,
                                                                   out int wrapmode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientTransform(HandleRef brush,
                                                                    HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientTransform(HandleRef brush,
                                                                    HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetPathGradientTransform(HandleRef brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyPathGradientTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslatePathGradientTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScalePathGradientTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotatePathGradientTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPathGradientFocusScales(HandleRef brush,
                                                                      float[] xScale,
                                                                      float[] yScale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPathGradientFocusScales(HandleRef brush,
                                                                      float xScale,
                                                                      float yScale);

            //----------------------------------------------------------------------------------------                                                           
            // Pen methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePen1(int argb, float width, int unit,
                                                      out IntPtr pen);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreatePen2(HandleRef brush, float width, int unit,
                                                      out IntPtr pen);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipClonePen(HandleRef pen, out IntPtr clonepen);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeletePen", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeletePen(HandleRef Pen);
            internal static int GdipDeletePen(HandleRef pen)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeletePen(pen);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenMode(HandleRef pen, PenAlignment penAlign);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenMode(HandleRef pen, out PenAlignment penAlign);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenWidth(HandleRef pen, float width);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenWidth(HandleRef pen, float[] width);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenLineCap197819(HandleRef pen, int startCap, int endCap, int dashCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenStartCap(HandleRef pen, int startCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenEndCap(HandleRef pen, int endCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenStartCap(HandleRef pen, out int startCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenEndCap(HandleRef pen, out int endCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenDashCap197819(HandleRef pen, out int dashCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenDashCap197819(HandleRef pen, int dashCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenLineJoin(HandleRef pen, int lineJoin);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenLineJoin(HandleRef pen, out int lineJoin);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenCustomStartCap(HandleRef pen, HandleRef customCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenCustomStartCap(HandleRef pen, out IntPtr customCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenCustomEndCap(HandleRef pen, HandleRef customCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenCustomEndCap(HandleRef pen, out IntPtr customCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenMiterLimit(HandleRef pen, float miterLimit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenMiterLimit(HandleRef pen, float[] miterLimit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenTransform(HandleRef pen, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenTransform(HandleRef pen, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetPenTransform(HandleRef brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyPenTransform(HandleRef brush, HandleRef matrix, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslatePenTransform(HandleRef brush, float dx, float dy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScalePenTransform(HandleRef brush, float sx, float sy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotatePenTransform(HandleRef brush, float angle, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenColor(HandleRef pen, int argb);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenColor(HandleRef pen, out int argb);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenBrushFill(HandleRef pen, HandleRef brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenBrushFill(HandleRef pen, out IntPtr brush);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenFillType(HandleRef pen, out int pentype);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenDashStyle(HandleRef pen, out int dashstyle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenDashStyle(HandleRef pen, int dashstyle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenDashArray(HandleRef pen, HandleRef memorydash, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenDashOffset(HandleRef pen, float[] dashoffset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenDashOffset(HandleRef pen, float dashoffset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenDashCount(HandleRef pen, out int dashcount);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenDashArray(HandleRef pen, IntPtr memorydash, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenCompoundCount(HandleRef pen, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPenCompoundArray(HandleRef pen, float[] array, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPenCompoundArray(HandleRef pen, float[] array, int count);

            //----------------------------------------------------------------------------------------                                                           
            // CustomLineCap methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateCustomLineCap(HandleRef fillpath, HandleRef strokepath, LineCap baseCap, float baseInset, out IntPtr customCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteCustomLineCap", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteCustomLineCap(HandleRef customCap);
            internal static int GdipDeleteCustomLineCap(HandleRef customCap)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteCustomLineCap(customCap);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneCustomLineCap(HandleRef customCap, out IntPtr clonedCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapType(HandleRef customCap,
                                                                out CustomLineCapType capType);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCustomLineCapStrokeCaps(HandleRef customCap,
                                                                      LineCap startCap,
                                                                      LineCap endCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapStrokeCaps(HandleRef customCap,
                                                                      out LineCap startCap,
                                                                      out LineCap endCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCustomLineCapStrokeJoin(HandleRef customCap,
                                                                      LineJoin lineJoin);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapStrokeJoin(HandleRef customCap,
                                                                      out LineJoin lineJoin);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCustomLineCapBaseCap(HandleRef customCap,
                                                                   LineCap baseCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapBaseCap(HandleRef customCap,
                                                                   out LineCap baseCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCustomLineCapBaseInset(HandleRef customCap,
                                                                     float inset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapBaseInset(HandleRef customCap,
                                                                     out float inset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCustomLineCapWidthScale(HandleRef customCap,
                                                                      float widthScale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCustomLineCapWidthScale(HandleRef customCap,
                                                                      out float widthScale);

            //----------------------------------------------------------------------------------------                                                           
            // AdjustableArrowCap methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateAdjustableArrowCap(float height, float width, bool isFilled, out IntPtr adjustableArrowCap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetAdjustableArrowCapHeight(HandleRef adjustableArrowCap,
                                                                       float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetAdjustableArrowCapHeight(HandleRef adjustableArrowCap,
                                                                       out float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetAdjustableArrowCapWidth(HandleRef adjustableArrowCap,
                                                                      float width);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetAdjustableArrowCapWidth(HandleRef adjustableArrowCap,
                                                                      out float width);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap,
                                                                            float middleInset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetAdjustableArrowCapMiddleInset(HandleRef adjustableArrowCap,
                                                                            out float middleInset);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetAdjustableArrowCapFillState(HandleRef adjustableArrowCap,
                                                                          bool fillState);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetAdjustableArrowCapFillState(HandleRef adjustableArrowCap,
                                                                          out bool fillState);

            //----------------------------------------------------------------------------------------                                                           
            // Image methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipLoadImageFromStream(UnsafeNativeMethods.IStream stream, out IntPtr image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipLoadImageFromFile(string filename, out IntPtr image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipLoadImageFromStreamICM(UnsafeNativeMethods.IStream stream, out IntPtr image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipLoadImageFromFileICM(string filename, out IntPtr image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneImage(HandleRef image, out IntPtr cloneimage);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDisposeImage", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDisposeImage(HandleRef image);
            internal static int GdipDisposeImage(HandleRef image)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDisposeImage(image);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSaveImageToFile(HandleRef image, string filename,
                                                           ref Guid classId, HandleRef encoderParams);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSaveImageToStream(HandleRef image, UnsafeNativeMethods.IStream stream,
                                                             ref Guid classId, HandleRef encoderParams);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSaveAdd(HandleRef image, HandleRef encoderParams);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSaveAddImage(HandleRef image, HandleRef newImage, HandleRef encoderParams);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageGraphicsContext(HandleRef image, out IntPtr graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageBounds(HandleRef image, ref GPRECTF gprectf, out GraphicsUnit unit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageDimension(HandleRef image, out float width, out float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageType(HandleRef image, out int type);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageWidth(HandleRef image, out int width);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageHeight(HandleRef image, out int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageHorizontalResolution(HandleRef image, out float horzRes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageVerticalResolution(HandleRef image, out float vertRes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageFlags(HandleRef image, out int flags);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageRawFormat(HandleRef image, ref Guid format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImagePixelFormat(HandleRef image, out int format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageThumbnail(HandleRef image, int thumbWidth, int thumbHeight,
                                                             out IntPtr thumbImage,
                                                             Image.GetThumbnailImageAbort callback,
                                                             IntPtr callbackdata);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetEncoderParameterListSize(HandleRef image, ref Guid clsid,
                                                                       out int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetEncoderParameterList(HandleRef image, ref Guid clsid, int size,
                                                                   IntPtr buffer);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageGetFrameDimensionsCount(HandleRef image, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageGetFrameDimensionsList(HandleRef image,
                                                                       IntPtr buffer,
                                                                       int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageGetFrameCount(HandleRef image, ref Guid dimensionID, int[] count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageSelectActiveFrame(HandleRef image, ref Guid dimensionID, int frameIndex);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageRotateFlip(HandleRef image, int rotateFlipType);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImagePalette(HandleRef image, IntPtr palette, int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImagePalette(HandleRef image, IntPtr palette);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImagePaletteSize(HandleRef image, out int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPropertyCount(HandleRef image, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPropertyIdList(HandleRef image, int count, int[] list);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPropertyItemSize(HandleRef image, int propid, out int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPropertyItem(HandleRef image, int propid, int size, IntPtr buffer);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPropertySize(HandleRef image, out int totalSize, ref int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetAllPropertyItems(HandleRef image, int totalSize, int count, IntPtr buffer);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRemovePropertyItem(HandleRef image, int propid);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPropertyItem(HandleRef image, PropertyItemInternal propitem);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipImageForceValidation(HandleRef image);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageDecodersSize(out int numDecoders, out int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageEncodersSize(out int numEncoders, out int size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);

            //----------------------------------------------------------------------------------------                                                           
            // Bitmap methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromStream(UnsafeNativeMethods.IStream stream, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromStreamICM(UnsafeNativeMethods.IStream stream, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, HandleRef scan0, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromGraphics(int width, int height, HandleRef graphics, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromHBITMAP(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromHICON(HandleRef hicon, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateBitmapFromResource(HandleRef hresource, HandleRef name, out IntPtr bitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateHBITMAPFromBitmap(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateHICONFromBitmap(HandleRef nativeBitmap, out IntPtr hicon);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneBitmapArea(float x, float y, float width, float height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneBitmapAreaI(int x, int y, int width, int height, int format, HandleRef srcbitmap, out IntPtr dstbitmap);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBitmapLockBits(HandleRef bitmap,
                                                          ref GPRECT rect,
                                                          ImageLockMode flags, // ImageLockMode
                                                          PixelFormat format, // PixelFormat
                                                          [In, Out] BitmapData lockedBitmapData);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBitmapUnlockBits(HandleRef bitmap,
                                                            BitmapData lockedBitmapData);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBitmapGetPixel(HandleRef bitmap, int x, int y, out int argb);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBitmapSetPixel(HandleRef bitmap, int x, int y, int argb);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBitmapSetResolution(HandleRef bitmap, float dpix, float dpiy);

            //----------------------------------------------------------------------------------------                                                           
            // ImageAttributes methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateImageAttributes(out IntPtr imageattr);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneImageAttributes(HandleRef imageattr, out IntPtr cloneImageattr);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDisposeImageAttributes", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDisposeImageAttributes(HandleRef imageattr);
            internal static int GdipDisposeImageAttributes(HandleRef imageattr)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDisposeImageAttributes(imageattr);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesColorMatrix(HandleRef imageattr,
                                                                         ColorAdjustType type,
                                                                         bool enableFlag,
                                                                         ColorMatrix colorMatrix,
                                                                         ColorMatrix grayMatrix,
                                                                         ColorMatrixFlag flags);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesThreshold(HandleRef imageattr,
                                                                       ColorAdjustType type,
                                                                       bool enableFlag,
                                                                       float threshold);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesGamma(HandleRef imageattr,
                                                                   ColorAdjustType type,
                                                                   bool enableFlag,
                                                                   float gamma);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesNoOp(HandleRef imageattr,
                                                                  ColorAdjustType type,
                                                                  bool enableFlag);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesColorKeys(HandleRef imageattr,
                                                                       ColorAdjustType type,
                                                                       bool enableFlag,
                                                                       int colorLow, // yes, ref, not out
                                                                       int colorHigh);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesOutputChannel(HandleRef imageattr,
                                                                           ColorAdjustType type,
                                                                           bool enableFlag,
                                                                           ColorChannelFlag flags);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesOutputChannelColorProfile(
                                                                                      HandleRef imageattr,
                                                                                      ColorAdjustType type,
                                                                                      bool enableFlag,
                                                                                      string colorProfileFilename);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesRemapTable(HandleRef imageattr,
                                                                        ColorAdjustType type,
                                                                        bool enableFlag,
                                                                        int mapSize,
                                                                        HandleRef map);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetImageAttributesWrapMode(HandleRef imageattr,
                                                                      int wrapmode,
                                                                      int argb,
                                                                      bool clamp);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetImageAttributesAdjustedPalette(HandleRef imageattr,
                                                                             HandleRef palette,
                                                                             ColorAdjustType type);

            //----------------------------------------------------------------------------------------                                                           
            // Graphics methods
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFlush(HandleRef graphics, FlushIntention intention);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFromHDC(HandleRef hdc, out IntPtr graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFromHDC2(HandleRef hdc, HandleRef hdevice, out IntPtr graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFromHWND(HandleRef hwnd, out IntPtr graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteGraphics", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteGraphics(HandleRef graphics);
            internal static int GdipDeleteGraphics(HandleRef graphics)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteGraphics(graphics);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetDC(HandleRef graphics, out IntPtr hdc);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipReleaseDC", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipReleaseDC(HandleRef graphics, HandleRef hdc);
            internal static int GdipReleaseDC(HandleRef graphics, HandleRef hdc)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipReleaseDC(graphics, hdc);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCompositingMode(HandleRef graphics, int compositeMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetTextRenderingHint(HandleRef graphics, TextRenderingHint textRenderingHint);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetTextContrast(HandleRef graphics, int textContrast);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetInterpolationMode(HandleRef graphics, int mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCompositingMode(HandleRef graphics, out int compositeMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetRenderingOrigin(HandleRef graphics, int x, int y);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetRenderingOrigin(HandleRef graphics, out int x, out int y);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetCompositingQuality(HandleRef graphics, CompositingQuality quality);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCompositingQuality(HandleRef graphics, out CompositingQuality quality);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetSmoothingMode(HandleRef graphics, SmoothingMode smoothingMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetSmoothingMode(HandleRef graphics, out SmoothingMode smoothingMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPixelOffsetMode(HandleRef graphics, PixelOffsetMode pixelOffsetMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPixelOffsetMode(HandleRef graphics, out PixelOffsetMode pixelOffsetMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetTextRenderingHint(HandleRef graphics, out TextRenderingHint textRenderingHint);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetTextContrast(HandleRef graphics, out int textContrast);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetInterpolationMode(HandleRef graphics, out int mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetWorldTransform(HandleRef graphics, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetWorldTransform(HandleRef graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMultiplyWorldTransform(HandleRef graphics, HandleRef matrix, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateWorldTransform(HandleRef graphics, float dx, float dy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipScaleWorldTransform(HandleRef graphics, float sx, float sy, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRotateWorldTransform(HandleRef graphics, float angle, MatrixOrder order);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetWorldTransform(HandleRef graphics, HandleRef matrix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPageUnit(HandleRef graphics, out int unit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetPageScale(HandleRef graphics, float[] scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPageUnit(HandleRef graphics, int unit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetPageScale(HandleRef graphics, float scale);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetDpiX(HandleRef graphics, float[] dpi);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetDpiY(HandleRef graphics, float[] dpi);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformPoints(HandleRef graphics, int destSpace,
                                                           int srcSpace, IntPtr points, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTransformPointsI(HandleRef graphics, int destSpace,
                                                            int srcSpace, IntPtr points, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetNearestColor(HandleRef graphics, ref int color);

            // Create the Win9x Halftone Palette (even on NT) with correct Desktop colors
            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern IntPtr GdipCreateHalftonePalette();

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawLine(HandleRef graphics, HandleRef pen, float x1, float y1,
                                                    float x2, float y2);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawLineI(HandleRef graphics, HandleRef pen, int x1, int y1,
                                                     int x2, int y2);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawLines(HandleRef graphics, HandleRef pen, HandleRef points,
                                                     int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawLinesI(HandleRef graphics, HandleRef pen, HandleRef points,
                                                      int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawArc(HandleRef graphics, HandleRef pen, float x, float y,
                                                   float width, float height, float startAngle,
                                                   float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawArcI(HandleRef graphics, HandleRef pen, int x, int y,
                                                    int width, int height, float startAngle,
                                                    float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawBezier(HandleRef graphics, HandleRef pen, float x1, float y1,
                                                      float x2, float y2, float x3, float y3,
                                                      float x4, float y4);


            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawBeziers(HandleRef graphics, HandleRef pen, HandleRef points,
                                                       int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawBeziersI(HandleRef graphics, HandleRef pen, HandleRef points,
                                                        int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawRectangle(HandleRef graphics, HandleRef pen, float x, float y,
                                                         float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawRectangleI(HandleRef graphics, HandleRef pen, int x, int y,
                                                          int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawRectangles(HandleRef graphics, HandleRef pen, HandleRef rects,
                                                          int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawRectanglesI(HandleRef graphics, HandleRef pen, HandleRef rects,
                                                           int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawEllipse(HandleRef graphics, HandleRef pen, float x, float y,
                                                       float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawEllipseI(HandleRef graphics, HandleRef pen, int x, int y,
                                                        int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawPie(HandleRef graphics, HandleRef pen, float x, float y,
                                                   float width, float height, float startAngle,
                                                   float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawPieI(HandleRef graphics, HandleRef pen, int x, int y,
                                                    int width, int height, float startAngle,
                                                    float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawPolygon(HandleRef graphics, HandleRef pen, HandleRef points,
                                                       int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawPolygonI(HandleRef graphics, HandleRef pen, HandleRef points,
                                                        int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawPath(HandleRef graphics, HandleRef pen, HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurve(HandleRef graphics, HandleRef pen, HandleRef points,
                                                     int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurveI(HandleRef graphics, HandleRef pen, HandleRef points,
                                                      int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurve2(HandleRef graphics, HandleRef pen, HandleRef points,
                                                      int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurve2I(HandleRef graphics, HandleRef pen, HandleRef points,
                                                       int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurve3(HandleRef graphics, HandleRef pen, HandleRef points,
                                                      int count, int offset,
                                                      int numberOfSegments, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawCurve3I(HandleRef graphics, HandleRef pen, HandleRef points,
                                                       int count, int offset,
                                                       int numberOfSegments, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawClosedCurve(HandleRef graphics, HandleRef pen, HandleRef points,
                                                           int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawClosedCurveI(HandleRef graphics, HandleRef pen, HandleRef points,
                                                            int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawClosedCurve2(HandleRef graphics, HandleRef pen, HandleRef points,
                                                            int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawClosedCurve2I(HandleRef graphics, HandleRef pen, HandleRef points,
                                                             int count, float tension);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGraphicsClear(HandleRef graphics, int argb);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillRectangle(HandleRef graphics, HandleRef brush, float x, float y,
                                                         float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillRectangleI(HandleRef graphics, HandleRef brush, int x, int y,
                                                          int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillRectangles(HandleRef graphics, HandleRef brush, HandleRef rects,
                                                          int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillRectanglesI(HandleRef graphics, HandleRef brush, HandleRef rects,
                                                           int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillPolygon(HandleRef graphics, HandleRef brush, HandleRef points,
                                                       int count, int brushMode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillPolygonI(HandleRef graphics, HandleRef brush, HandleRef points,
                                                        int count, int brushMode);


            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillEllipse(HandleRef graphics, HandleRef brush, float x, float y,
                                                       float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillEllipseI(HandleRef graphics, HandleRef brush, int x, int y,
                                                        int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillPie(HandleRef graphics, HandleRef brush, float x, float y,
                                                   float width, float height, float startAngle,
                                                   float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillPieI(HandleRef graphics, HandleRef brush, int x, int y,
                                                    int width, int height, float startAngle,
                                                    float sweepAngle);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillPath(HandleRef graphics, HandleRef brush, HandleRef path);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillClosedCurve(HandleRef graphics, HandleRef brush, HandleRef points,
                                                           int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillClosedCurveI(HandleRef graphics, HandleRef brush, HandleRef points,
                                                            int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillClosedCurve2(HandleRef graphics, HandleRef brush, HandleRef points,
                                                            int count, float tension,
                                                            int mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillClosedCurve2I(HandleRef graphics, HandleRef brush, HandleRef points,
                                                             int count, float tension,
                                                             int mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipFillRegion(HandleRef graphics, HandleRef brush, HandleRef region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImage(HandleRef graphics, HandleRef image, float x, float y);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImageI(HandleRef graphics, HandleRef image, int x, int y);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImageRect(HandleRef graphics, HandleRef image, float x,
                                                         float y, float width, float height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImageRectI(HandleRef graphics, HandleRef image, int x,
                                                          int y, int width, int height);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePoints(HandleRef graphics, HandleRef image,
                                                           HandleRef points, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePointsI(HandleRef graphics, HandleRef image,
                                                            HandleRef points, int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePointRect(HandleRef graphics, HandleRef image, float x,
                                                              float y, float srcx, float srcy,
                                                              float srcwidth, float srcheight,
                                                              int srcunit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePointRectI(HandleRef graphics, HandleRef image, int x,
                                                               int y, int srcx, int srcy,
                                                               int srcwidth, int srcheight,
                                                               int srcunit);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImageRectRect(HandleRef graphics, HandleRef image,
                                                             float dstx, float dsty,
                                                             float dstwidth, float dstheight,
                                                             float srcx, float srcy,
                                                             float srcwidth, float srcheight,
                                                             int srcunit, HandleRef imageAttributes,
                                                             Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImageRectRectI(HandleRef graphics, HandleRef image,
                                                              int dstx, int dsty,
                                                              int dstwidth, int dstheight,
                                                              int srcx, int srcy,
                                                              int srcwidth, int srcheight,
                                                              int srcunit, HandleRef imageAttributes,
                                                              Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePointsRect(HandleRef graphics, HandleRef image,
                                                               HandleRef points, int count, float srcx,
                                                               float srcy, float srcwidth,
                                                               float srcheight, int srcunit,
                                                               HandleRef imageAttributes,
                                                               Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawImagePointsRectI(HandleRef graphics, HandleRef image,
                                                                HandleRef points, int count, int srcx,
                                                                int srcy, int srcwidth,
                                                                int srcheight, int srcunit,
                                                                HandleRef imageAttributes,
                                                                Graphics.DrawImageAbort callback, HandleRef callbackdata);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestPoint(HandleRef graphics,
                                                                      HandleRef metafile,
                                                                      GPPOINTF destPoint,
                                                                      Graphics.EnumerateMetafileProc callback,
                                                                      HandleRef callbackdata,
                                                                      HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestPointI(HandleRef graphics,
                                                                       HandleRef metafile,
                                                                       GPPOINT destPoint,
                                                                       Graphics.EnumerateMetafileProc callback,
                                                                       HandleRef callbackdata,
                                                                       HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestRect(HandleRef graphics,
                                                                     HandleRef metafile,
                                                                     ref GPRECTF destRect,
                                                                     Graphics.EnumerateMetafileProc callback,
                                                                     HandleRef callbackdata,
                                                                     HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestRectI(HandleRef graphics,
                                                                      HandleRef metafile,
                                                                      ref GPRECT destRect,
                                                                      Graphics.EnumerateMetafileProc callback,
                                                                      HandleRef callbackdata,
                                                                      HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestPoints(HandleRef graphics,
                                                                       HandleRef metafile,
                                                                       IntPtr destPoints,
                                                                       int count,
                                                                       Graphics.EnumerateMetafileProc callback,
                                                                       HandleRef callbackdata,
                                                                       HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileDestPointsI(HandleRef graphics,
                                                                        HandleRef metafile,
                                                                        IntPtr destPoints,
                                                                        int count,
                                                                        Graphics.EnumerateMetafileProc callback,
                                                                        HandleRef callbackdata,
                                                                        HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestPoint(HandleRef graphics,
                                                                             HandleRef metafile,
                                                                             GPPOINTF destPoint,
                                                                             ref GPRECTF srcRect,
                                                                             int pageUnit,
                                                                             Graphics.EnumerateMetafileProc callback,
                                                                             HandleRef callbackdata,
                                                                             HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestPointI(HandleRef graphics,
                                                                              HandleRef metafile,
                                                                              GPPOINT destPoint,
                                                                              ref GPRECT srcRect,
                                                                              int pageUnit,
                                                                              Graphics.EnumerateMetafileProc callback,
                                                                              HandleRef callbackdata,
                                                                              HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestRect(HandleRef graphics,
                                                                            HandleRef metafile,
                                                                            ref GPRECTF destRect,
                                                                            ref GPRECTF srcRect,
                                                                            int pageUnit,
                                                                            Graphics.EnumerateMetafileProc callback,
                                                                            HandleRef callbackdata,
                                                                            HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestRectI(HandleRef graphics,
                                                                             HandleRef metafile,
                                                                             ref GPRECT destRect,
                                                                             ref GPRECT srcRect,
                                                                             int pageUnit,
                                                                             Graphics.EnumerateMetafileProc callback,
                                                                             HandleRef callbackdata,
                                                                             HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestPoints(HandleRef graphics,
                                                                              HandleRef metafile,
                                                                              IntPtr destPoints,
                                                                              int count,
                                                                              ref GPRECTF srcRect,
                                                                              int pageUnit,
                                                                              Graphics.EnumerateMetafileProc callback,
                                                                              HandleRef callbackdata,
                                                                              HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEnumerateMetafileSrcRectDestPointsI(HandleRef graphics,
                                                                               HandleRef metafile,
                                                                               IntPtr destPoints,
                                                                               int count,
                                                                               ref GPRECT srcRect,
                                                                               int pageUnit,
                                                                               Graphics.EnumerateMetafileProc callback,
                                                                               HandleRef callbackdata,
                                                                               HandleRef imageattributes);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPlayMetafileRecord(HandleRef graphics,
                                                              EmfPlusRecordType recordType,
                                                              int flags,
                                                              int dataSize,
                                                              byte[] data);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetClipGraphics(HandleRef graphics, HandleRef srcgraphics, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetClipRect(HandleRef graphics, float x, float y,
                                                       float width, float height, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetClipRectI(HandleRef graphics, int x, int y,
                                                        int width, int height, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetClipPath(HandleRef graphics, HandleRef path, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetClipRegion(HandleRef graphics, HandleRef region, CombineMode mode);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipResetClip(HandleRef graphics);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipTranslateClip(HandleRef graphics, float dx, float dy);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetClip(HandleRef graphics, HandleRef region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetClipBounds(HandleRef graphics, ref GPRECTF rect);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsClipEmpty(HandleRef graphics,
                                                       out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetVisibleClipBounds(HandleRef graphics, ref GPRECTF rect);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleClipEmpty(HandleRef graphics,
                                                              out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisiblePoint(HandleRef graphics, float x, float y,
                                                          out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisiblePointI(HandleRef graphics, int x, int y,
                                                           out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRect(HandleRef graphics, float x, float y,
                                                         float width, float height,
                                                         out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsVisibleRectI(HandleRef graphics, int x, int y,
                                                          int width, int height,
                                                          out int boolean);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSaveGraphics(HandleRef graphics, out int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRestoreGraphics(HandleRef graphics, int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBeginContainer(HandleRef graphics, ref GPRECTF dstRect,
                                                          ref GPRECTF srcRect, int unit,
                                                          out int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBeginContainer2(HandleRef graphics, out int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipBeginContainerI(HandleRef graphics, ref GPRECT dstRect,
                                                           ref GPRECT srcRect, int unit,
                                                           out int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipEndContainer(HandleRef graphics, int state);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMetafileHeaderFromWmf(HandleRef hMetafile,      // WMF
                                                                    WmfPlaceableFileHeader wmfplaceable,
                                                                    [In, Out] MetafileHeaderWmf metafileHeaderWmf);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMetafileHeaderFromEmf(HandleRef hEnhMetafile,   // EMF
                                                                    [In, Out] MetafileHeaderEmf metafileHeaderEmf);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMetafileHeaderFromFile(string filename,
                                                                     IntPtr header);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMetafileHeaderFromStream(UnsafeNativeMethods.IStream stream,
                                                                       IntPtr header);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetMetafileHeaderFromMetafile(HandleRef metafile,
                                                                         IntPtr header);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetHemfFromMetafile(HandleRef metafile,
                                                               out IntPtr hEnhMetafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMetafileFromWmf(HandleRef hMetafile, [MarshalAs(UnmanagedType.Bool)]bool deleteWmf, WmfPlaceableFileHeader wmfplacealbeHeader, out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMetafileFromEmf(HandleRef hEnhMetafile, bool deleteEmf, out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateMetafileFromFile(string file, out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode        
            internal static extern int GdipCreateMetafileFromStream(UnsafeNativeMethods.IStream stream, out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafile(HandleRef referenceHdc,
                                                          int emfType,
                                                          ref GPRECTF frameRect,
                                                          int frameUnit,
                                                          string description,
                                                          out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafile(HandleRef referenceHdc,
                                                          int emfType,
                                                          HandleRef pframeRect,
                                                          int frameUnit,
                                                          string description,
                                                          out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileI(HandleRef referenceHdc,
                                                           int emfType,
                                                           ref GPRECT frameRect,
                                                           int frameUnit,
                                                           string description,
                                                           out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileFileName(string fileName,
                                                                  HandleRef referenceHdc,
                                                                  int emfType,
                                                                  ref GPRECTF frameRect,
                                                                  int frameUnit,
                                                                  string description,
                                                                  out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileFileName(string fileName,
                                                                  HandleRef referenceHdc,
                                                                  int emfType,
                                                                  HandleRef pframeRect,
                                                                  int frameUnit,
                                                                  string description,
                                                                  out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileFileNameI(string fileName,
                                                                   HandleRef referenceHdc,
                                                                   int emfType,
                                                                   ref GPRECT frameRect,
                                                                   int frameUnit,
                                                                   string description,
                                                                   out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileStream(UnsafeNativeMethods.IStream stream,
                                                                HandleRef referenceHdc,
                                                                int emfType,
                                                                ref GPRECTF frameRect,
                                                                int frameUnit,
                                                                string description,
                                                                out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileStream(UnsafeNativeMethods.IStream stream,
                                                                HandleRef referenceHdc,
                                                                int emfType,
                                                                HandleRef pframeRect,
                                                                int frameUnit,
                                                                string description,
                                                                out IntPtr metafile);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipRecordMetafileStreamI(UnsafeNativeMethods.IStream stream,
                                                                 HandleRef referenceHdc,
                                                                 int emfType,
                                                                 ref GPRECT frameRect,
                                                                 int frameUnit,
                                                                 string description,
                                                                 out IntPtr metafile);



            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipComment(HandleRef graphics, int sizeData, byte[] data);

            //----------------------------------------------------------------------------------------
            // Font Collection
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipNewInstalledFontCollection(out IntPtr fontCollection);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipNewPrivateFontCollection(out IntPtr fontCollection);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeletePrivateFontCollection", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeletePrivateFontCollection(out IntPtr fontCollection);
            internal static int GdipDeletePrivateFontCollection(out IntPtr fontCollection)
            {
                if (!Initialized)
                {
                    fontCollection = IntPtr.Zero;
                    return SafeNativeMethods.Gdip.Ok;
                }
                int result = IntGdipDeletePrivateFontCollection(out fontCollection);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFontCollectionFamilyCount(HandleRef fontCollection, out int numFound);

            // should be IntPtr
            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFontCollectionFamilyList(HandleRef fontCollection, int numSought, IntPtr[] gpfamilies,
                                                                       out int numFound);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPrivateAddFontFile(HandleRef fontCollection, string filename);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipPrivateAddMemoryFont(HandleRef fontCollection, HandleRef memory, int length);

            //----------------------------------------------------------------------------------------                                                           
            // FontFamily
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFontFamilyFromName(string name, HandleRef fontCollection, out IntPtr FontFamily);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetGenericFontFamilySerif(out IntPtr fontfamily);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetGenericFontFamilyMonospace(out IntPtr fontfamily);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteFontFamily", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteFontFamily(HandleRef fontFamily);
            internal static int GdipDeleteFontFamily(HandleRef fontFamily)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteFontFamily(fontFamily);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneFontFamily(HandleRef fontfamily, out IntPtr clonefontfamily);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFamilyName(HandleRef family, StringBuilder name, int language);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipIsStyleAvailable(HandleRef family, FontStyle style, out int isStyleAvailable);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetEmHeight(HandleRef family, FontStyle style, out int EmHeight);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCellAscent(HandleRef family, FontStyle style, out int CellAscent);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetCellDescent(HandleRef family, FontStyle style, out int CellDescent);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetLineSpacing(HandleRef family, FontStyle style, out int LineSpaceing);
            //----------------------------------------------------------------------------------------                                                           
            // Font      
            //----------------------------------------------------------------------------------------

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFontFromDC(HandleRef hdc, ref IntPtr font);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            internal static extern int GdipCreateFontFromLogfontW(HandleRef hdc, [In, Out, MarshalAs(UnmanagedType.AsAny)] object lf, out IntPtr font);
#pragma warning restore CS0618

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateFont(HandleRef fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            internal static extern int GdipGetLogFontW(HandleRef font, HandleRef graphics, [In, Out, MarshalAs(UnmanagedType.AsAny)] object lf);
#pragma warning restore CS0618

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            internal static extern int GdipCloneFont(HandleRef font, out IntPtr cloneFont);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteFont", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteFont(HandleRef font);
            internal static int GdipDeleteFont(HandleRef font)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteFont(font);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFamily(HandleRef font, out IntPtr family);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            internal static extern int GdipGetFontStyle(HandleRef font, out FontStyle style);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFontSize(HandleRef font, out float size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFontHeight(HandleRef font, HandleRef graphics, out float size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetFontHeightGivenDPI(HandleRef font, float dpi, out float size);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
            internal static extern int GdipGetFontUnit(HandleRef font, out GraphicsUnit unit);

            // Text

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipDrawString(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect,
                                                      HandleRef stringFormat, HandleRef brush);
            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMeasureString(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect,
                                                         HandleRef stringFormat, [In, Out] ref GPRECTF boundingBox, out int codepointsFitted, out int linesFilled);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipMeasureCharacterRanges(HandleRef graphics, string textString, int length, HandleRef font, ref GPRECTF layoutRect, HandleRef stringFormat,
                                                               int characterCount, [In, Out] IntPtr[] region);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatMeasurableCharacterRanges(HandleRef format, int rangeCount, [In, Out] CharacterRange[] range);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCreateStringFormat(StringFormatFlags options, int language, out IntPtr format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipStringFormatGetGenericDefault(out IntPtr format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipStringFormatGetGenericTypographic(out IntPtr format);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDeleteStringFormat", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            private static extern int IntGdipDeleteStringFormat(HandleRef format);
            internal static int GdipDeleteStringFormat(HandleRef format)
            {
                if (!Initialized) return SafeNativeMethods.Gdip.Ok;
                int result = IntGdipDeleteStringFormat(format);
                return result;
            }

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipCloneStringFormat(HandleRef format, out IntPtr newFormat);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatFlags(HandleRef format, StringFormatFlags options);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatFlags(HandleRef format, out StringFormatFlags result);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatAlign(HandleRef format, StringAlignment align);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatAlign(HandleRef format, out StringAlignment align);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatLineAlign(HandleRef format, StringAlignment align);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatLineAlign(HandleRef format, out StringAlignment align);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatHotkeyPrefix(HandleRef format, HotkeyPrefix hotkeyPrefix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatHotkeyPrefix(HandleRef format, out HotkeyPrefix hotkeyPrefix);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatTabStops(HandleRef format, float firstTabOffset, int count, float[] tabStops);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatTabStops(HandleRef format, int count, out float firstTabOffset, [In, Out]float[] tabStops);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatTabStopCount(HandleRef format, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatMeasurableCharacterRangeCount(HandleRef format, out int count);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatTrimming(HandleRef format, StringTrimming trimming);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatTrimming(HandleRef format, out StringTrimming trimming);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipSetStringFormatDigitSubstitution(HandleRef format, int langID, StringDigitSubstitute sds);

            [DllImport(ExternDll.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)] // 3 = Unicode
            internal static extern int GdipGetStringFormatDigitSubstitution(HandleRef format, out int langID, out StringDigitSubstitute sds);

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
                    case GenericError: return new ExternalException(SR.Format(SR.GdiplusGenericError), E_FAIL);
                    case InvalidParameter: return new ArgumentException(SR.Format(SR.GdiplusInvalidParameter));
                    case OutOfMemory: return new OutOfMemoryException(SR.Format(SR.GdiplusOutOfMemory));
                    case ObjectBusy: return new InvalidOperationException(SR.Format(SR.GdiplusObjectBusy));
                    case InsufficientBuffer: return new OutOfMemoryException(SR.Format(SR.GdiplusInsufficientBuffer));
                    case NotImplemented: return new NotImplementedException(SR.Format(SR.GdiplusNotImplemented));
                    case Win32Error: return new ExternalException(SR.Format(SR.GdiplusGenericError), E_FAIL);
                    case WrongState: return new InvalidOperationException(SR.Format(SR.GdiplusWrongState));
                    case Aborted: return new ExternalException(SR.Format(SR.GdiplusAborted), E_ABORT);
                    case FileNotFound: return new FileNotFoundException(SR.Format(SR.GdiplusFileNotFound));
                    case ValueOverflow: return new OverflowException(SR.Format(SR.GdiplusOverflow));
                    case AccessDenied: return new ExternalException(SR.Format(SR.GdiplusAccessDenied), E_ACCESSDENIED);
                    case UnknownImageFormat: return new ArgumentException(SR.Format(SR.GdiplusUnknownImageFormat));
                    case PropertyNotFound: return new ArgumentException(SR.Format(SR.GdiplusPropertyNotFoundError));
                    case PropertyNotSupported: return new ArgumentException(SR.Format(SR.GdiplusPropertyNotSupportedError));


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
                    throw new ArgumentNullException("memory");

                PointF[] points = new PointF[count];

                int index;
                GPPOINTF pt = new GPPOINTF();

                int size = (int)Marshal.SizeOf(pt.GetType());

                for (index = 0; index < count; index++)
                {
                    pt = (GPPOINTF)UnsafeNativeMethods.PtrToStructure((IntPtr)((long)memory + index * size), pt.GetType());
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
                    throw new ArgumentNullException("memory");

                Point[] points = new Point[count];

                int index;
                GPPOINT pt = new GPPOINT();

                int size = (int)Marshal.SizeOf(pt.GetType());

                for (index = 0; index < count; index++)
                {
                    pt = (GPPOINT)UnsafeNativeMethods.PtrToStructure((IntPtr)((long)memory + index * size), pt.GetType());
                    points[index] = new Point((int)pt.X, (int)pt.Y);
                }

                return points;
            }

            //----------------------------------------------------------------------------------------                                                           
            // Helper function:  Convert PointF[] to native memory block GpPointF*
            //----------------------------------------------------------------------------------------
            internal static IntPtr ConvertPointToMemory(PointF[] points)
            {
                if (points == null)
                    throw new ArgumentNullException("points");

                int index;

                int size = (int)Marshal.SizeOf(typeof(GPPOINTF));
                int count = points.Length;

                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (index = 0; index < count; index++)
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
                    throw new ArgumentNullException("points");

                int index;

                int size = (int)Marshal.SizeOf(typeof(GPPOINT));
                int count = points.Length;

                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (index = 0; index < count; index++)
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
                    throw new ArgumentNullException("rect");

                int index;

                int size = (int)Marshal.SizeOf(typeof(GPRECTF));
                int count = rect.Length;

                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (index = 0; index < count; index++)
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
                    throw new ArgumentNullException("rect");

                int index;

                int size = (int)Marshal.SizeOf(typeof(GPRECT));
                int count = rect.Length;

                IntPtr memory = Marshal.AllocHGlobal(checked(count * size));

                for (index = 0; index < count; index++)
                {
                    Marshal.StructureToPtr(new GPRECT(rect[index]), (IntPtr)(checked((long)memory + index * size)), false);
                }

                return memory;
            }
        }

        public static IntPtr InvalidIntPtr = ((IntPtr)((int)(-1)));


        public const int ERROR_CANCELLED = 1223;

        public const int
        RASTERCAPS = 38,
        RC_PALETTE = 0x0100,
        SIZEPALETTE = 104,
        SYSPAL_STATIC = 1,

        BS_SOLID = 0,
        HOLLOW_BRUSH = 5,

        // Binary raster operations.
        R2_BLACK = 1,  /*  0       */
        R2_NOTMERGEPEN = 2,  /* DPon     */
        R2_MASKNOTPEN = 3,  /* DPna     */
        R2_NOTCOPYPEN = 4,  /* PN       */
        R2_MASKPENNOT = 5,  /* PDna     */
        R2_NOT = 6,  /* Dn       */
        R2_XORPEN = 7,  /* DPx      */
        R2_NOTMASKPEN = 8,  /* DPan     */
        R2_MASKPEN = 9,  /* DPa      */
        R2_NOTXORPEN = 10, /* DPxn     */
        R2_NOP = 11, /* D        */
        R2_MERGENOTPEN = 12, /* DPno     */
        R2_COPYPEN = 13, /* P        */
        R2_MERGEPENNOT = 14, /* PDno     */
        R2_MERGEPEN = 15, /* DPo      */
        R2_WHITE = 16, /*  1       */

        UOI_FLAGS = 1,
        WSF_VISIBLE = 0x0001,
        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        E_NOTIMPL = unchecked((int)0x80004001),
        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        E_INVALIDARG = unchecked((int)0x80070057),
        E_NOINTERFACE = unchecked((int)0x80004002),
        E_POINTER = unchecked((int)0x80004003),
        E_HANDLE = unchecked((int)0x80070006),
        E_ABORT = unchecked((int)0x80004004),
        E_FAIL = unchecked((int)0x80004005),
        E_ACCESSDENIED = unchecked((int)0x80070005),
        PM_NOREMOVE = 0x0000,
        PM_REMOVE = 0x0001,
        PM_NOYIELD = 0x0002,
        GMEM_FIXED = 0x0000,
        GMEM_MOVEABLE = 0x0002,
        GMEM_NOCOMPACT = 0x0010,
        GMEM_NODISCARD = 0x0020,
        GMEM_ZEROINIT = 0x0040,
        GMEM_MODIFY = 0x0080,
        GMEM_DISCARDABLE = 0x0100,
        GMEM_NOT_BANKED = 0x1000,
        GMEM_SHARE = 0x2000,
        GMEM_DDESHARE = 0x2000,
        GMEM_NOTIFY = 0x4000,
        GMEM_LOWER = 0x1000,
        GMEM_VALID_FLAGS = 0x7F72,
        GMEM_INVALID_HANDLE = unchecked((int)0x8000),
        DM_UPDATE = 1,
        DM_COPY = 2,
        DM_PROMPT = 4,
        DM_MODIFY = 8,
        DM_IN_BUFFER = 8,
        DM_IN_PROMPT = 4,
        DM_OUT_BUFFER = 2,
        DM_OUT_DEFAULT = 1,
        DT_PLOTTER = 0,
        DT_RASDISPLAY = 1,
        DT_RASPRINTER = 2,
        DT_RASCAMERA = 3,
        DT_CHARSTREAM = 4,
        DT_METAFILE = 5,
        DT_DISPFILE = 6,
        TECHNOLOGY = 2,
        DC_FIELDS = 1,
        DC_PAPERS = 2,
        DC_PAPERSIZE = 3,
        DC_MINEXTENT = 4,
        DC_MAXEXTENT = 5,
        DC_BINS = 6,
        DC_DUPLEX = 7,
        DC_SIZE = 8,
        DC_EXTRA = 9,
        DC_VERSION = 10,
        DC_DRIVER = 11,
        DC_BINNAMES = 12,
        DC_ENUMRESOLUTIONS = 13,
        DC_FILEDEPENDENCIES = 14,
        DC_TRUETYPE = 15,
        DC_PAPERNAMES = 16,
        DC_ORIENTATION = 17,
        DC_COPIES = 18,
        PD_ALLPAGES = 0x00000000,
        PD_SELECTION = 0x00000001,
        PD_PAGENUMS = 0x00000002,
        PD_CURRENTPAGE = 0x00400000,
        PD_NOSELECTION = 0x00000004,
        PD_NOPAGENUMS = 0x00000008,
        PD_NOCURRENTPAGE = 0x00800000,
        PD_COLLATE = 0x00000010,
        PD_PRINTTOFILE = 0x00000020,
        PD_PRINTSETUP = 0x00000040,
        PD_NOWARNING = 0x00000080,
        PD_RETURNDC = 0x00000100,
        PD_RETURNIC = 0x00000200,
        PD_RETURNDEFAULT = 0x00000400,
        PD_SHOWHELP = 0x00000800,
        PD_ENABLEPRINTHOOK = 0x00001000,
        PD_ENABLESETUPHOOK = 0x00002000,
        PD_ENABLEPRINTTEMPLATE = 0x00004000,
        PD_ENABLESETUPTEMPLATE = 0x00008000,
        PD_ENABLEPRINTTEMPLATEHANDLE = 0x00010000,
        PD_ENABLESETUPTEMPLATEHANDLE = 0x00020000,
        PD_USEDEVMODECOPIES = 0x00040000,
        PD_USEDEVMODECOPIESANDCOLLATE = 0x00040000,
        PD_DISABLEPRINTTOFILE = 0x00080000,
        PD_HIDEPRINTTOFILE = 0x00100000,
        PD_NONETWORKBUTTON = 0x00200000,
        DI_MASK = 0x0001,
        DI_IMAGE = 0x0002,
        DI_NORMAL = 0x0003,
        DI_COMPAT = 0x0004,
        DI_DEFAULTSIZE = 0x0008,
        IDC_ARROW = 32512,
        IDC_IBEAM = 32513,
        IDC_WAIT = 32514,
        IDC_CROSS = 32515,
        IDC_UPARROW = 32516,
        IDC_SIZE = 32640,
        IDC_ICON = 32641,
        IDC_SIZENWSE = 32642,
        IDC_SIZENESW = 32643,
        IDC_SIZEWE = 32644,
        IDC_SIZENS = 32645,
        IDC_SIZEALL = 32646,
        IDC_NO = 32648,
        IDC_APPSTARTING = 32650,
        IDC_HELP = 32651,
        IMAGE_BITMAP = 0,
        IMAGE_ICON = 1,
        IMAGE_CURSOR = 2,
        IMAGE_ENHMETAFILE = 3,
        IDI_APPLICATION = 32512,
        IDI_HAND = 32513,
        IDI_QUESTION = 32514,
        IDI_EXCLAMATION = 32515,
        IDI_ASTERISK = 32516,
        IDI_WINLOGO = 32517,
        IDI_WARNING = 32515,
        IDI_ERROR = 32513,
        IDI_INFORMATION = 32516,
        IDI_SHIELD = 32518,
        SRCCOPY = 0x00CC0020,
        PLANES = 14,
        PS_SOLID = 0,
        PS_DASH = 1,
        PS_DOT = 2,
        PS_DASHDOT = 3,
        PS_DASHDOTDOT = 4,
        PS_NULL = 5,
        PS_INSIDEFRAME = 6,
        PS_USERSTYLE = 7,
        PS_ALTERNATE = 8,
        PS_STYLE_MASK = 0x0000000F,
        PS_ENDCAP_ROUND = 0x00000000,
        PS_ENDCAP_SQUARE = 0x00000100,
        PS_ENDCAP_FLAT = 0x00000200,
        PS_ENDCAP_MASK = 0x00000F00,
        PS_JOIN_ROUND = 0x00000000,
        PS_JOIN_BEVEL = 0x00001000,
        PS_JOIN_MITER = 0x00002000,
        PS_JOIN_MASK = 0x0000F000,
        PS_COSMETIC = 0x00000000,
        PS_GEOMETRIC = 0x00010000,
        PS_TYPE_MASK = 0x000F0000,
        BITSPIXEL = 12,
        ALTERNATE = 1,
        LOGPIXELSX = 88,
        LOGPIXELSY = 90,
        PHYSICALWIDTH = 110,
        PHYSICALHEIGHT = 111,
        PHYSICALOFFSETX = 112,
        PHYSICALOFFSETY = 113,
        WINDING = 2,
        VERTRES = 10,
        HORZRES = 8,
        DM_SPECVERSION = 0x0401,
        DM_ORIENTATION = 0x00000001,
        DM_PAPERSIZE = 0x00000002,
        DM_PAPERLENGTH = 0x00000004,
        DM_PAPERWIDTH = 0x00000008,
        DM_SCALE = 0x00000010,
        DM_COPIES = 0x00000100,
        DM_DEFAULTSOURCE = 0x00000200,
        DM_PRINTQUALITY = 0x00000400,
        DM_COLOR = 0x00000800,
        DM_DUPLEX = 0x00001000,
        DM_YRESOLUTION = 0x00002000,
        DM_TTOPTION = 0x00004000,
        DM_COLLATE = 0x00008000,
        DM_FORMNAME = 0x00010000,
        DM_LOGPIXELS = 0x00020000,
        DM_BITSPERPEL = 0x00040000,
        DM_PELSWIDTH = 0x00080000,
        DM_PELSHEIGHT = 0x00100000,
        DM_DISPLAYFLAGS = 0x00200000,
        DM_DISPLAYFREQUENCY = 0x00400000,
        DM_PANNINGWIDTH = 0x00800000,
        DM_PANNINGHEIGHT = 0x01000000,
        DM_ICMMETHOD = 0x02000000,
        DM_ICMINTENT = 0x04000000,
        DM_MEDIATYPE = 0x08000000,
        DM_DITHERTYPE = 0x10000000,
        DM_ICCMANUFACTURER = 0x20000000,
        DM_ICCMODEL = 0x40000000,
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
        DMPAPER_USER = 256,

        DMBIN_UPPER = 1,
        DMBIN_ONLYONE = 1,
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
        DMTT_BITMAP = 1,
        DMTT_DOWNLOAD = 2,
        DMTT_SUBDEV = 3,
        DMTT_DOWNLOAD_OUTLINE = 4,
        DMCOLLATE_FALSE = 0,
        DMCOLLATE_TRUE = 1,
        DMDISPLAYFLAGS_TEXTMODE = 0x00000004,
        DMICMMETHOD_NONE = 1,
        DMICMMETHOD_SYSTEM = 2,
        DMICMMETHOD_DRIVER = 3,
        DMICMMETHOD_DEVICE = 4,
        DMICMMETHOD_USER = 256,
        DMICM_SATURATE = 1,
        DMICM_CONTRAST = 2,
        DMICM_COLORMETRIC = 3,
        DMICM_USER = 256,
        DMMEDIA_STANDARD = 1,
        DMMEDIA_TRANSPARENCY = 2,
        DMMEDIA_GLOSSY = 3,
        DMMEDIA_USER = 256,
        DMDITHER_NONE = 1,
        DMDITHER_COARSE = 2,
        DMDITHER_FINE = 3,
        DMDITHER_LINEART = 4,
        DMDITHER_GRAYSCALE = 5,
        DMDITHER_USER = 256,
        PRINTER_ENUM_DEFAULT = 0x00000001,
        PRINTER_ENUM_LOCAL = 0x00000002,
        PRINTER_ENUM_CONNECTIONS = 0x00000004,
        PRINTER_ENUM_FAVORITE = 0x00000004,
        PRINTER_ENUM_NAME = 0x00000008,
        PRINTER_ENUM_REMOTE = 0x00000010,
        PRINTER_ENUM_SHARED = 0x00000020,
        PRINTER_ENUM_NETWORK = 0x00000040,
        PRINTER_ENUM_EXPAND = 0x00004000,
        PRINTER_ENUM_CONTAINER = 0x00008000,
        PRINTER_ENUM_ICONMASK = 0x00ff0000,
        PRINTER_ENUM_ICON1 = 0x00010000,
        PRINTER_ENUM_ICON2 = 0x00020000,
        PRINTER_ENUM_ICON3 = 0x00040000,
        PRINTER_ENUM_ICON4 = 0x00080000,
        PRINTER_ENUM_ICON5 = 0x00100000,
        PRINTER_ENUM_ICON6 = 0x00200000,
        PRINTER_ENUM_ICON7 = 0x00400000,
        PRINTER_ENUM_ICON8 = 0x00800000,
        DC_BINADJUST = 19,
        DC_EMF_COMPLIANT = 20,
        DC_DATATYPE_PRODUCED = 21,
        DC_COLLATE = 22,
        DCTT_BITMAP = 0x0000001,
        DCTT_DOWNLOAD = 0x0000002,
        DCTT_SUBDEV = 0x0000004,
        DCTT_DOWNLOAD_OUTLINE = 0x0000008,
        DCBA_FACEUPNONE = 0x0000,
        DCBA_FACEUPCENTER = 0x0001,
        DCBA_FACEUPLEFT = 0x0002,
        DCBA_FACEUPRIGHT = 0x0003,
        DCBA_FACEDOWNNONE = 0x0100,
        DCBA_FACEDOWNCENTER = 0x0101,
        DCBA_FACEDOWNLEFT = 0x0102,
        DCBA_FACEDOWNRIGHT = 0x0103,
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
        SM_CXSCREEN = 0,
        SM_CYSCREEN = 1,
        SM_CXVSCROLL = 2,
        SM_CYHSCROLL = 3,
        SM_CYCAPTION = 4,
        SM_CXBORDER = 5,
        SM_CYBORDER = 6,
        SM_CXDLGFRAME = 7,
        SM_CYDLGFRAME = 8,
        SM_CYVTHUMB = 9,
        SM_CXHTHUMB = 10,
        SM_CXICON = 11,
        SM_CYICON = 12,
        SM_CXCURSOR = 13,
        SM_CYCURSOR = 14,
        SM_CYMENU = 15,
        SM_CXFULLSCREEN = 16,
        SM_CYFULLSCREEN = 17,
        SM_CYKANJIWINDOW = 18,
        SM_MOUSEPRESENT = 19,
        SM_CYVSCROLL = 20,
        SM_CXHSCROLL = 21,
        SM_DEBUG = 22,
        SM_SWAPBUTTON = 23,
        SM_RESERVED1 = 24,
        SM_RESERVED2 = 25,
        SM_RESERVED3 = 26,
        SM_RESERVED4 = 27,
        SM_CXMIN = 28,
        SM_CYMIN = 29,
        SM_CXSIZE = 30,
        SM_CYSIZE = 31,
        SM_CXFRAME = 32,
        SM_CYFRAME = 33,
        SM_CXMINTRACK = 34,
        SM_CYMINTRACK = 35,
        SM_CXDOUBLECLK = 36,
        SM_CYDOUBLECLK = 37,
        SM_CXICONSPACING = 38,
        SM_CYICONSPACING = 39,
        SM_MENUDROPALIGNMENT = 40,
        SM_PENWINDOWS = 41,
        SM_DBCSENABLED = 42,
        SM_CMOUSEBUTTONS = 43,
        SM_CXFIXEDFRAME = 7,
        SM_CYFIXEDFRAME = 8,
        SM_CXSIZEFRAME = 32,
        SM_CYSIZEFRAME = 33,
        SM_SECURE = 44,
        SM_CXEDGE = 45,
        SM_CYEDGE = 46,
        SM_CXMINSPACING = 47,
        SM_CYMINSPACING = 48,
        SM_CXSMICON = 49,
        SM_CYSMICON = 50,
        SM_CYSMCAPTION = 51,
        SM_CXSMSIZE = 52,
        SM_CYSMSIZE = 53,
        SM_CXMENUSIZE = 54,
        SM_CYMENUSIZE = 55,
        SM_ARRANGE = 56,
        SM_CXMINIMIZED = 57,
        SM_CYMINIMIZED = 58,
        SM_CXMAXTRACK = 59,
        SM_CYMAXTRACK = 60,
        SM_CXMAXIMIZED = 61,
        SM_CYMAXIMIZED = 62,
        SM_NETWORK = 63,
        SM_CLEANBOOT = 67,
        SM_CXDRAG = 68,
        SM_CYDRAG = 69,
        SM_SHOWSOUNDS = 70,
        SM_CXMENUCHECK = 71,
        SM_CYMENUCHECK = 72,
        SM_SLOWMACHINE = 73,
        SM_MIDEASTENABLED = 74,
        SM_MOUSEWHEELPRESENT = 75,
        SM_XVIRTUALSCREEN = 76,
        SM_YVIRTUALSCREEN = 77,
        SM_CXVIRTUALSCREEN = 78,
        SM_CYVIRTUALSCREEN = 79,
        SM_CMONITORS = 80,
        SM_SAMEDISPLAYFORMAT = 81,
        SM_CMETRICS = 83,

        /* SetGraphicsMode(hdc, iMode ) */
        GM_COMPATIBLE = 1,
        GM_ADVANCED = 2,
        MWT_IDENTITY = 1,

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

        /* Mapping Modes */
        MM_TEXT = 1,
        // some others...

        /* Object Definitions for GetCurrentObject() and others. */
        OBJ_FONT = 6,
        // some others...


        /* Text Aligment */
        //        TA_NOUPDATECP = 0,
        //        TA_LEFT       = 0,
        //        TA_TOP        = 0,
        TA_DEFAULT = 0,

        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000,
        FORMAT_MESSAGE_DEFAULT = FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM;
        // some others...


        public const int NOMIRRORBITMAP = unchecked((int)0x80000000); /* Do not Mirror the bitmap in this call */

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleBitmap", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateCompatibleBitmap(HandleRef hDC, int width, int height);
        public static IntPtr CreateCompatibleBitmap(HandleRef hDC, int width, int height)
        {
            return System.Internal.HandleCollector.Add(IntCreateCompatibleBitmap(hDC, width, height), SafeNativeMethods.CommonHandles.GDI);
        }



        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateBitmap", CharSet = CharSet.Auto)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        public static extern IntPtr IntCreateBitmap(int width, int height, int planes, int bpp, IntPtr bitmapData);
        public static IntPtr CreateBitmap(int width, int height, int planes, int bpp, IntPtr bitmapData)
        {
            return System.Internal.HandleCollector.Add(IntCreateBitmap(width, height, planes, bpp, bitmapData), SafeNativeMethods.CommonHandles.GDI);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr GlobalFree(HandleRef handle);
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int StartDoc(HandleRef hDC, DOCINFO lpDocInfo);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int StartPage(HandleRef hDC);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int EndPage(HandleRef hDC);
        //      public static extern int SetAbortProc(m_hDC, (ABORTPROC)lpfn);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int AbortDoc(HandleRef hDC);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int EndDoc(HandleRef hDC);

        [DllImport(ExternDll.Comdlg32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool PrintDlg([In, Out] PRINTDLG lppd);

        [DllImport(ExternDll.Comdlg32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool PrintDlg([In, Out] PRINTDLGX86 lppd);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int DeviceCapabilities(string pDevice, string pPort, short fwCapabilities, IntPtr pOutput, IntPtr /*DEVMODE*/ pDevMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false)]
        public static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false)]
        public static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(ExternDll.Winspool, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int EnumPrinters(int flags, string name, int level, IntPtr pPrinterEnum/*buffer*/,
                                              int cbBuf, out int pcbNeeded, out int pcReturned);

        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr GlobalLock(HandleRef handle);
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr /*HDC*/ ResetDC(HandleRef hDC, HandleRef /*DEVMODE*/ lpDevMode);
        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool GlobalUnlock(HandleRef handle);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateRectRgn", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr IntCreateRectRgn(int x1, int y1, int x2, int y2);
        public static IntPtr CreateRectRgn(int x1, int y1, int x2, int y2)
        {
            return System.Internal.HandleCollector.Add(IntCreateRectRgn(x1, y1, x2, y2), SafeNativeMethods.CommonHandles.GDI);
        }
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetClipRgn(HandleRef hDC, HandleRef hRgn);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int SelectClipRgn(HandleRef hDC, HandleRef hRgn);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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
                    DeleteObject(new HandleRef(null, hTempRgn));
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
                    DeleteObject(new HandleRef(null, hRgn));
            }
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int ExtEscape(HandleRef hDC, int nEscape, int cbInput, ref int inData, int cbOutput, [Out] out int outData);
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int ExtEscape(HandleRef hDC, int nEscape, int cbInput, byte[] inData, int cbOutput, [Out] out int outData);
        public const int QUERYESCSUPPORT = 8, CHECKJPEGFORMAT = 4119, CHECKPNGFORMAT = 4120;

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntersectClipRect(HandleRef hDC, int x1, int y1, int x2, int y2);
        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, EntryPoint = "GlobalAlloc", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr IntGlobalAlloc(int uFlags, UIntPtr dwBytes); // size should be 32/64bits compatible
        public static IntPtr GlobalAlloc(int uFlags, uint dwBytes)
        {
            return IntGlobalAlloc(uFlags, new UIntPtr(dwBytes));
        }
        [DllImport(ExternDll.Kernel32)]
        static internal extern void ZeroMemory(IntPtr destination, UIntPtr length);

        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_PROC_NOT_FOUND = 127;


        [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
        public class ENHMETAHEADER
        {
            /// The ENHMETAHEADER structure is defined natively as a union with WmfHeader.  
            /// Extreme care should be taken if changing the layout of the corresponding managaed 
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
            public int cbSize = 20; //ndirect.DllLib.sizeOf(this);
            public String lpszDocName;
            public String lpszOutput;
            public String lpszDatatype;
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



        public enum StructFormat
        {
            Ansi = 1,
            Unicode = 2,
            Auto = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            // pt was a by-value POINT structure
            public int pt_x;
            public int pt_y;
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
            /*
             * Pictypes
             */
            public const int PICTYPE_UNINITIALIZED = -1;
            public const int PICTYPE_NONE = 0;
            public const int PICTYPE_BITMAP = 1;
            public const int PICTYPE_METAFILE = 2;
            public const int PICTYPE_ICON = 3;
            public const int PICTYPE_ENHMETAFILE = 4;

            public const int STATFLAG_DEFAULT = 0;
            public const int STATFLAG_NONAME = 1;
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
                PICTDESC pictdesc = new PICTDESC();
                pictdesc.cbSizeOfStruct = 12;
                pictdesc.picType = Ole.PICTYPE_ICON;
                pictdesc.union1 = hicon;
                return pictdesc;
            }

            public virtual IntPtr GetHandle()
            {
                return union1;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEVMODE
        {
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
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
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
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
                System.Internal.DebugHandleTracker.Initialize();
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
#endif
            }

            /// <summary>
            /// Handle type for accelerator tables.
            /// </summary>
            public static readonly int Accelerator = System.Internal.HandleCollector.RegisterType("Accelerator", 80, 50);

            /// <summary>
            /// Handle type for cursors.
            /// </summary>
            public static readonly int Cursor = System.Internal.HandleCollector.RegisterType("Cursor", 20, 500);

            /// <summary>
            /// Handle type for enhanced metafiles.
            /// </summary>
            public static readonly int EMF = System.Internal.HandleCollector.RegisterType("EnhancedMetaFile", 20, 500);

            /// <summary>
            /// Handle type for file find handles.
            /// </summary>
            public static readonly int Find = System.Internal.HandleCollector.RegisterType("Find", 0, 1000);

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

            /// <summary>
            /// Handle type for files.
            /// </summary>
            public static readonly int Menu = System.Internal.HandleCollector.RegisterType("Menu", 30, 1000);

            /// <summary>
            /// Handle type for windows.
            /// </summary>
            public static readonly int Window = System.Internal.HandleCollector.RegisterType("Window", 5, 1000);

#if DEBUG
            private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
            {
                System.Internal.DebugHandleTracker.CheckLeaks();
            }

            private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
            {
                System.Internal.DebugHandleTracker.CheckLeaks();
            }
#endif
        }


        public class StreamConsts
        {
            public const int LOCK_WRITE = 0x1;
            public const int LOCK_EXCLUSIVE = 0x2;
            public const int LOCK_ONLYONCE = 0x4;
            public const int STATFLAG_DEFAULT = 0x0;
            public const int STATFLAG_NONAME = 0x1;
            public const int STATFLAG_NOOPEN = 0x2;
            public const int STGC_DEFAULT = 0x0;
            public const int STGC_OVERWRITE = 0x1;
            public const int STGC_ONLYIFCURRENT = 0x2;
            public const int STGC_DANGEROUSLYCOMMITMERELYTODISKCACHE = 0x4;
            public const int STREAM_SEEK_SET = 0x0;
            public const int STREAM_SEEK_CUR = 0x1;
            public const int STREAM_SEEK_END = 0x2;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        internal static extern int IntDeleteObject(HandleRef hObject);
        public static int DeleteObject(HandleRef hObject)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hObject, SafeNativeMethods.CommonHandles.GDI);
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

        [DllImport(ExternDll.Shell32, CharSet = System.Runtime.InteropServices.CharSet.Auto, BestFitMapping = false, EntryPoint = "ExtractAssociatedIcon")]
        public unsafe static extern IntPtr IntExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index);

        public unsafe static IntPtr ExtractAssociatedIcon(HandleRef hInst, StringBuilder iconPath, ref int index)
        {
            return System.Internal.HandleCollector.Add(IntExtractAssociatedIcon(hInst, iconPath, ref index), SafeNativeMethods.CommonHandles.Icon);
        }

        [DllImport(ExternDll.User32, SetLastError = true, EntryPoint = "LoadIcon", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr IntLoadIcon(HandleRef hInst, IntPtr iconId);
        public static IntPtr LoadIcon(HandleRef hInst, int iconId)
        { // we only use the case were the low word of the IntPtr is used a resource id but it still has to be an intptr
            return IntLoadIcon(hInst, new IntPtr(iconId)); // on 32bits it'll be the same size, wider on 64bits
        }

        [DllImport(ExternDll.Comctl32, SetLastError = true, EntryPoint = "LoadIconWithScaleDown", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int IntLoadIconWithScaleDown(HandleRef hInst, IntPtr iconId, int cx, int cy, ref IntPtr phico);
        public static int LoadIconWithScaleDown(HandleRef hInst, int iconId, int cx, int cy, ref IntPtr phico)
        { // we only use the case were the low word of the IntPtr is used a resource id but it still has to be an intptr
            return IntLoadIconWithScaleDown(hInst, new IntPtr(iconId), cx, cy, ref phico); // on 32bits it'll be the same size, wider on 64bits
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "DestroyIcon", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool IntDestroyIcon(HandleRef hIcon);
        public static bool DestroyIcon(HandleRef hIcon)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hIcon, SafeNativeMethods.CommonHandles.Icon);
            return IntDestroyIcon(hIcon);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "CopyImage", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr IntCopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);
        public static IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags)
        {
            int handleType;
            switch (uType)
            {
                case SafeNativeMethods.IMAGE_ICON:
                    handleType = SafeNativeMethods.CommonHandles.Icon;
                    break;
                default:
                    handleType = SafeNativeMethods.CommonHandles.GDI;
                    break;
            }
            return System.Internal.HandleCollector.Add(IntCopyImage(hImage, uType, cxDesired, cyDesired, fuFlags), handleType);
        }

        // GetObject stuff
        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetObject(HandleRef hObject, int nSize, [In, Out] SafeNativeMethods.BITMAP bm);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetObject(HandleRef hObject, int nSize, [In, Out] SafeNativeMethods.LOGFONT lf);
        public static int GetObject(HandleRef hObject, SafeNativeMethods.LOGFONT lp)
        {
            return GetObject(hObject, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SafeNativeMethods.LOGFONT)), lp);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool GetIconInfo(HandleRef hIcon, [In, Out] SafeNativeMethods.ICONINFO info);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetSysColor(int nIndex);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool DrawIconEx(HandleRef hDC, int x, int y, HandleRef hIcon, int width, int height, int iStepIfAniCursor, HandleRef hBrushFlickerFree, int diFlags);

#if CUSTOM_MARSHALING_ISTREAM
        [DllImport(ExternDll.Oleaut32, PreserveSig=false)]
        public static extern IPicture OleLoadPictureEx(
                                                        [return: MarshalAs(UnmanagedType.CustomMarshaler,MarshalType="StreamToIStreamMarshaler")] Stream pStream, 
                                                        int lSize, bool fRunmode, ref Guid refiid, int width, int height, int dwFlags);
                                                        
                                                        
#endif
        [DllImport(ExternDll.Oleaut32, PreserveSig = false)]
        public static extern IPicture OleCreatePictureIndirect(SafeNativeMethods.PICTDESC pictdesc, [In]ref Guid refiid, bool fOwn);

        [
            ComImport(),
            Guid("7BF80980-BF32-101A-8BBB-00AA00300CAB"),
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown),

        ]
        public interface IPicture
        {
            [SuppressUnmanagedCodeSecurity()]
            IntPtr GetHandle();

            [SuppressUnmanagedCodeSecurity()]
            IntPtr GetHPal();

            [return: MarshalAs(UnmanagedType.I2)]
            [SuppressUnmanagedCodeSecurity()]
            short GetPictureType();

            [SuppressUnmanagedCodeSecurity()]
            int GetWidth();

            [SuppressUnmanagedCodeSecurity()]
            int GetHeight();

            [SuppressUnmanagedCodeSecurity()]
            void Render();

            [SuppressUnmanagedCodeSecurity()]
            void SetHPal(
                    [In]
                     IntPtr phpal);

            [SuppressUnmanagedCodeSecurity()]
            IntPtr GetCurDC();

            [SuppressUnmanagedCodeSecurity()]
            void SelectPicture(
                    [In]
                     IntPtr hdcIn,
                    [Out, MarshalAs(UnmanagedType.LPArray)]
                     int[] phdcOut,
                    [Out, MarshalAs(UnmanagedType.LPArray)]
                     int[] phbmpOut);

            [return: MarshalAs(UnmanagedType.Bool)]
            [SuppressUnmanagedCodeSecurity()]
            bool GetKeepOriginalFormat();

            [SuppressUnmanagedCodeSecurity()]
            void SetKeepOriginalFormat(
                    [In, MarshalAs(UnmanagedType.Bool)]
                     bool pfkeep);

            [SuppressUnmanagedCodeSecurity()]
            void PictureChanged();

            [SuppressUnmanagedCodeSecurity()]
            [System.Runtime.InteropServices.PreserveSig]
            int SaveAsFile(
                    [In, MarshalAs(UnmanagedType.Interface)]
                     UnsafeNativeMethods.IStream pstm,
                    [In]
                     int fSaveMemCopy,
                    [Out]
                     out int pcbSize);

            [SuppressUnmanagedCodeSecurity()]
            int GetAttributes();

            [SuppressUnmanagedCodeSecurity()]
            void SetHdc(
                    [In]
                     IntPtr hdc);
        }


        // for pulling encoded IPictures out of Access Databases
        //
        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECTHEADER
        {
            public short signature; // this looks like it's always 0x1c15
            public short headersize; // how big all this goo ends up being.  after this is the actual object data.
            public short objectType; // we don't care about anything else...they don't seem to be meaningful anyway.
            public short nameLen;
            public short classLen;
            public short nameOffset;
            public short classOffset;
            public short width;
            public short height;
            public IntPtr pInfo;
        }


        //values used in our known colortable
        internal enum Win32SystemColors
        {
            ActiveBorder = 0x0A,
            ActiveCaption = 0x02,
            ActiveCaptionText = 0x09,
            AppWorkspace = 0x0C,
            ButtonFace = 0x0F,
            ButtonHighlight = 0x14,
            ButtonShadow = 0x10,
            Control = 0x0F,
            ControlDark = 0x10,
            ControlDarkDark = 0x15,
            ControlLight = 0x16,
            ControlLightLight = 0x14,
            ControlText = 0x12,
            Desktop = 0x01,
            GradientActiveCaption = 0x1B,
            GradientInactiveCaption = 0x1C,
            GrayText = 0x11,
            Highlight = 0x0D,
            HighlightText = 0x0E,
            HotTrack = 0x1A,
            InactiveBorder = 0x0B,
            InactiveCaption = 0x03,
            InactiveCaptionText = 0x13,
            Info = 0x18,
            InfoText = 0x17,
            Menu = 0x04,
            MenuBar = 0x1E,
            MenuHighlight = 0x1D,
            MenuText = 0x07,
            ScrollBar = 0x00,
            Window = 0x05,
            WindowFrame = 0x06,
            WindowText = 0x08
        }


        // GDI stuff

        // see wingdi.h
        public enum BackgroundMode : int
        {
            TRANSPARENT = 1,
            OPAQUE = 2
        }
    }
}

