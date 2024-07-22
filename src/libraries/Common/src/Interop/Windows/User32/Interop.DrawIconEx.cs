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
        internal static partial bool DrawIconEx(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hDC, int x, int y,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hIcon, int width, int height, int iStepIfAniCursor,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hBrushFlickerFree, int diFlags);
    }
}
