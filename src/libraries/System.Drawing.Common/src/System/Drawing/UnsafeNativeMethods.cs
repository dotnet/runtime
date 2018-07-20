// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, EntryPoint = "RtlMoveMemory", CharSet = CharSet.Auto)]
        public static extern void CopyMemory(HandleRef destData, HandleRef srcData, int size);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        private static extern IntPtr IntGetDC(HandleRef hWnd);

        public static IntPtr GetDC(HandleRef hWnd)
        {
            return System.Internal.HandleCollector.Add(IntGetDC(hWnd), SafeNativeMethods.CommonHandles.HDC);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "DeleteDC", CharSet = CharSet.Auto)]
        private static extern bool IntDeleteDC(HandleRef hDC);

        public static bool DeleteDC(HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, SafeNativeMethods.CommonHandles.GDI);
            return IntDeleteDC(hDC);
        }

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = CharSet.Auto)]
        private static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);

        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            System.Internal.HandleCollector.Remove((IntPtr)hDC, SafeNativeMethods.CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDC);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateCompatibleDC", CharSet = CharSet.Auto)]
        private static extern IntPtr IntCreateCompatibleDC(HandleRef hDC);

        public static IntPtr CreateCompatibleDC(HandleRef hDC)
        {
            return System.Internal.HandleCollector.Add(IntCreateCompatibleDC(hDC), SafeNativeMethods.CommonHandles.GDI);
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetStockObject(int nIndex);

        [DllImport(ExternDll.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetSystemDefaultLCID();

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, [In, Out] NativeMethods.NONCLIENTMETRICS pvParam, int fWinIni);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, [In, Out] SafeNativeMethods.LOGFONT pvParam, int fWinIni);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetObjectType(HandleRef hObject);
    }
}
