// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public readonly partial struct DateTime
    {
        private static unsafe bool SystemSupportsLeapSeconds()
        {
            Interop.Kernel32.PROCESS_LEAP_SECOND_INFO info;

            // Store apps don't have access to an API that would let us find out whether leap seconds have been
            // disabled by policy: this implementation will produce slightly different results from what
            // we have for Win32. If GetProcessInformation succeeds, we have to act as if leap seconds existed.
            // They could still have been disabled by policy, but we have no way to check for that.
            return Interop.Kernel32.GetProcessInformation(
                Interop.Kernel32.GetCurrentProcess(),
                Interop.Kernel32.ProcessLeapSecondInfo,
                &info,
                sizeof(Interop.Kernel32.PROCESS_LEAP_SECOND_INFO)) != Interop.BOOL.FALSE;
        }
    }
}
