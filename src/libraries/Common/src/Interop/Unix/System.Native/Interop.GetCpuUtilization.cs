// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessCpuInformation
        {
            internal ulong lastRecordedCurrentTime;
            internal ulong lastRecordedKernelTime;
            internal ulong lastRecordedUserTime;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetCpuUtilization")]
        internal static extern int GetCpuUtilization(ref ProcessCpuInformation previousCpuInfo);
    }
}
