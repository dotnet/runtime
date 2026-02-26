// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class SafeProcessHandleTests
    {
        [Fact]
        public void SendSignal_SIGINT_TerminatesProcessInNewProcessGroup()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.Zero, out _);
            Assert.False(hasExited, "Process should still be running before signal is sent");

            processHandle.Signal(PosixSignal.SIGINT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(3000));

            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public void Signal_SIGQUIT_TerminatesProcessInNewProcessGroup()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.Zero, out _);
            Assert.False(hasExited, "Process should still be running before signal is sent");

            processHandle.Signal(PosixSignal.SIGQUIT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(3000));

            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public void Signal_UnsupportedSignal_ThrowsArgumentException()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            try
            {
                Assert.Throws<ArgumentException>(() => processHandle.Signal(PosixSignal.SIGTERM));
            }
            finally
            {
                processHandle.Kill();
                processHandle.WaitForExit();
            }
        }

        [Fact]
        public void CreateNewProcessGroup_CanBeSetToTrue()
        {
            ProcessStartOptions options = new("cmd.exe")
            {
                Arguments = { "/c", "echo test" },
                CreateNewProcessGroup = true
            };

            Assert.True(options.CreateNewProcessGroup);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.Equal(0, exitStatus.ExitCode);
        }
    }
}
