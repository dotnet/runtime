// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class SafeProcessHandleTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void SendSignal_SIGTERM_TerminatesProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            processHandle.Signal(PosixSignal.SIGTERM);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(1));

            Assert.Equal(PosixSignal.SIGTERM, exitStatus.Signal);
            Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void SendSignal_SIGINT_TerminatesProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            processHandle.Signal(PosixSignal.SIGINT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(1));

            Assert.Equal(PosixSignal.SIGINT, exitStatus.Signal);
            Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void SendSignal_HardcodedSigkillValue_TerminatesProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            processHandle.Signal((PosixSignal)9);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(1));

            Assert.Equal(PosixSignal.SIGKILL, exitStatus.Signal);
            Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void Signal_InvalidSignal_ThrowsWin32Exception()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            PosixSignal invalidSignal = (PosixSignal)100;

            Win32Exception exception = Assert.Throws<Win32Exception>(() => processHandle.Signal(invalidSignal));

            // EINVAL error code is 22 on Unix systems
            Assert.Equal(22, exception.NativeErrorCode);

            processHandle.Kill();
            processHandle.WaitForExit();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void SendSignal_ToExitedProcess_ThrowsWin32Exception()
        {
            ProcessStartOptions options = new("echo") { Arguments = { "test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            processHandle.WaitForExit();

            Win32Exception exception = Assert.Throws<Win32Exception>(() => processHandle.Signal(PosixSignal.SIGTERM));

            // ESRCH error code is 3 on Unix systems
            Assert.Equal(3, exception.NativeErrorCode);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public static void ProcessId_WhenNotProvided_ThrowsPlatformNotSupportedException_OnPlatformsThatDontSupportProcessDesrcriptors()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            using SafeProcessHandle started = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            using SafeProcessHandle copy = new(started.DangerousGetHandle(), ownsHandle: false);
            Assert.Throws<PlatformNotSupportedException>(() => copy.ProcessId);

            started.Kill();
            Assert.True(started.TryWaitForExit(TimeSpan.FromMilliseconds(300), out _));
        }
    }
}
