// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct LingerOption
        {
            public int OnOff;   // Non-zero to enable linger
            public int Seconds; // Number of seconds to linger for
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetLingerOption")]
        internal static unsafe partial Error GetLingerOption(SafeHandle socket, LingerOption* option);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetLingerOption")]
        internal static unsafe partial Error SetLingerOption(SafeHandle socket, LingerOption* option);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetLingerOption")]
        internal static unsafe partial Error SetLingerOption(IntPtr socket, LingerOption* option);
    }
}
