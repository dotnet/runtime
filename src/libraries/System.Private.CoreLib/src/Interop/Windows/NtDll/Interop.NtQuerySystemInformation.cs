// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll)]
        internal static unsafe extern int NtQuerySystemInformation(int SystemInformationClass, void* SystemInformation, int SystemInformationLength, uint* ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_LEAP_SECOND_INFORMATION
        {
            public bool Enabled;
            public uint Flags;
        }

        internal const int SystemLeapSecondInformation = 206;
    }
}
