// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal class UnsafeNativeMethods
    {
        [DllImport(ExternDll.Kernel32, SetLastError = true, ExactSpelling = true, EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(HandleRef destData, HandleRef srcData, int size);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GetDC(HandleRef hWnd);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern bool DeleteDC(HandleRef hDC);

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern int ReleaseDC(HandleRef hWnd, HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CreateCompatibleDC(HandleRef hDC);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr GetStockObject(int nIndex);

        [DllImport(ExternDll.Kernel32, SetLastError = true)]
        public static extern int GetSystemDefaultLCID();

        [DllImport(ExternDll.User32, SetLastError = true, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport(ExternDll.User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern unsafe bool SystemParametersInfo(uint uiAction, uint uiParam, void* pvParam, uint fWinIni);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(HandleRef hDC, int nIndex);

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetObjectType(HandleRef hObject);
    }
}
