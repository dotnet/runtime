// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentProcessorCount
    {
        [Fact]
        public void ProcessorCountTest()
        {
            //arrange
            int expected;

            if(Interop.IsWindows)
            {
                SYSTEM_INFO sysInfo = new SYSTEM_INFO();
                SYSTEM_INFO.GetSystemInfo(ref sysInfo);
                expected = sysInfo.dwNumberOfProcessors;
            }
            else
            {
                int _SC_NPROCESSORS_ONLN = Interop.IsOSX ? 58 : 84;
                expected = (int)sysconf(_SC_NPROCESSORS_ONLN);                
            }

            //act
            int actual = Environment.ProcessorCount;

            //assert
            Assert.True(actual > 0);
            Assert.Equal(expected, actual);
        }

        [DllImport("libc")]
        private static extern long sysconf(int name);

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
            [DllImport("api-ms-win-core-sysinfo-l1-1-0.dll", SetLastError = true)]
            internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
        }


    }

    

    
}
