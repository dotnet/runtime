// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelFutex_WaitOnAddress")]
        internal static unsafe partial void LowLevelFutex_WaitOnAddress(int* address, int comparand);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelFutex_WaitOnAddressTimeout")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool LowLevelFutex_WaitOnAddressTimeout(int* address, int comparand, int timeoutMilliseconds);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelFutex_WakeByAddressSingle")]
        internal static unsafe partial void LowLevelFutex_WakeByAddressSingle(int* address);
    }
}
