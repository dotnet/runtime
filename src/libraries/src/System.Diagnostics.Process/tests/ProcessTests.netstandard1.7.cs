// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void TestHandleCount()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.HandleCount > 0);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void TestRespondingWindows()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.Responding);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        private void TestWindowApisUnix()
        {
            // This tests the hardcoded implementations of these APIs on Unix.
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.Responding);
                Assert.Equal(string.Empty, p.MainWindowTitle);
                Assert.False(p.CloseMainWindow());
                Assert.Throws<InvalidOperationException>(()=>p.WaitForInputIdle());
            }
        }

        [Fact]
        public void TestNonpagedSystemMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.NonpagedSystemMemorySize);
        }

        [Fact]
        public void TestPagedMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.PagedMemorySize);
        }

        [Fact]
        public void TestPagedSystemMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.PagedSystemMemorySize);
        }

        [Fact]
        public void TestPeakPagedMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakPagedMemorySize);
        }

        [Fact]
        public void TestPeakVirtualMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakVirtualMemorySize);
        }

        [Fact]
        public void TestPeakWorkingSet()
        {
            AssertNonZeroWindowsZeroUnix(_process.PeakWorkingSet);
        }

        [Fact]
        public void TestPrivateMemorySize()
        {
            AssertNonZeroWindowsZeroUnix(_process.PrivateMemorySize);
        }

        [Fact]
        public void TestVirtualMemorySize()
        {
            Assert.Equal(unchecked((int)_process.VirtualMemorySize64), _process.VirtualMemorySize);
        }

        [Fact]
        public void TestWorkingSet()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // resident memory can be 0 on OSX.
                Assert.True(_process.WorkingSet >= 0);
                return;
            }

            Assert.True(_process.WorkingSet > 0);
        }
    }
}
