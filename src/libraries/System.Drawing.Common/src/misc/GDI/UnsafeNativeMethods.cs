// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    [System.Security.SuppressUnmanagedCodeSecurityAttribute]
    internal static partial class IntUnsafeNativeMethods
    {
        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr IntGetDC(HandleRef hWnd);
        public static IntPtr GetDC(HandleRef hWnd)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntGetDC(hWnd), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "GetHdc([hWnd=0x{0:X8}]) failed.", hWnd);
            return hdc;
        }

        /// <devdoc>
        ///     NOTE: DeleteDC is to be used to delete the hdc created from CreateCompatibleDC ONLY.  All other hdcs should be
        ///     deleted with DeleteHDC.
        /// </devdoc>
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

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);
        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, IntSafeNativeMethods.CommonHandles.HDC);
            // Note: retVal == 0 means it was not released but doesn't necessarily means an error; class or private DCs are never released.
            return IntReleaseDC(hWnd, hDC);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateDC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr IntCreateDC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);
        public static IntPtr CreateDC(String lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntCreateDC(lpszDriverName, lpszDeviceName, lpszOutput, lpInitData), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "CreateDC([driverName={0}], [deviceName={1}], [fileName={2}], [devMode={3}]) failed.", lpszDriverName, lpszDeviceName, lpszOutput, lpInitData.Handle);
            return hdc;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateIC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr IntCreateIC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);
        public static IntPtr CreateIC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData)
        {
            IntPtr hdc = System.Internal.HandleCollector.Add(IntCreateIC(lpszDriverName, lpszDeviceName, lpszOutput, lpInitData), IntSafeNativeMethods.CommonHandles.HDC);
            DbgUtil.AssertWin32(hdc != IntPtr.Zero, "CreateIC([driverName={0}], [deviceName={1}], [fileName={2}], [devMode={3}]) failed.", lpszDriverName, lpszDeviceName, lpszOutput, lpInitData.Handle);
            return hdc;
        }

        /// <devdoc>
        ///     CreateCompatibleDC requires to add a GDI handle instead of an HDC handle to avoid perf penalty in HandleCollector.
        ///     The hdc obtained from this method needs to be deleted with DeleteDC instead of DeleteHDC.
        /// </devdoc>
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleDC", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateCompatibleDC(HandleRef hDC);
        public static IntPtr CreateCompatibleDC(HandleRef hDC)
        {
            IntPtr compatibleDc = System.Internal.HandleCollector.Add(IntCreateCompatibleDC(hDC), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(compatibleDc != IntPtr.Zero, "CreateCompatibleDC([hdc=0x{0:X8}]) failed", hDC.Handle);
            return compatibleDc;
        }


        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SaveDC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntSaveDC(HandleRef hDC);
        public static int SaveDC(HandleRef hDC)
        {
            int state = IntSaveDC(hDC);
            DbgUtil.AssertWin32(state != 0, "SaveDC([hdc=0x{0:X8}]) failed", hDC.Handle);
            return state;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "RestoreDC", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "OffsetViewportOrgEx", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntOffsetViewportOrgEx(HandleRef hDC, int nXOffset, int nYOffset, [In, Out] IntNativeMethods.POINT point);
        public static bool OffsetViewportOrgEx(HandleRef hDC, int nXOffset, int nYOffset, [In, Out] IntNativeMethods.POINT point)
        {
            bool retVal = IntOffsetViewportOrgEx(hDC, nXOffset, nYOffset, point);
            DbgUtil.AssertWin32(retVal, "OffsetViewportOrgEx([hdc=0x{0:X8}], dx=[{1}], dy=[{2}], [out pPoint]) failed.", hDC.Handle, nXOffset, nYOffset);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SetGraphicsMode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntSetGraphicsMode(HandleRef hDC, int iMode);
        public static int SetGraphicsMode(HandleRef hDC, int iMode)
        {
            iMode = IntSetGraphicsMode(hDC, iMode);
            DbgUtil.AssertWin32(iMode != 0, "SetGraphicsMode([hdc=0x{0:X8}], [GM_ADVANCED]) failed.", hDC.Handle);
            return iMode;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetGraphicsMode(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern int GetROP2(HandleRef hdc);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int SetROP2(HandleRef hDC, int nDrawMode);


        // Region.
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CombineRgn", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetClipRgn", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntGetClipRgn(HandleRef hDC, HandleRef hRgn);
        public static int GetClipRgn(HandleRef hDC, HandleRef hRgn)
        {
            int retVal = IntGetClipRgn(hDC, hRgn);
            DbgUtil.AssertWin32(retVal != -1, "IntGetClipRgn([hdc=0x{0:X8}], [hRgn]) failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SelectClipRgn", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntNativeMethods.RegionFlags IntSelectClipRgn(HandleRef hDC, HandleRef hRgn);
        public static IntNativeMethods.RegionFlags SelectClipRgn(HandleRef hDC, HandleRef hRgn)
        {
            IntNativeMethods.RegionFlags result = IntSelectClipRgn(hDC, hRgn);
            DbgUtil.AssertWin32(result != IntNativeMethods.RegionFlags.ERROR, "SelectClipRgn([hdc=0x{0:X8}], [hRegion=0x{1:X8}]) failed.", hDC.Handle, hRgn.Handle);
            return result;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetRgnBox", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntNativeMethods.RegionFlags IntGetRgnBox(HandleRef hRgn, [In, Out] ref IntNativeMethods.RECT clipRect);
        public static IntNativeMethods.RegionFlags GetRgnBox(HandleRef hRgn, [In, Out] ref IntNativeMethods.RECT clipRect)
        {
            IntNativeMethods.RegionFlags result = IntGetRgnBox(hRgn, ref clipRect);
            DbgUtil.AssertWin32(result != IntNativeMethods.RegionFlags.ERROR, "GetRgnBox([hRegion=0x{0:X8}], [out rect]) failed.", hRgn.Handle);
            return result;
        }

        // Font.

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "CreateFontIndirect", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
        public static extern IntPtr IntCreateFontIndirect([In, Out, MarshalAs(UnmanagedType.AsAny)] object lf); // need object here since LOGFONT is not public.
#pragma warning restore CS0618
        public static IntPtr CreateFontIndirect(/*IntNativeMethods.LOGFONT*/ object lf)
        {
            IntPtr hFont = System.Internal.HandleCollector.Add(IntCreateFontIndirect(lf), IntSafeNativeMethods.CommonHandles.GDI);
            DbgUtil.AssertWin32(hFont != IntPtr.Zero, "CreateFontIndirect(logFont) failed.");
            return hFont;
        }

        // Common.
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteObject", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntDeleteObject(HandleRef hObject);
        public static bool DeleteObject(HandleRef hObject)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hObject, IntSafeNativeMethods.CommonHandles.GDI);
            bool retVal = IntDeleteObject(hObject);
            DbgUtil.AssertWin32(retVal, "DeleteObject(hObj=[0x{0:X8}]) failed.", hObject.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "GetObject", ExactSpelling = false, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntGetObject(HandleRef hBrush, int nSize, [In, Out] IntNativeMethods.LOGBRUSH lb);
        public static int GetObject(HandleRef hBrush, IntNativeMethods.LOGBRUSH lb)
        {
            int retVal = IntGetObject(hBrush, System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntNativeMethods.LOGBRUSH)), lb);
            DbgUtil.AssertWin32(retVal != 0, "GetObject(hObj=[0x{0:X8}], [LOGBRUSH]) failed.", hBrush.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, EntryPoint = "GetObject", ExactSpelling = false, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntGetObject(HandleRef hFont, int nSize, [In, Out] IntNativeMethods.LOGFONT lf);
        public static int GetObject(HandleRef hFont, IntNativeMethods.LOGFONT lp)
        {
            int retVal = IntGetObject(hFont, System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntNativeMethods.LOGFONT)), lp);
            DbgUtil.AssertWin32(retVal != 0, "GetObject(hObj=[0x{0:X8}], [LOGFONT]) failed.", hFont.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SelectObject", CharSet = CharSet.Auto)]
        public static extern IntPtr IntSelectObject(HandleRef hdc, HandleRef obj);
        public static IntPtr SelectObject(HandleRef hdc, HandleRef obj)
        {
            IntPtr oldObj = IntSelectObject(hdc, obj);
            DbgUtil.AssertWin32(oldObj != IntPtr.Zero, "SelectObject(hdc=hObj=[0x{0:X8}], hObj=[0x{1:X8}]) failed.", hdc.Handle, obj.Handle);
            return oldObj;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetCurrentObject", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr IntGetCurrentObject(HandleRef hDC, int uObjectType);
        public static IntPtr GetCurrentObject(HandleRef hDC, int uObjectType)
        {
            IntPtr hGdiObj = IntGetCurrentObject(hDC, uObjectType);
            // If the selected object is a region the return value is HGI_ERROR on failure.
            DbgUtil.AssertWin32(hGdiObj != IntPtr.Zero, "GetObject(hdc=[0x{0:X8}], type=[{1}]) failed.", hDC, uObjectType);
            return hGdiObj;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetStockObject", CharSet = CharSet.Auto)]
        public static extern IntPtr IntGetStockObject(int nIndex);
        public static IntPtr GetStockObject(int nIndex)
        {
            IntPtr hGdiObj = IntGetStockObject(nIndex);
            DbgUtil.AssertWin32(hGdiObj != IntPtr.Zero, "GetStockObject({0}) failed.", nIndex);
            return hGdiObj;
        }


        // Drawing.

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetNearestColor(HandleRef hDC, int color);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int /*COLORREF*/ SetTextColor(HandleRef hDC, int crColor);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetTextAlign(HandleRef hdc);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int /*COLORREF*/ GetTextColor(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int SetBkColor(HandleRef hDC, int clr);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SetBkMode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntSetBkMode(HandleRef hDC, int nBkMode);
        public static int SetBkMode(HandleRef hDC, int nBkMode)
        {
            int oldMode = IntSetBkMode(hDC, nBkMode);
            DbgUtil.AssertWin32(oldMode != 0, "SetBkMode(hdc=[0x{0:X8}], Mode=[{1}]) failed.", hDC.Handle, nBkMode);
            return oldMode;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetBkMode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntGetBkMode(HandleRef hDC);
        public static int GetBkMode(HandleRef hDC)
        {
            int mode = IntGetBkMode(hDC);
            DbgUtil.AssertWin32(mode != 0, "GetBkMode(hdc=[0x{0:X8}]) failed.", hDC.Handle);
            return mode;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetBkColor(HandleRef hDC);


        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int DrawTextW(HandleRef hDC, string lpszString, int nCount, ref IntNativeMethods.RECT lpRect, int nFormat);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern int DrawTextA(HandleRef hDC, byte[] lpszString, int byteCount, ref IntNativeMethods.RECT lpRect, int nFormat);

        public static int DrawText(HandleRef hDC, string text, ref IntNativeMethods.RECT lpRect, int nFormat)
        {
            int retVal;
            if (Marshal.SystemDefaultCharSize == 1)
            {
                // CapRectangleForWin9x(ref lpRect);
                // we have to do this because if we pass too big of a value (<= 2^31) to win9x
                // it fails and returns negative values as the rect in which it painted the text
                lpRect.top = Math.Min(Int16.MaxValue, lpRect.top);
                lpRect.left = Math.Min(Int16.MaxValue, lpRect.left);
                lpRect.right = Math.Min(Int16.MaxValue, lpRect.right);
                lpRect.bottom = Math.Min(Int16.MaxValue, lpRect.bottom);

                // Convert Unicode string to ANSI.
                int byteCount = IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
                byte[] textBytes = new byte[byteCount];
                IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, textBytes, textBytes.Length, IntPtr.Zero, IntPtr.Zero);

                // Security: Windows 95/98/Me: This value may not exceed 8192.
                byteCount = Math.Min(byteCount, IntNativeMethods.MaxTextLengthInWin9x);
                retVal = DrawTextA(hDC, textBytes, byteCount, ref lpRect, nFormat);
            }
            else
            {
                retVal = DrawTextW(hDC, text, text.Length, ref lpRect, nFormat);
            }

            DbgUtil.AssertWin32(retVal != 0, "DrawText(hdc=[0x{0:X8}], text=[{1}], rect=[{2}], flags=[{3}] failed.", hDC.Handle, text, lpRect, nFormat);
            return retVal;
        }

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int DrawTextExW(HandleRef hDC, string lpszString, int nCount, ref IntNativeMethods.RECT lpRect, int nFormat, [In, Out] IntNativeMethods.DRAWTEXTPARAMS lpDTParams);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern int DrawTextExA(HandleRef hDC, byte[] lpszString, int byteCount, ref IntNativeMethods.RECT lpRect, int nFormat, [In, Out] IntNativeMethods.DRAWTEXTPARAMS lpDTParams);

        public static int DrawTextEx(HandleRef hDC, string text, ref IntNativeMethods.RECT lpRect, int nFormat, [In, Out] IntNativeMethods.DRAWTEXTPARAMS lpDTParams)
        {
            int retVal;
            if (Marshal.SystemDefaultCharSize == 1)
            {
                // CapRectangleForWin9x(ref lpRect);
                // we have to do this because if we pass too big of a value (<= 2^31) to win9x
                // it fails and returns negative values as the rect in which it painted the text
                lpRect.top = Math.Min(Int16.MaxValue, lpRect.top);
                lpRect.left = Math.Min(Int16.MaxValue, lpRect.left);
                lpRect.right = Math.Min(Int16.MaxValue, lpRect.right);
                lpRect.bottom = Math.Min(Int16.MaxValue, lpRect.bottom);

                // Convert Unicode string to ANSI.
                int byteCount = IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
                byte[] textBytes = new byte[byteCount];
                IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, textBytes, textBytes.Length, IntPtr.Zero, IntPtr.Zero);

                // Security: Windows 95/98/Me: This value may not exceed 8192.
                byteCount = Math.Min(byteCount, IntNativeMethods.MaxTextLengthInWin9x);
                retVal = DrawTextExA(hDC, textBytes, byteCount, ref lpRect, nFormat, lpDTParams);
            }
            else
            {
                retVal = DrawTextExW(hDC, text, text.Length, ref lpRect, nFormat, lpDTParams);
            }

            DbgUtil.AssertWin32(retVal != 0, "DrawTextEx(hdc=[0x{0:X8}], text=[{1}], rect=[{2}], flags=[{3}] failed.", hDC.Handle, text, lpRect, nFormat);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int GetTextExtentPoint32W(HandleRef hDC, string text, int len, [In, Out] IntNativeMethods.SIZE size);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern int GetTextExtentPoint32A(HandleRef hDC, byte[] lpszString, int byteCount, [In, Out] IntNativeMethods.SIZE size);

        public static int GetTextExtentPoint32(HandleRef hDC, string text, [In, Out] IntNativeMethods.SIZE size)
        {
            int retVal;
            int byteCount = text.Length;

            if (Marshal.SystemDefaultCharSize == 1)
            {
                // Convert Unicode string to ANSI.
                byteCount = IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
                byte[] textBytes = new byte[byteCount];
                IntUnsafeNativeMethods.WideCharToMultiByte(IntNativeMethods.CP_ACP, 0, text, text.Length, textBytes, textBytes.Length, IntPtr.Zero, IntPtr.Zero);

                // Security: Windows 95/98/Me: This value may not exceed 8192.
                byteCount = Math.Min(text.Length, IntNativeMethods.MaxTextLengthInWin9x);
                retVal = GetTextExtentPoint32A(hDC, textBytes, byteCount, size);
            }
            else
            {
                retVal = GetTextExtentPoint32W(hDC, text, text.Length, size);
            }

            DbgUtil.AssertWin32(retVal != 0, "GetTextExtentPoint32(hdc=[0x{0:X8}], text=[{1}], size=[{2}] failed.", hDC.Handle, text, size);
            return retVal;
        }

        // WARNING: This method is currently used just for drawing the text background (ComponentEditorForm.cs) and not for rendering text.
        //          Prefer using DrawText over this method if possible, it handles Win9x issues properly.  Ideally, we should remove this method
        //          but to avoid issues at this point I'm leaving it here.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = false, CharSet = CharSet.Auto)]
        internal static extern bool ExtTextOut(HandleRef hdc, int x, int y, int options, ref IntNativeMethods.RECT rect, string str, int length, int[] spacing);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "LineTo", CharSet = CharSet.Auto)]
        public static extern bool IntLineTo(HandleRef hdc, int x, int y);
        public static bool LineTo(HandleRef hdc, int x, int y)
        {
            bool retVal = IntLineTo(hdc, x, y);
            DbgUtil.AssertWin32(retVal, "LineTo(hdc=[0x{0:X8}], x=[{1}], y=[{2}] failed.", hdc.Handle, x, y);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "MoveToEx", CharSet = CharSet.Auto)]
        public static extern bool IntMoveToEx(HandleRef hdc, int x, int y, IntNativeMethods.POINT pt);
        public static bool MoveToEx(HandleRef hdc, int x, int y, IntNativeMethods.POINT pt)
        {
            bool retVal = IntMoveToEx(hdc, x, y, pt);
            DbgUtil.AssertWin32(retVal, "MoveToEx(hdc=[0x{0:X8}], x=[{1}], y=[{2}], pt=[{3}] failed.", hdc.Handle, x, y, pt);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "Rectangle", CharSet = CharSet.Auto)]
        public static extern bool IntRectangle(HandleRef hdc, int left, int top, int right, int bottom);
        public static bool Rectangle(HandleRef hdc, int left, int top, int right, int bottom)
        {
            bool retVal = IntRectangle(hdc, left, top, right, bottom);
            DbgUtil.AssertWin32(retVal, "Rectangle(hdc=[0x{0:X8}], left=[{1}], top=[{2}], right=[{3}], bottom=[{4}] failed.", hdc.Handle, left, top, right, bottom);
            return retVal;
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "FillRect", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntFillRect(HandleRef hdc, [In] ref IntNativeMethods.RECT rect, HandleRef hbrush);
        public static bool FillRect(HandleRef hDC, [In] ref IntNativeMethods.RECT rect, HandleRef hbrush)
        {
            bool retVal = IntFillRect(hDC, ref rect, hbrush);
            DbgUtil.AssertWin32(retVal, "FillRect(hdc=[0x{0:X8}], rect=[{1}], hbrush=[{2}]", hDC.Handle, rect, hbrush.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SetMapMode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntSetMapMode(HandleRef hDC, int nMapMode);
        public static int SetMapMode(HandleRef hDC, int nMapMode)
        {
            int oldMapMode = IntSetMapMode(hDC, nMapMode);
            DbgUtil.AssertWin32(oldMapMode != 0, "SetMapMode(hdc=[0x{0:X8}], MapMode=[{1}]", hDC.Handle, nMapMode);
            return oldMapMode;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetMapMode", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int IntGetMapMode(HandleRef hDC);
        public static int GetMapMode(HandleRef hDC)
        {
            int mapMode = IntGetMapMode(hDC);
            DbgUtil.AssertWin32(mapMode != 0, "GetMapMode(hdc=[0x{0:X8}]", hDC.Handle);
            return mapMode;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetViewportExtEx")]
        public static extern bool IntGetViewportExtEx(HandleRef hdc, [In, Out] IntNativeMethods.SIZE lpSize);
        public static bool GetViewportExtEx(HandleRef hdc, [In, Out] IntNativeMethods.SIZE lpSize)
        {
            bool retVal = IntGetViewportExtEx(hdc, lpSize);
            DbgUtil.AssertWin32(retVal, "GetViewportExtEx([hdc=0x{0:X8}], [out size]) failed.", hdc.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetViewportOrgEx")]
        public static extern bool IntGetViewportOrgEx(HandleRef hdc, [In, Out] IntNativeMethods.POINT lpPoint);
        public static bool GetViewportOrgEx(HandleRef hdc, [In, Out] IntNativeMethods.POINT lpPoint)
        {
            bool retVal = IntGetViewportOrgEx(hdc, lpPoint);
            DbgUtil.AssertWin32(retVal, "GetViewportOrgEx([hdc=0x{0:X8}], [out point]) failed.", hdc.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SetViewportExtEx", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntSetViewportExtEx(HandleRef hDC, int x, int y, [In, Out] IntNativeMethods.SIZE size);
        public static bool SetViewportExtEx(HandleRef hDC, int x, int y, [In, Out] IntNativeMethods.SIZE size)
        {
            bool retVal = IntSetViewportExtEx(hDC, x, y, size);
            DbgUtil.AssertWin32(retVal, "SetViewportExtEx([hdc=0x{0:X8}], x=[{1}], y=[{2}], [out size]) failed.", hDC.Handle, x, y);
            return retVal;
        }
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "SetViewportOrgEx", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntSetViewportOrgEx(HandleRef hDC, int x, int y, [In, Out] IntNativeMethods.POINT point);
        public static bool SetViewportOrgEx(HandleRef hDC, int x, int y, [In, Out] IntNativeMethods.POINT point)
        {
            bool retVal = IntSetViewportOrgEx(hDC, x, y, point);
            DbgUtil.AssertWin32(retVal, "SetViewportOrgEx([hdc=0x{0:X8}], x=[{1}], y=[{2}], [out point]) failed.", hDC.Handle, x, y);
            return retVal;
        }



        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int GetTextMetricsW(HandleRef hDC, [In, Out] ref IntNativeMethods.TEXTMETRIC lptm);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        public static extern int GetTextMetricsA(HandleRef hDC, [In, Out] ref IntNativeMethods.TEXTMETRICA lptm);

        public static int GetTextMetrics(HandleRef hDC, ref IntNativeMethods.TEXTMETRIC lptm)
        {
            int retVal;

            if (Marshal.SystemDefaultCharSize == 1)
            {
                // ANSI
                IntNativeMethods.TEXTMETRICA lptmA = new IntNativeMethods.TEXTMETRICA();
                retVal = IntUnsafeNativeMethods.GetTextMetricsA(hDC, ref lptmA);

                lptm.tmHeight = lptmA.tmHeight;
                lptm.tmAscent = lptmA.tmAscent;
                lptm.tmDescent = lptmA.tmDescent;
                lptm.tmInternalLeading = lptmA.tmInternalLeading;
                lptm.tmExternalLeading = lptmA.tmExternalLeading;
                lptm.tmAveCharWidth = lptmA.tmAveCharWidth;
                lptm.tmMaxCharWidth = lptmA.tmMaxCharWidth;
                lptm.tmWeight = lptmA.tmWeight;
                lptm.tmOverhang = lptmA.tmOverhang;
                lptm.tmDigitizedAspectX = lptmA.tmDigitizedAspectX;
                lptm.tmDigitizedAspectY = lptmA.tmDigitizedAspectY;
                lptm.tmFirstChar = (char)lptmA.tmFirstChar;
                lptm.tmLastChar = (char)lptmA.tmLastChar;
                lptm.tmDefaultChar = (char)lptmA.tmDefaultChar;
                lptm.tmBreakChar = (char)lptmA.tmBreakChar;
                lptm.tmItalic = lptmA.tmItalic;
                lptm.tmUnderlined = lptmA.tmUnderlined;
                lptm.tmStruckOut = lptmA.tmStruckOut;
                lptm.tmPitchAndFamily = lptmA.tmPitchAndFamily;
                lptm.tmCharSet = lptmA.tmCharSet;
            }
            else
            {
                // Unicode
                retVal = IntUnsafeNativeMethods.GetTextMetricsW(hDC, ref lptm);
            }

            DbgUtil.AssertWin32(retVal != 0, "GetTextMetrics(hdc=[0x{0:X8}], [out TEXTMETRIC] failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "BeginPath", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntBeginPath(HandleRef hDC);
        public static bool BeginPath(HandleRef hDC)
        {
            bool retVal = IntBeginPath(hDC);
            DbgUtil.AssertWin32(retVal, "BeginPath(hdc=[0x{0:X8}]failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "EndPath", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntEndPath(HandleRef hDC);
        public static bool EndPath(HandleRef hDC)
        {
            bool retVal = IntEndPath(hDC);
            DbgUtil.AssertWin32(retVal, "EndPath(hdc=[0x{0:X8}]failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "StrokePath", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntStrokePath(HandleRef hDC);
        public static bool StrokePath(HandleRef hDC)
        {
            bool retVal = IntStrokePath(hDC);
            DbgUtil.AssertWin32(retVal, "StrokePath(hdc=[0x{0:X8}]failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "AngleArc", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntAngleArc(HandleRef hDC, int x, int y, int radius, float startAngle, float endAngle);
        public static bool AngleArc(HandleRef hDC, int x, int y, int radius, float startAngle, float endAngle)
        {
            bool retVal = IntAngleArc(hDC, x, y, radius, startAngle, endAngle);
            DbgUtil.AssertWin32(retVal, "AngleArc(hdc=[0x{0:X8}], ...) failed.", hDC.Handle);
            return retVal;
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "Arc", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntArc(
            HandleRef hDC,
            int nLeftRect,   // x-coord of rectangle's upper-left corner
            int nTopRect,    // y-coord of rectangle's upper-left corner
            int nRightRect,  // x-coord of rectangle's lower-right corner
            int nBottomRect, // y-coord of rectangle's lower-right corner
            int nXStartArc,  // x-coord of first radial ending point
            int nYStartArc,  // y-coord of first radial ending point
            int nXEndArc,    // x-coord of second radial ending point
            int nYEndArc     // y-coord of second radial ending point
            );
        public static bool Arc(
            HandleRef hDC,
            int nLeftRect,   // x-coord of rectangle's upper-left corner
            int nTopRect,    // y-coord of rectangle's upper-left corner
            int nRightRect,  // x-coord of rectangle's lower-right corner
            int nBottomRect, // y-coord of rectangle's lower-right corner
            int nXStartArc,  // x-coord of first radial ending point
            int nYStartArc,  // y-coord of first radial ending point
            int nXEndArc,    // x-coord of second radial ending point
            int nYEndArc     // y-coord of second radial ending point
            )
        {
            bool retVal = IntArc(hDC, nLeftRect, nTopRect, nRightRect, nBottomRect, nXStartArc, nYStartArc, nXEndArc, nYEndArc);
            DbgUtil.AssertWin32(retVal, "Arc(hdc=[0x{0:X8}], ...) failed.", hDC.Handle);
            return retVal;
        }

        // Misc.

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int SetTextAlign(HandleRef hDC, int nMode);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "Ellipse", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool IntEllipse(HandleRef hDc, int x1, int y1, int x2, int y2);
        public static bool Ellipse(HandleRef hDc, int x1, int y1, int x2, int y2)
        {
            bool retVal = IntEllipse(hDc, x1, y1, x2, y2);
            DbgUtil.AssertWin32(retVal, "Ellipse(hdc=[0x{0:X8}], x1=[{1}], y1=[{2}], x2=[{3}], y2=[{4}]) failed.", hDc.Handle, x1, y1, x2, y2);
            return retVal;
        }

        // From MSDN: Using the MultiByteToWideChar/WideCharToMultiByte function incorrectly can compromise the security of your application. 
        // Calling the WideCharToMultiByte function can easily cause a buffer overrun because the size of the In buffer equals the number 
        // of WCHARs in the string, while the size of the Out buffer equals the number of bytes. To avoid a buffer overrun, be sure to specify 
        // a buffer size appropriate for the data type the buffer receives. For more information, see Security Considerations: International Features. 
        // Always call these functions passing a null destination buffer to get its size and the create the buffer with the exact size.
        [DllImport(ExternDll.Kernel32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int WideCharToMultiByte(int codePage, int flags, [MarshalAs(UnmanagedType.LPWStr)]string wideStr, int chars, [In, Out]byte[] pOutBytes, int bufferBytes, IntPtr defaultChar, IntPtr pDefaultUsed);
    }
}
