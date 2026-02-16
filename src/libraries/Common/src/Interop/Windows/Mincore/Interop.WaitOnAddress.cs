// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Mincore
    {
        [LibraryImport(Libraries.Synch, SetLastError = true)]
        internal static unsafe partial BOOL WaitOnAddress(void* Address, void* CompareAddress, nint AddressSize, int dwMilliseconds);

        [LibraryImport(Libraries.Synch)]
        internal static unsafe partial void WakeByAddressSingle(void* Address);
    }
}
