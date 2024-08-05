// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class User32
    {
        [LibraryImport(Libraries.User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetIconInfo(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hIcon, ref ICONINFO info);

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
