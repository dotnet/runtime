// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.Windows)]
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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("hostname")
                : new("pwd");

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
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(-1, exitStatus.ExitCode);
            }
            else
            {
                Assert.Equal(PosixSignal.SIGKILL, exitStatus.Signal);
                // Exit code for signal termination is 128 + signal_number (native signal number, not enum value)
                Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
            }
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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static void TryWaitForExit_ReturnsTrueWhenProcessExitsBeforeTimeout()
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

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
            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(-1, exitStatus.ExitCode);
            }
            else
            {
                // On Unix, the process should have been killed with SIGKILL
                Assert.Equal(PosixSignal.SIGKILL, exitStatus.Signal);
                // Exit code for signal termination is 128 + signal_number (native signal number, not enum value)
                Assert.True(exitStatus.ExitCode > 128, $"Exit code {exitStatus.ExitCode} should indicate signal termination (>128)");
            }
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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = await processHandle.WaitForExitAsync();

            Assert.Equal(0, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [Fact]
        public static async Task WaitForExitOrKillOnCancellationAsync_CompletesNormallyWhenProcessExits()
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

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
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };

            options.KillOnParentExit = true;

            Assert.True(options.KillOnParentExit);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            Assert.Equal(0, exitStatus.ExitCode);
        }

        [Fact]
        public static void KillOnParentExit_DefaultsToFalse()
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd.exe") { Arguments = { "/c", "echo test" } }
                : new("echo") { Arguments = { "test" } };
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Signal_UnsupportedSignal_ThrowsPlatformNotSupportedException()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => processHandle.Signal(PosixSignal.SIGTERM));
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

        [ConditionalTheory]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task InheritedHandles_DontLeak_ToNextSpawnedProcess(bool ownsHandle, bool spawnSecondProcessUsingOldApi)
        {
            ProcessStartOptions options = CreateTenSecondSleep();

            using AnonymousPipeServerStream server = new(PipeDirection.In);
            // Important: the handle should not leak no matter if it's owned (and can be disposed) or not.
            using SafeFileHandle handleToInherit = new(server.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle);
            options.InheritedHandles.Add(handleToInherit);

            using SafeProcessHandle firstProcessHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            // The first process may need to make handleToInherit inheritable for the time of spawning the process.
            // When it exits, the handle should not be inheritable anymore.

            using SafeProcessHandle? secondHandle = spawnSecondProcessUsingOldApi
                ? null
                : SafeProcessHandle.Start(CreateTenSecondSleep(), input: null, output: null, error: null);

            using Process? secondProcess = spawnSecondProcessUsingOldApi
                ? Process.Start(ToProcessStartInfo(CreateTenSecondSleep()))
                : null;

            try
            {
                // close the parent copy
                server.ClientSafePipeHandle.Dispose();
                handleToInherit.Dispose();

                // The pipe is currently opened by the child process.
                Task<int> readTask = server.ReadAsync(new byte[1], 0, 1);
                Task finishedTask = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromMilliseconds(300)));
                Assert.NotSame(finishedTask, readTask);

                firstProcessHandle.Kill();
                await firstProcessHandle.WaitForExitAsync();

                // If process handle was not inherited by the second process,
                // the handle to the pipe was closed and EOF was reported.
                finishedTask = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromMilliseconds(300))); // give some time for the pipe to be closed
                Assert.Same(finishedTask, readTask);
                Assert.Equal(0, await readTask);
            }
            finally
            {
                secondHandle?.Kill();
                secondProcess?.Kill();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task KillProcessGroup_Kills_EntireProcessTree_OnWindows(bool createNewProcessGroupForGrandChild)
        {
            // We use RemoteExecutor to build for us the file name and arguments, so we can run it using the SafeProcessHandle API.
            ProcessStartOptions options = MapToRemoteExecutorStartOptions(
                static (enabledStr) =>
                {
                    ProcessStartOptions processStartOptions = CreateTenSecondSleep();
                    processStartOptions.CreateNewProcessGroup = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartOptions, input: null, output: null, error: null);
                    Console.WriteLine(started.ProcessId);

                    // This will block the child until parent kills it.
                    Thread.Sleep(TimeSpan.FromHours(8));
                    return 0;
                },
                arg: createNewProcessGroupForGrandChild.ToString());

            options.CreateNewProcessGroup = true; // the child needs to have CreateNewProcessGroup enabled

            using IDisposable server = CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite);
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: outputWrite, error: Console.OpenStandardErrorHandle());
            outputWrite.Dispose();

            using StreamReader reader = new(new FileStream(outputRead, FileAccess.Read));
            string? line = await reader.ReadLineAsync();
            Assert.NotNull(line);
            int grandChildPid = int.Parse(line);

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.Zero);
            Assert.True(exitStatus.Canceled);

            // On Windows CreateNewProcessGroup creates a new Job that gets derived by all child processes,
            // even if they start their own group. When the parent job is terminated, entire process tree gets killed.
            // On Unix CreateNewProcessGroup creates a new Process Group that gets derived by all child processes,
            // unless they start their own group. When the parent group is killed, child process groups remain alive.
            VerifyProcessIsRunning(shouldExited: OperatingSystem.IsWindows() || !createNewProcessGroupForGrandChild, grandChildPid);
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

        private static ProcessStartInfo ToProcessStartInfo(ProcessStartOptions options)
        {
            ProcessStartInfo info = new(options.FileName);
            foreach (string argument in options.Arguments)
            {
                info.ArgumentList.Add(argument);
            }

            // Redirect STD OUT so the started process does not pollute test run output.
            info.RedirectStandardOutput = true;

            return info;
        }

        private static ProcessStartOptions MapToRemoteExecutorStartOptions(Func<string, int> method, string arg)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false, Start = false };
            _ = RemoteExecutor.Invoke(method, arg: arg, remoteInvokeOptions);

            ProcessStartOptions options = new(remoteInvokeOptions.StartInfo.FileName);
            StringBuilder argumentBuilder = new();
            bool isQuoted = false;

            foreach (char c in remoteInvokeOptions.StartInfo.Arguments)
            {
                switch (c)
                {
                    case '"' when !isQuoted:
                        isQuoted = true;
                        break;

                    case ' ' when !isQuoted:
                    case '"' when isQuoted:
                        if (argumentBuilder.Length > 0)
                        {
                            options.Arguments.Add(argumentBuilder.ToString());
                            argumentBuilder.Clear();
                        }
                        isQuoted = false;
                        break;

                    default:
                        argumentBuilder.Append(c);
                        break;
                }
            }

            if (argumentBuilder.Length > 0)
            {
                options.Arguments.Add(argumentBuilder.ToString());
                argumentBuilder.Clear();
            }

            return options;
        }

        private static IDisposable CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle)
        {
            AnonymousPipeServerStream server = new(PipeDirection.Out);

            writeHandle = new(server.SafePipeHandle.DangerousGetHandle(), ownsHandle: true);
            readHandle = new(server.ClientSafePipeHandle.DangerousGetHandle(), ownsHandle: true);

            return server;
        }
    }
}
