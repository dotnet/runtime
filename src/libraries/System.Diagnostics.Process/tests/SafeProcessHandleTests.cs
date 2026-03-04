// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public partial class SafeProcessHandleTests
    {
        // On Windows:
        // - timeout utility requires non-redirected STD IN (to handle Ctrl+C). We can't use it in CI.
        // - powershell is not available on Nano. We can't always use it.
        // - ping seems to be a workaround, but it's simple and work everywhere. The arguments are set to make it sleep for approximately 10 seconds.
        private static ProcessStartOptions CreateTenSecondSleep() => OperatingSystem.IsWindows()
            ? new("ping") { Arguments = { "127.0.0.1", "-n", "11" } }
            : new("sleep") { Arguments = { "10" } };

        [Fact]
        public static void Start_WithNoArguments_Succeeds()
        {
            ProcessStartOptions options = new("whoami");

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();
            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void Kill_KillsRunningProcess()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            bool wasKilled = processHandle.Kill();
            Assert.True(wasKilled);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.False(exitStatus.Canceled);
            Assert.Equal(-1, exitStatus.ExitCode);
        }

        [Fact]
        public static void Kill_CanBeCalledMultipleTimes()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            bool firstKill = processHandle.Kill();
            Assert.True(firstKill);

            _ = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));

            bool secondKill = processHandle.Kill();
            Assert.False(secondKill);
        }

        [Fact]
        public static void WaitForExit_Called_After_Kill_ReturnsExitCodeImmediately()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

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
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(300));

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(270), TimeSpan.FromMilliseconds(2000));
            Assert.True(exitStatus.Canceled);
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [Fact]
        public static void WaitForExit_WaitsIndefinitelyForProcessToComplete()
        {
            ProcessStartOptions options = new("cmd.exe") { Arguments = { "/c", "echo test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();

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
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
                stopwatch.Stop();

                Assert.False(exited);
                Assert.Null(exitStatus);
                Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(270), TimeSpan.FromMilliseconds(2000));
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
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(300));
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(270), TimeSpan.FromSeconds(2));
            Assert.True(exitStatus.Canceled, "Process should be marked as canceled when killed due to timeout");
            Assert.NotEqual(0, exitStatus.ExitCode);
            Assert.Equal(-1, exitStatus.ExitCode);
        }

        [Fact]
        public static async Task WaitForExitOrKillOnCancellationAsync_KillsOnCancellation_AndDoesNotThrow()
        {
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

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
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

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
        public void Open_CanWaitForExitOnOpenedProcess()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            using SafeProcessHandle started = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            using SafeProcessHandle opened = SafeProcessHandle.Open(started.ProcessId);

            opened.Kill();
            Assert.True(opened.TryWaitForExit(TimeSpan.FromMilliseconds(300), out _));
        }

        [Fact]
        public static void ProcessId_IsFetched_WhenNotProvided()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            using SafeProcessHandle started = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            using SafeProcessHandle copy = new(started.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(started.ProcessId, copy.ProcessId);

            copy.Kill();
            Assert.True(started.TryWaitForExit(TimeSpan.FromMilliseconds(300), out _));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(PosixSignal.SIGINT)]
        [InlineData(PosixSignal.SIGQUIT)]
        public void Signal_TerminatesProcessInNewProcessGroup(PosixSignal signal)
        {
            // When a process is created with CREATE_NEW_PROCESS_GROUP specified, an implicit call to SetConsoleCtrlHandler(NULL,TRUE) is made.
            // We need to re-enable the handler inside the child process.
            // Since RemoteExecutor doesn't support SafeProcessHandle APIs yet, we use PowerShell.

            const string script = """
                Add-Type -TypeDefinition @"
                using System;
                using System.Runtime.InteropServices;

                public class Kernel32 {
                    [DllImport("kernel32.dll", SetLastError = true)]
                    public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);
                }
                "@

                [Kernel32]::SetConsoleCtrlHandler([IntPtr]::Zero, $false)
                Start-Sleep -Seconds 5
                """;

            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("powershell.exe") { Arguments = { "-NoProfile", "-Command", script } }
                : CreateTenSecondSleep();

            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            bool hasExited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(200), out _); // let PWS start and set up the handler
            Assert.False(hasExited, "Process should still be running before signal is sent");

            processHandle.Signal(signal);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(3000));

            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

        [ConditionalTheory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task WaitForExitOrKill_KillsProcessGroup_WhenAvailable(bool createNewProcessGroup, bool useAsync)
        {
            // Start a shell (child) that spawns long-running process (grandchild).
            // The grandchild will inherit the file handle and keep it open.
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe")
                {
                    Arguments = { "/c", "ping", "127.0.0.1", "-n", "11" },
                }
                : new("sh")
                {
                    Arguments = { "-c", "sleep 10 & wait" },
                };

            options.CreateNewProcessGroup = createNewProcessGroup;

            // Create a pipe that will be inherited by the child process.
            using AnonymousPipeServerStream server = new(PipeDirection.In);
            options.InheritedHandles.Add(server.ClientSafePipeHandle);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);
            server.ClientSafePipeHandle.Dispose(); // close the parent copy

            // The pipe is currently opened by the child and grand child process.
            Task<int> readTask = server.ReadAsync(new byte[1], 0, 1);
            Assert.False(readTask.IsCompleted);

            TimeSpan timeout = TimeSpan.FromMilliseconds(300);
            using CancellationTokenSource cts = new(timeout);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = useAsync
                ? await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token)
                : processHandle.WaitForExitOrKillOnTimeout(timeout);

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(2000));
            Assert.NotEqual(0, exitStatus.ExitCode);

            // If process group was used, entire process tree was killed,
            // the last handle to the pipe was closed and EOF was reported.
            Task finishedTask = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromMilliseconds(300))); // give some time for the pipe to be closed
            Assert.Equal(createNewProcessGroup, finishedTask == readTask);
            Assert.Equal(createNewProcessGroup, readTask.IsCompleted);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentExits(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartOptions processStartOptions = CreateTenSecondSleep();
                    processStartOptions.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartOptions, input: null, output: null, error: null);
                    return started.ProcessId; // return grand child pid as exit code
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, remoteHandle.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentIsKilled(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartOptions processStartOptions = CreateTenSecondSleep();
                    processStartOptions.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartOptions, input: null, output: null, error: null);
                    Console.WriteLine(started.ProcessId);

                    // This will block the child until parent kills it.
                    _ = Console.ReadLine();
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.Kill();
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentCrashes(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardError = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartOptions processStartOptions = CreateTenSecondSleep();
                    processStartOptions.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartOptions, input: null, output: null, error: null);
                    Console.WriteLine(started.ProcessId);

                    // This will block the child until parent writes input.
                    _ = Console.ReadLine();

                    // Guaranteed Access Violation - write to null pointer
                    Marshal.WriteInt32(IntPtr.Zero, 42);
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.StandardInput.WriteLine("One AccessViolationException please.");
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        private static void VerifyProcessIsRunning(bool shouldExited, int processId)
        {
            try
            {
                using SafeProcessHandle grandChild = SafeProcessHandle.Open(processId);
                grandChild.Kill();
                grandChild.WaitForExit();
            }
            catch (Win32Exception) when (shouldExited)
            {
            }
        }
    }
}
