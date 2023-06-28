// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace JitTest
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        private uint _dwOemId;
        private uint _dwPageSize;
        private System.IntPtr _lpMinimumApplicationAddress;
        private System.IntPtr _lpMaximumApplicationAddress;
        private System.IntPtr _dwActiveProcessorMask;
        private uint _dwNumberOfProcessors;
        private uint _dwProcessorType;
        private uint _dwAllocationGranularity;
        private ushort _wProcessorLevel;
        private ushort _wProcessorRevision;

        [DllImport("kernel32", CharSet = CharSet.Ansi)]
        public extern static void GetSystemInfo(ref SYSTEM_INFO si);

        [Fact]
        public static int TestEntryPoint()
        {
            SYSTEM_INFO si = new SYSTEM_INFO();
            try
            {
                GetSystemInfo(ref si);
            }
            finally
            {
                Console.WriteLine(si._dwNumberOfProcessors.ToString() + " processor(s) found");
                Console.WriteLine("Allocation granularity is " + si._dwAllocationGranularity.ToString() + " for this system.");
            }
            return 100;
        }
    }
}
