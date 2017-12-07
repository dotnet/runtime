// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Drawing.Internal
{
    internal static partial class IntUnsafeNativeMethods
    {
        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        public static extern IntPtr IntGetDC(HandleRef hWnd);

        public static IntPtr GetDC(HandleRef hWnd)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntGetDC(hWnd), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "GetHdc([hWnd=0x{0:X8}]) failed.", hWnd);
            return hdc;
        }

        /// <summary>
        /// NOTE: DeleteDC is to be used to delete the hdc created from CreateCompatibleDC ONLY. All other hdcs should
        /// be deleted with DeleteHDC.
        /// </summary>
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteDC", CharSet = CharSet.Auto)]
        public static extern bool IntDeleteDC(HandleRef hDC);

        public static bool DeleteDC(HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, IntSafeNativeMethods.CommonHandles.GDI);
            bool retVal = IntDeleteDC(hDC);
            DbgUtil.AssertWin32(retVal, "DeleteDC([hdc=0x{0:X8}]) failed.", hDC.Handle);
            return retVal;
        }

        public static bool DeleteHDC(HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, IntSafeNativeMethods.CommonHandles.HDC);
            bool retVal = IntDeleteDC(hDC);
            DbgUtil.AssertWin32(retVal, "DeleteHDC([hdc=0x{0:X8}]) failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = CharSet.Auto)]
        public static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);

        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, IntSafeNativeMethods.CommonHandles.HDC);
            // Note: retVal == 0 means it was not released but doesn't necessarily means an error; class or private DCs are never released.
            return IntReleaseDC(hWnd, hDC);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateDC", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateDC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);

        public static IntPtr CreateDC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntCreateDC(lpszDriverName, lpszDeviceName, lpszOutput, lpInitData), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "CreateDC([driverName={0}], [deviceName={1}], [fileName={2}], [devMode={3}]) failed.", lpszDriverName, lpszDeviceName, lpszOutput, lpInitData.Handle);
            return hdc;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateIC", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateIC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);

        public static IntPtr CreateIC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntCreateIC(lpszDriverName, lpszDeviceName, lpszOutput, lpInitData), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "CreateIC([driverName={0}], [deviceName={1}], [fileName={2}], [devMode={3}]) failed.", lpszDriverName, lpszDeviceName, lpszOutput, lpInitData.Handle);
            return hdc;
        }

        /// <summary>
        /// CreateCompatibleDC requires to add a GDI handle instead of an HDC handle to avoid perf penalty in HandleCollector.
        /// The hdc obtained from this method needs to be deleted with DeleteDC instead of DeleteHDC.
        /// </summary>
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleDC", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateCompatibleDC(HandleRef hDC);

        public static IntPtr CreateCompatibleDC(HandleRef hDC)
        {
            IntPtr compatibleDc = System.Internal.HandleCollector.Add(IntCreateCompatibleDC(hDC), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(compatibleDc != IntPtr.Zero, "CreateCompatibleDC([hdc=0x{0:X8}]) failed", hDC.Handle);
            return compatibleDc;
        }


        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SaveDC", CharSet = CharSet.Auto)]
        public static extern int IntSaveDC(HandleRef hDC);

        public static int SaveDC(HandleRef hDC)
        {
            int state = IntSaveDC(hDC);
            DbgUtil.AssertWin32(state != 0, "SaveDC([hdc=0x{0:X8}]) failed", hDC.Handle);
            return state;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "RestoreDC", CharSet = CharSet.Auto)]
        public static extern bool IntRestoreDC(HandleRef hDC, int nSavedDC);

        public static bool RestoreDC(HandleRef hDC, int nSavedDC)
        {
            bool retVal = IntRestoreDC(hDC, nSavedDC);
            // When a winforms app is closing, the cached MeasurementGraphics is finalized but it is possible that
            // its DeviceContext is finalized first so when this method is called the DC has already been relesaed poping up the 
            // assert window.  Need to find a way to work around this and enable the assert IF NEEDED.
            // DbgUtil.AssertWin32(retVal, "RestoreDC([hdc=0x{0:X8}], [restoreState={1}]) failed.", (int)hDC.Handle, nSavedDC);
            return retVal;
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr WindowFromDC(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "OffsetViewportOrgEx", CharSet = CharSet.Auto)]
        public static extern bool IntOffsetViewportOrgEx(HandleRef hDC, int nXOffset, int nYOffset, [In, Out] IntNativeMethods.POINT point);

        public static bool OffsetViewportOrgEx(HandleRef hDC, int nXOffset, int nYOffset, [In, Out] IntNativeMethods.POINT point)
        {
            bool retVal = IntOffsetViewportOrgEx(hDC, nXOffset, nYOffset, point);
            DbgUtil.AssertWin32(retVal, "OffsetViewportOrgEx([hdc=0x{0:X8}], dx=[{1}], dy=[{2}], [out pPoint]) failed.", hDC.Handle, nXOffset, nYOffset);
            return retVal;
        }

        // Region.
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CombineRgn", CharSet = CharSet.Auto)]
        public static extern IntNativeMethods.RegionFlags IntCombineRgn(HandleRef hRgnDest, HandleRef hRgnSrc1, HandleRef hRgnSrc2, RegionCombineMode combineMode);

        public static IntNativeMethods.RegionFlags CombineRgn(HandleRef hRgnDest, HandleRef hRgnSrc1, HandleRef hRgnSrc2, RegionCombineMode combineMode)
        {
            Debug.Assert(hRgnDest.Wrapper != null && hRgnDest.Handle != IntPtr.Zero, "Destination region is invalid");
            Debug.Assert(hRgnSrc1.Wrapper != null && hRgnSrc1.Handle != IntPtr.Zero, "Source region 1 is invalid");
            Debug.Assert(hRgnSrc2.Wrapper != null && hRgnSrc2.Handle != IntPtr.Zero, "Source region 2 is invalid");

            if (hRgnDest.Wrapper == null || hRgnSrc1.Wrapper == null || hRgnSrc2.Wrapper == null)
            {
                return IntNativeMethods.RegionFlags.ERROR;
            }

            // Note: CombineRgn can return Error when no regions are combined, this is not an error condition.
            return IntCombineRgn(hRgnDest, hRgnSrc1, hRgnSrc2, combineMode);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetClipRgn", CharSet = CharSet.Auto)]
        public static extern int IntGetClipRgn(HandleRef hDC, HandleRef hRgn);

        public static int GetClipRgn(HandleRef hDC, HandleRef hRgn)
        {
            int retVal = IntGetClipRgn(hDC, hRgn);
            DbgUtil.AssertWin32(retVal != -1, "IntGetClipRgn([hdc=0x{0:X8}], [hRgn]) failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SelectClipRgn", CharSet = CharSet.Auto)]
        public static extern IntNativeMethods.RegionFlags IntSelectClipRgn(HandleRef hDC, HandleRef hRgn);

        public static IntNativeMethods.RegionFlags SelectClipRgn(HandleRef hDC, HandleRef hRgn)
        {
            IntNativeMethods.RegionFlags result = IntSelectClipRgn(hDC, hRgn);
            DbgUtil.AssertWin32(result != IntNativeMethods.RegionFlags.ERROR, "SelectClipRgn([hdc=0x{0:X8}], [hRegion=0x{1:X8}]) failed.", hDC.Handle, hRgn.Handle);
            return result;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetRgnBox", CharSet = CharSet.Auto)]
        public static extern IntNativeMethods.RegionFlags IntGetRgnBox(HandleRef hRgn, [In, Out] ref IntNativeMethods.RECT clipRect);

        public static IntNativeMethods.RegionFlags GetRgnBox(HandleRef hRgn, [In, Out] ref IntNativeMethods.RECT clipRect)
        {
            IntNativeMethods.RegionFlags result = IntGetRgnBox(hRgn, ref clipRect);
            DbgUtil.AssertWin32(result != IntNativeMethods.RegionFlags.ERROR, "GetRgnBox([hRegion=0x{0:X8}], [out rect]) failed.", hRgn.Handle);
            return result;
        }

        // Font.

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateFontIndirect", CharSet = CharSet.Auto)]
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        public static extern IntPtr IntCreateFontIndirect([In, Out, MarshalAs(UnmanagedType.AsAny)] object lf); // need object here since LOGFONT is not public.
#pragma warning restore CS0618

        // Common.
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = CharSet.Auto)]
        public static extern bool IntDeleteObject(HandleRef hObject);

        public static bool DeleteObject(HandleRef hObject)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hObject, IntSafeNativeMethods.CommonHandles.GDI);
            bool retVal = IntDeleteObject(hObject);
            DbgUtil.AssertWin32(retVal, "DeleteObject(hObj=[0x{0:X8}]) failed.", hObject.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetCurrentObject", CharSet = CharSet.Auto)]
        public static extern IntPtr IntGetCurrentObject(HandleRef hDC, int uObjectType);

        public static IntPtr GetCurrentObject(HandleRef hDC, int uObjectType)
        {
            IntPtr hGdiObj = IntGetCurrentObject(hDC, uObjectType);
            // If the selected object is a region the return value is HGI_ERROR on failure.
            DbgUtil.AssertWin32(hGdiObj != IntPtr.Zero, "GetObject(hdc=[0x{0:X8}], type=[{1}]) failed.", hDC, uObjectType);
            return hGdiObj;
        }
    }
}
