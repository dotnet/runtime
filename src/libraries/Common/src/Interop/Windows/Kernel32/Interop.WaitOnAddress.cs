// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport("API-MS-Win-Core-Synch-l1-2-0.dll")]
        internal static unsafe partial BOOL WaitOnAddress(void* Address, void* CompareAddress, nint AddressSize, int dwMilliseconds);

        [SuppressGCTransition]
        [LibraryImport("API-MS-Win-Core-Synch-l1-2-0.dll")]
        internal static unsafe partial void WakeByAddressSingle(void* Address);
    }
}
