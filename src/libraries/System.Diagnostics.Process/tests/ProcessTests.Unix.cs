// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
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
        public void MainWindowHandle_GetUnix_ThrowsPlatformNotSupportedException()
        {
            CreateDefaultProcess();

            Assert.Equal(IntPtr.Zero, _process.MainWindowHandle);
        }

        [Fact]
        public void TestProcessOnRemoteMachineUnix()
        {
            Process currentProcess = Process.GetCurrentProcess();

            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessesByName(currentProcess.ProcessName, "127.0.0.1"));
            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessById(currentProcess.Id, "127.0.0.1"));
        }

        [Theory]
        [MemberData(nameof(MachineName_Remote_TestData))]
        public void GetProcessesByName_RemoteMachineNameUnix_ThrowsPlatformNotSupportedException(string machineName)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessesByName(currentProcess.ProcessName, machineName));
        }

        [Fact]
        public void TestRootGetProcessById()
        {
            Process p = Process.GetProcessById(1);
            Assert.Equal(1, p.Id);
        }

        [Fact]
        public void TestUseShellExecute_Unix_Succeeds()
        {
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = "exit", Arguments = "42" }))
            {
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(42, p.ExitCode);
            }
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestPriorityClassUnix()
        {
            CreateDefaultProcess();

            ProcessPriorityClass priorityClass = _process.PriorityClass;

            _process.PriorityClass = ProcessPriorityClass.Idle;
            Assert.Equal(_process.PriorityClass, ProcessPriorityClass.Idle);

            try
            {
                _process.PriorityClass = ProcessPriorityClass.High;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.High);

                _process.PriorityClass = ProcessPriorityClass.Normal;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.Normal);

                _process.PriorityClass = priorityClass;
            }
            catch (Win32Exception ex)
            {
                Assert.True(!PlatformDetection.IsSuperUser, $"Failed even though superuser {ex.ToString()}");
            }
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestBasePriorityOnUnix()
        {
            CreateDefaultProcess();

            ProcessPriorityClass originalPriority = _process.PriorityClass;
            Assert.Equal(ProcessPriorityClass.Normal, originalPriority);

            SetAndCheckBasePriority(ProcessPriorityClass.Idle, 19);

            try
            {
                SetAndCheckBasePriority(ProcessPriorityClass.Normal, 0);
                SetAndCheckBasePriority(ProcessPriorityClass.High, -11);
                _process.PriorityClass = originalPriority;
            }
            catch (Win32Exception ex)
            {
                Assert.True(!PlatformDetection.IsSuperUser, $"Failed even though superuser {ex.ToString()}");
            }
        }

        [Fact]
        public void TestStartOnUnixWithBadPermissions()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 644)); // no execute permissions

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(path));
            Assert.NotEqual(0, e.NativeErrorCode);
        }

        [Fact]
        public void TestStartOnUnixWithBadFormat()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 744)); // execute permissions

            using (Process p = Process.Start(path))
            {
                p.WaitForExit();
                Assert.NotEqual(0, p.ExitCode);
            }
        }

        [DllImport("libc")]
        private static extern int chmod(string path, int mode);
    }
}
