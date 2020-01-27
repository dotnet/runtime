// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        // From SYSTEM_INFORMATION_CLASS
        // Use for NtQuerySystemInformation
        internal const int SystemLeapSecondInformation = 206;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_LEAP_SECOND_INFORMATION
        {
            public BOOLEAN Enabled;
            public uint Flags;
        }
    }
}
