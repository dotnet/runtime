// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]
        public void TestHandleCount()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.HandleCount > 0);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void TestHandleCount_OSX()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.Equal(0, p.HandleCount);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Windows)]
        public void HandleCountChanges()
        {
            RemoteInvoke(() =>
            {
                Process p = Process.GetCurrentProcess();
                int handleCount = p.HandleCount;
                using (var fs1 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                using (var fs2 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                using (var fs3 = File.Open(GetTestFilePath(), FileMode.OpenOrCreate))
                {
                    p.Refresh();
                    int secondHandleCount = p.HandleCount;
                    Assert.True(handleCount < secondHandleCount);
                    handleCount = secondHandleCount;
                }
                p.Refresh();
                int thirdHandleCount = p.HandleCount;
                Assert.True(thirdHandleCount < handleCount);
                return SuccessExitCode;
            }).Dispose();
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

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Process_StartInvalidNamesTest()
        {
            Assert.Throws<InvalidOperationException>(() => Process.Start(null, "userName", new SecureString(), "thisDomain"));
            Assert.Throws<InvalidOperationException>(() => Process.Start(string.Empty, "userName", new SecureString(), "thisDomain"));
            Assert.Throws<Win32Exception>(() => Process.Start("exe", string.Empty, new SecureString(), "thisDomain"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Process_StartWithInvalidUserNamePassword()
        {
            SecureString password = AsSecureString("Value");
            Assert.Throws<Win32Exception>(() => Process.Start(GetCurrentProcessName(), "userName", password, "thisDomain"));
            Assert.Throws<Win32Exception>(() => Process.Start(GetCurrentProcessName(), Environment.UserName, password, Environment.UserDomainName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Process_StartTest()
        {
            string currentProcessName = GetCurrentProcessName();
            string userName = string.Empty;
            string domain = "thisDomain";
            SecureString password = AsSecureString("Value");

            Process p = Process.Start(currentProcessName, userName, password, domain);
            Assert.NotNull(p);
            Assert.Equal(currentProcessName, p.StartInfo.FileName);
            Assert.Equal(userName, p.StartInfo.UserName);
            Assert.Same(password, p.StartInfo.Password);
            Assert.Equal(domain, p.StartInfo.Domain);
            p.Kill();
            password.Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Process_StartWithArgumentsTest()
        {
            string currentProcessName = GetCurrentProcessName();
            string userName = string.Empty;
            string domain = Environment.UserDomainName;
            string arguments = "-xml testResults.xml";
            SecureString password = AsSecureString("Value");

            Process p = Process.Start(currentProcessName, arguments, userName, password, domain);
            Assert.NotNull(p);
            Assert.Equal(currentProcessName, p.StartInfo.FileName);
            Assert.Equal(arguments, p.StartInfo.Arguments);
            Assert.Equal(userName, p.StartInfo.UserName);
            Assert.Same(password, p.StartInfo.Password);
            Assert.Equal(domain, p.StartInfo.Domain);
            p.Kill();
            password.Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Process_StartWithDuplicatePassword()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "exe";
            psi.UserName = "dummyUser";
            psi.PasswordInClearText = "Value";
            psi.Password = AsSecureString("Value");

            Process p = new Process();
            p.StartInfo = psi;
            Assert.Throws<ArgumentException>(() => p.Start());
        }

        private string GetCurrentProcessName()
        {
            return $"{Process.GetCurrentProcess().ProcessName}.exe";
        }

        private SecureString AsSecureString(string str)
        {
            SecureString secureString = new SecureString();

            foreach (var ch in str)
            {
                secureString.AppendChar(ch);
            }

            return secureString;
        }
    }
}
