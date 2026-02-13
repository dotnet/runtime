// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public partial class SafeProcessHandleTests
    {
        [Fact]
        public static void GetProcessId_ReturnsValidPid()
        {
            ProcessStartOptions info = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(info, input: null, output: null, error: null);
            int pid = processHandle.ProcessId;

            Assert.NotEqual(0, pid);
            Assert.NotEqual(-1, pid);
            Assert.True(pid > 0, "Process ID should be a positive integer");

            nint handleValue = processHandle.DangerousGetHandle();
            Assert.NotEqual(handleValue, (nint)pid);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();
            Assert.Equal(0, exitStatus.ExitCode);
            Assert.Null(exitStatus.Signal);
            Assert.False(exitStatus.Canceled);
        }

        [Fact]
        public static void Kill_KillsRunningProcess()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool wasKilled = processHandle.Kill();
            Assert.True(wasKilled);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.False(exitStatus.Canceled);
            Assert.Equal(-1, exitStatus.ExitCode);
        }

        [Fact]
        public static void Kill_CanBeCalledMultipleTimes()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool firstKill = processHandle.Kill();
            Assert.True(firstKill);

            _ = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));

            bool secondKill = processHandle.Kill();
            Assert.False(secondKill);
        }

        [Fact]
        public static void WaitForExit_Called_After_Kill_ReturnsExitCodeImmediately()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool wasKilled = processHandle.Kill();
            Assert.True(wasKilled);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(3));

            Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Assert.False(exitStatus.Canceled);
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public static void Kill_OnAlreadyExitedProcess_ReturnsFalse()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();
            Assert.Equal(0, exitStatus.ExitCode);

            bool wasKilled = processHandle.Kill();
            Assert.False(wasKilled);
        }

        [Fact]
        public static void WaitForExitOrKillOnTimeout_KillsOnTimeout()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(300));

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(290), TimeSpan.FromMilliseconds(2000));
            Assert.True(exitStatus.Canceled);
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public static void WaitForExit_WaitsIndefinitelyForProcessToComplete()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExit();
            stopwatch.Stop();

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void TryWaitForExit_ReturnsTrueWhenProcessExitsBeforeTimeout()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool exited = processHandle.TryWaitForExit(TimeSpan.FromSeconds(5), out ProcessExitStatus? exitStatus);

            Assert.True(exited);
            Assert.NotNull(exitStatus);
            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void TryWaitForExit_ReturnsFalseWhenProcessDoesNotExitBeforeTimeout()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
                stopwatch.Stop();

                Assert.False(exited);
                Assert.Null(exitStatus);
                Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(290), TimeSpan.FromMilliseconds(2000));
            }
            finally
            {
                processHandle.Kill();
                processHandle.WaitForExit();
            }
        }

        [Fact]
        public static void WaitForExitOrKillOnTimeout_DoesNotKillWhenProcessExitsBeforeTimeout()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled, "Process should not be marked as canceled when it exits normally before timeout");
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void WaitForExitOrKillOnTimeout_KillsAndWaitsWhenTimeoutOccurs()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 10" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(300));
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(290), TimeSpan.FromSeconds(2));
            Assert.True(exitStatus.Canceled, "Process should be marked as canceled when killed due to timeout");
            Assert.NotEqual(0, exitStatus.ExitCode);
            Assert.Equal(-1, exitStatus.ExitCode);
        }

        [Fact]
        public static async Task WaitForExitOrKillOnCancellationAsync_KillsOnCancellation()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 5" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));

            ProcessExitStatus exitStatus = await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token);

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(270), TimeSpan.FromSeconds(2));
            Assert.True(exitStatus.Canceled);
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public static async Task WaitForExitAsync_ThrowsOnCancellation()
        {
            ProcessStartOptions options = new("powershell") { Arguments = { "-InputFormat", "None", "-Command", "Start-Sleep 5" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await processHandle.WaitForExitAsync(cts.Token));

                stopwatch.Stop();

                Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(2000));

                bool hasExited = processHandle.TryWaitForExit(TimeSpan.Zero, out _);
                Assert.False(hasExited, "Process should still be running after cancellation");
            }
            finally
            {
                processHandle.Kill();
            }
        }

        [Fact]
        public static async Task WaitForExitAsync_CompletesNormallyWhenProcessExits()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            ProcessExitStatus exitStatus = await processHandle.WaitForExitAsync(cts.Token);

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static async Task WaitForExitAsync_WithoutCancellationToken_CompletesNormally()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = await processHandle.WaitForExitAsync();

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static async Task WaitForExitOrKillOnCancellationAsync_CompletesNormallyWhenProcessExits()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
            ProcessExitStatus exitStatus = await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token);

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void KillOnParentExit_CanBeSetToTrue()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" }, KillOnParentExit = true };

            Assert.True(options.KillOnParentExit);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.Equal(0, exitStatus.ExitCode);
        }

        [Fact]
        public static void KillOnParentExit_DefaultsToFalse()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };
            Assert.False(options.KillOnParentExit);
        }

        [Fact]
        public static void Open_InvalidProcessId_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SafeProcessHandle.Open(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SafeProcessHandle.Open(-1));
        }

        [Fact]
        public static void Open_CurrentProcess_Succeeds()
        {
            int currentPid = Environment.ProcessId;
            using SafeProcessHandle handle = SafeProcessHandle.Open(currentPid);

            Assert.False(handle.IsInvalid);
            Assert.Equal(currentPid, handle.ProcessId);
        }
    }
}
