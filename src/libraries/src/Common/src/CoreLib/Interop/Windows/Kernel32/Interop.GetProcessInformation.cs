// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal const int ProcessLeapSecondInfo = 8;

        internal struct PROCESS_LEAP_SECOND_INFO
        {
            public uint Flags;
            public uint Reserved;
        }

        [DllImport(Libraries.Kernel32)]
        internal static unsafe extern Interop.BOOL GetProcessInformation(IntPtr hProcess, int ProcessInformationClass, void* ProcessInformation, int ProcessInformationSize);
    }
}
