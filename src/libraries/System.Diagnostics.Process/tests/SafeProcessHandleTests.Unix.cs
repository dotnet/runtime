// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class SafeProcessHandleTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void SendSignal_SIGTERM_TerminatesProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            processHandle.Signal(PosixSignal.SIGTERM);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));

            Assert.Equal(PosixSignal.SIGTERM, exitStatus.Signal);
            Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void SendSignal_SIGINT_TerminatesProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            processHandle.Signal(PosixSignal.SIGINT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));

            Assert.Equal(PosixSignal.SIGINT, exitStatus.Signal);
            Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void Signal_InvalidSignal_ThrowsArgumentOutOfRangeException()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            PosixSignal invalidSignal = (PosixSignal)100;

            Assert.Throws<ArgumentOutOfRangeException>(() => processHandle.Signal(invalidSignal));

            processHandle.Kill();
            processHandle.WaitForExit();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void SendSignal_ToExitedProcess_ThrowsWin32Exception()
        {
            ProcessStartOptions options = new("echo") { Arguments = { "test" } };

            using SafeFileHandle nullHandle = File.OpenNullHandle();
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(
                options,
                input: null,
                output: nullHandle,
                error: nullHandle);

            processHandle.WaitForExit();

            Win32Exception exception = Assert.Throws<Win32Exception>(() => processHandle.Signal(PosixSignal.SIGTERM));

            // ESRCH error code is 3 on Unix systems
            Assert.Equal(3, exception.NativeErrorCode);
        }
    }
}
