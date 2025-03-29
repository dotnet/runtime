// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        internal static partial IntPtr SelectObject(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hdc,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef obj);
    }
}
