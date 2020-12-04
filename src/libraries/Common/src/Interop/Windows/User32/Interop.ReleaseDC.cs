// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32, ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        public static int ReleaseDC(HandleRef hWnd, IntPtr hDC)
        {
            int result = ReleaseDC(hWnd.Handle, hDC);
            GC.KeepAlive(hWnd.Wrapper);
            return result;
        }

        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            int result = ReleaseDC(hWnd.Handle, hDC.Handle);
            GC.KeepAlive(hWnd.Wrapper);
            GC.KeepAlive(hDC.Wrapper);
            return result;
        }
    }
}
