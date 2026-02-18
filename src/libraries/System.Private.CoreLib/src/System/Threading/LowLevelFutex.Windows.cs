// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// A compare-and-wait synchronization primitive.
    /// Provides simple functionality to block and wake threads.
    /// </summary>
    internal static unsafe class LowLevelFutex
    {
        internal static void WaitOnAddress(int* address, int comparand)
        {
            Interop.BOOL result = Interop.Mincore.WaitOnAddress(address, &comparand, sizeof(int), -1);
            // assert success, but in release treat unexpected results as spurious wakes
            Debug.Assert(result == Interop.BOOL.TRUE);
        }

        internal static bool WaitOnAddressTimeout(int* address, int comparand, int milliseconds)
        {
            Interop.BOOL result = Interop.Mincore.WaitOnAddress(address, &comparand, sizeof(int), milliseconds);
            if (result == Interop.BOOL.TRUE)
            {
                // normal or spurious wake
                return true;
            }

            int lastError = Marshal.GetLastWin32Error();
            Debug.Assert(lastError == Interop.Errors.ERROR_TIMEOUT);
            if (lastError == Interop.Errors.ERROR_TIMEOUT)
            {
                // timeout
                return false;
            }

            // in release treat unexpected results as spurious wakes
            return true;
        }

        internal static void WakeByAddressSingle(int* address)
        {
            Interop.Mincore.WakeByAddressSingle(address);
        }
    }
}
