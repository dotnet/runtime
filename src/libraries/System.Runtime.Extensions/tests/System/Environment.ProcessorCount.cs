// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class EnvironmentProcessorCount
    {
        [Fact]
        public void ProcessorCount_IsPositive()
        {
            Assert.InRange(Environment.ProcessorCount, 1, int.MaxValue);
        }
        
        [PlatformSpecific(TestPlatforms.Windows)] // Uses P/Invokes to get processor information
        [Fact]
        public void ProcessorCount_Windows_MatchesGetSystemInfo()
        {
            GetSystemInfo(out SYSTEM_INFO sysInfo);
            Assert.Equal(sysInfo.dwNumberOfProcessors, Environment.ProcessorCount);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            internal int dwOemId; // This is a union of a DWORD and a struct containing 2 WORDs.
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
    }
}
