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
        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal ushort wProcessorArchitecture;
            internal ushort wReserved;
            internal int dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel;
            internal short wProcessorRevision;
        }

        internal enum ProcessorArchitecture : ushort
        {
            Processor_Architecture_INTEL = 0,
            Processor_Architecture_ARM = 5,
            Processor_Architecture_IA64 = 6,
            Processor_Architecture_AMD64 = 9,
            Processor_Architecture_ARM64 = 12,
            Processor_Architecture_UNKNOWN = 0xFFFF
        }
    }
}
