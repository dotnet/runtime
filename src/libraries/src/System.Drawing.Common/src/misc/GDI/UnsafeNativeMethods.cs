// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    internal static partial class IntUnsafeNativeMethods
    {
        /// <summary>
        /// NOTE: DeleteDC is to be used to delete the hdc created from CreateCompatibleDC ONLY. All other hdcs should
        /// be deleted with DeleteHDC.
        /// </summary>
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool DeleteDC(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateDC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateIC(string lpszDriverName, string lpszDeviceName, string lpszOutput, HandleRef /*DEVMODE*/ lpInitData);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateCompatibleDC(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern int SaveDC(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool RestoreDC(HandleRef hDC, int nSavedDC);

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

        [DllImport(ExternDll.Gdi32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFontIndirect(ref SafeNativeMethods.LOGFONT lf);

        // Common.
        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool DeleteObject(HandleRef hObject);

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
