// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System
{
    public readonly partial struct DateTime
    {
        private static unsafe bool SystemSupportsLeapSeconds()
        {
            Interop.NtDll.SYSTEM_LEAP_SECOND_INFORMATION slsi;

            return Interop.NtDll.NtQuerySystemInformation(
                Interop.NtDll.SystemLeapSecondInformation,
                (void *) &slsi,
                sizeof(Interop.NtDll.SYSTEM_LEAP_SECOND_INFORMATION),
                null) == 0 && slsi.Enabled;
        }
    }
}
