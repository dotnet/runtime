// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessCpuInformation
        {
            ulong lastRecordedCurrentTime;
            ulong lastRecordedKernelTime;
            ulong lastRecordedUserTime;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetCpuUtilization")]
        internal static extern unsafe int GetCpuUtilization(ref ProcessCpuInformation previousCpuInfo);
    }
}
