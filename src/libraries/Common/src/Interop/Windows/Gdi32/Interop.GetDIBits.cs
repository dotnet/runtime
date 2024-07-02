// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [LibraryImport(Libraries.Gdi32)]
        internal static partial int GetDIBits(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hbm, int arg1, int arg2, IntPtr arg3, ref BITMAPINFO_FLAT bmi, int arg5);

    }
}
