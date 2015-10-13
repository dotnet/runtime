// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct CpuSetBits
        {
            private fixed ulong Bits[16];
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int SchedSetAffinity(int pid, ref CpuSetBits mask);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int SchedGetAffinity(int pid, out CpuSetBits mask);

        [DllImport(Libraries.SystemNative)]
        internal static extern void CpuSet(int cpu, ref CpuSetBits set);

        [DllImport(Libraries.SystemNative)]
        internal static extern bool CpuIsSet(int cpu, ref CpuSetBits set);

    }
}
