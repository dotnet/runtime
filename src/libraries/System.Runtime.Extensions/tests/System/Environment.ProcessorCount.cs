// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class EnvironmentProcessorCount
    {
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to get processor information
        [Fact]
        public void Windows_ProcessorCountTest()
        {
            //arrange
            SYSTEM_INFO sysInfo = new SYSTEM_INFO();
            GetSystemInfo(ref sysInfo);
            int expected = sysInfo.dwNumberOfProcessors;

            //act
            int actual = Environment.ProcessorCount;

            //assert
            Assert.True(actual > 0);
            Assert.Equal(expected, actual);
        }

#if Unix
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes to get processor information
        [Fact]
        public void Unix_ProcessorCountTest()
        {
            //arrange
            int _SC_NPROCESSORS_ONLN =
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? 84 :
                RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD")) ? 1002 :
                58;
            int expected = (int)sysconf(_SC_NPROCESSORS_ONLN);

            //act
            int actual = Environment.ProcessorCount;

            //assert
            Assert.True(actual > 0);
            Assert.Equal(expected, actual);
        }

        [DllImport("libc")]
        private static extern long sysconf(int name);
#endif

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

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
