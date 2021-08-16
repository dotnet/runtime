// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class User32
    {
        [DllImport(Libraries.User32, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadIcon(HandleRef hInst, IntPtr iconId);

        [DllImport(Libraries.User32, SetLastError = true, ExactSpelling = true)]
        internal static extern bool DestroyIcon(HandleRef hIcon);

        [DllImport(Libraries.User32, SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr CopyImage(HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);

        [DllImport(Libraries.User32, SetLastError = true, ExactSpelling = true)]
        internal static extern bool GetIconInfo(HandleRef hIcon, ref ICONINFO info);

        [DllImport(Libraries.User32, SetLastError = true, ExactSpelling = true)]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport(Libraries.User32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern bool DrawIconEx(HandleRef hDC, int x, int y, HandleRef hIcon, int width, int height, int iStepIfAniCursor, HandleRef hBrushFlickerFree, int diFlags);

        [DllImport(Libraries.User32, ExactSpelling = true, SetLastError = true)]
        internal static extern unsafe IntPtr CreateIconFromResourceEx(byte* pbIconBits, uint cbIconBits, bool fIcon, int dwVersion, int csDesired, int cyDesired, int flags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ICONINFO
        {
            internal uint fIcon;
            internal uint xHotspot;
            internal uint yHotspot;
            internal IntPtr hbmMask;
            internal IntPtr hbmColor;
        }
    }
}