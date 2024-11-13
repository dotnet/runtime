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
        [LibraryImport(Libraries.User32, EntryPoint = "LoadIconW", SetLastError = true)]
        internal static partial IntPtr LoadIcon(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hInst, IntPtr iconId);
    }
}
