// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;


namespace System.Threading
{
    // NOTE: Only supported on Linux for now.
    //       Most OS have support for futex-like APIs and should be added in the future.
    //       (ex: OSX has `os_sync_wait_on_address`, but support may vary by OS version)

    /// <summary>
    /// A compare-and-wait synchronization primitive.
    /// Provides simple functionality to block and wake threads.
    /// </summary>
    internal static unsafe class LowLevelFutex
    {
        internal static void WaitOnAddress(int* address, int comparand)
        {
            Interop.Sys.LowLevelFutex_WaitOnAddress(address, comparand);
        }

        internal static bool WaitOnAddressTimeout(int* address, int comparand, int milliseconds)
        {
            Debug.Assert(milliseconds >= -1);
            if (milliseconds == -1)
            {
                WaitOnAddress(address, comparand);
                return true;
            }

            return Interop.Sys.LowLevelFutex_WaitOnAddressTimeout(address, comparand, milliseconds);
        }

        internal static void WakeByAddressSingle(int* address)
        {
            Interop.Sys.LowLevelFutex_WakeByAddressSingle(address);
        }
    }
}
