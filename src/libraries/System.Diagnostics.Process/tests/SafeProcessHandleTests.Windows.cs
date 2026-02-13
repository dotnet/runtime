// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class SafeProcessHandleTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void SendSignal_SIGINT_TerminatesProcessInNewProcessGroup()
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            ProcessStartOptions options = new("timeout")
            {
                Arguments = { "/t", "3", "/nobreak" },
                CreateNewProcessGroup = true
            };

            using SafeFileHandle stdin = Console.OpenStandardInputHandle();
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: stdin, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.Zero, out _);
            Assert.False(hasExited, "Process should still be running before signal is sent");

            processHandle.Signal(PosixSignal.SIGINT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(3000));

            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void Signal_SIGQUIT_TerminatesProcessInNewProcessGroup()
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            ProcessStartOptions options = new("timeout")
            {
                Arguments = { "/t", "3", "/nobreak" },
                CreateNewProcessGroup = true
            };

            using SafeFileHandle stdin = Console.OpenStandardInputHandle();
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: stdin, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.Zero, out _);
            Assert.False(hasExited, "Process should still be running before signal is sent");

            processHandle.Signal(PosixSignal.SIGQUIT);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(3000));

            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void Signal_UnsupportedSignal_ThrowsArgumentException()
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            ProcessStartOptions options = new("timeout")
            {
                Arguments = { "/t", "3", "/nobreak" },
                CreateNewProcessGroup = true
            };

            using SafeFileHandle stdin = Console.OpenStandardInputHandle();
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: stdin, output: null, error: null);

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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))]
        public void Kill_EntireProcessGroup_WithoutCreateNewProcessGroup_Throws()
        {
            if (Console.IsInputRedirected)
            {
                return;
            }

            ProcessStartOptions options = new("timeout")
            {
                Arguments = { "/t", "3", "/nobreak" },
                CreateNewProcessGroup = false
            };

            using SafeFileHandle stdin = Console.OpenStandardInputHandle();
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: stdin, output: null, error: null);

            Assert.Throws<InvalidOperationException>(() => processHandle.KillProcessGroup());

            Assert.True(processHandle.Kill());
        }

        [Fact]
        public void StartSuspended_ResumeCompletes()
        {
            ProcessStartOptions options = new("cmd.exe")
            {
                Arguments = { "/c", "echo test" }
            };

            using SafeProcessHandle processHandle = SafeProcessHandle.StartSuspended(options, input: null, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(200), out _);
            Assert.False(hasExited, "Suspended process should not have exited yet");

            processHandle.Resume();

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.Equal(0, exitStatus.ExitCode);
        }

        [Fact]
        public void Resume_OnNonSuspendedProcess_ThrowsInvalidOperationException()
        {
            ProcessStartOptions options = new("cmd.exe")
            {
                Arguments = { "/c", "echo test" }
            };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            Assert.Throws<InvalidOperationException>(() => processHandle.Resume());

            processHandle.WaitForExit();
        }
    }
}
