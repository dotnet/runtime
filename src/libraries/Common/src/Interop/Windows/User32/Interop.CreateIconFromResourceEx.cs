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
        internal static unsafe partial IntPtr CreateIconFromResourceEx(byte* pbIconBits, uint cbIconBits, [MarshalAs(UnmanagedType.Bool)] bool fIcon, int dwVersion, int csDesired, int cyDesired, int flags);
    }
}
