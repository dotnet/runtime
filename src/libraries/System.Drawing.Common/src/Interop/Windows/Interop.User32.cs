// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

internal static partial class Interop
{
    internal static partial class User32
    {
        [GeneratedDllImport(Libraries.User32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial IntPtr LoadIcon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hInst, IntPtr iconId);

        [GeneratedDllImport(Libraries.User32, ExactSpelling = true, SetLastError = true)]
        internal static partial bool DestroyIcon(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hIcon);

        [GeneratedDllImport(Libraries.User32, ExactSpelling = true, SetLastError = true)]
        internal static partial IntPtr CopyImage(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hImage, int uType, int cxDesired, int cyDesired, int fuFlags);

        [GeneratedDllImport(Libraries.User32, ExactSpelling = true, SetLastError = true)]
        internal static partial bool GetIconInfo(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hIcon, ref ICONINFO info);

        [GeneratedDllImport(Libraries.User32, SetLastError = true)]
        public static partial int GetSystemMetrics(int nIndex);

        [GeneratedDllImport(Libraries.User32, CharSet = CharSet.Auto, ExactSpelling = true, SetLastError = true)]
        internal static partial bool DrawIconEx(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int x, int y,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hIcon, int width, int height, int iStepIfAniCursor,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hBrushFlickerFree, int diFlags);

        [GeneratedDllImport(Libraries.User32, SetLastError = true)]
        internal static unsafe partial IntPtr CreateIconFromResourceEx(byte* pbIconBits, uint cbIconBits, bool fIcon, int dwVersion, int csDesired, int cyDesired, int flags);

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
