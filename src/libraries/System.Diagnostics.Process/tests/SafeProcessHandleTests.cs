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
            ProcessStartOptions options = new("hostname");

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task CanRedirectOutputToPipe(bool readAsync)
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd") { Arguments = { "/c", "echo Test" } }
                : new("sh") { Arguments = { "-c", "echo 'Test'" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, asyncRead: readAsync);

            using (readPipe)
            using (writePipe)
            using (SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: writePipe, error: null))
            using (FileStream fileStream = new(readPipe, FileAccess.Read, bufferSize: 1, isAsync: readAsync))
            {
                // Close the parent copy of the child handle, so the pipe will signal EOF when the child exits
                writePipe.Close();

                using (StreamReader streamReader = new(fileStream))
                {
                    string output = readAsync
                        ? await streamReader.ReadToEndAsync()
                        : streamReader.ReadToEnd();

                    Assert.Equal(OperatingSystem.IsWindows() ? "Test\r\n" : "Test\n", output);
                }

                ProcessExitStatus processExitStatus = await processHandle.WaitForExitAsync();

                Assert.Equal(0, processExitStatus.ExitCode);
                Assert.Null(processExitStatus.Signal);
                Assert.False(processExitStatus.Canceled);
            }
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task CanRedirectOutputAndErrorToDifferentPipes(bool readAsync)
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd") { Arguments = { "/c", "echo Hello from stdout && echo Error from stderr 1>&2" } }
                : new("sh") { Arguments = { "-c", "echo 'Hello from stdout' && echo 'Error from stderr' >&2" } };

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite, asyncRead: readAsync);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle errorRead, out SafeFileHandle errorWrite, asyncRead: readAsync);

            using (outputRead)
            using (outputWrite)
            using (errorRead)
            using (errorWrite)
            using (SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: outputWrite, error: errorWrite))
            using (FileStream outputStream = new(outputRead, FileAccess.Read, bufferSize: 1, isAsync: readAsync))
            using (FileStream errorStream = new(errorRead, FileAccess.Read, bufferSize: 1, isAsync: readAsync))
            {
                // Close the parent copy of the child handle, so the pipe will signal EOF when the child exits
                outputWrite.Close();
                errorWrite.Close();

                using (StreamReader outputReader = new(outputStream))
                using (StreamReader errorReader = new(errorStream))
                {
                    Task<string> outputTask = outputReader.ReadToEndAsync();
                    Task<string> errorTask = errorReader.ReadToEndAsync();

                    Assert.Equal(OperatingSystem.IsWindows() ? "Hello from stdout \r\n" : "Hello from stdout\n", await outputTask);
                    Assert.Equal(OperatingSystem.IsWindows() ? "Error from stderr \r\n" : "Error from stderr\n", await errorTask);
                }

                ProcessExitStatus exitStatus = await processHandle.WaitForExitAsync();

                Assert.Equal(0, exitStatus.ExitCode);
                Assert.Null(exitStatus.Signal);
                Assert.False(exitStatus.Canceled);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task CanRedirectToTheSameFileHandle(bool andStdError)
        {
            string tempFile = Path.GetTempFileName();

            try
            {
                // Write test data to the file before opening the handle to avoid lock conflicts
                File.WriteAllText(tempFile, "Test Line\n");

                SafeFileHandle fileHandle = File.OpenHandle(
                    tempFile,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                // Start a process that reads from stdin and writes to stdout
                // We use 'cmd /c findstr .*' which reads stdin and outputs matching lines
                ProcessStartOptions options = new("cmd.exe")
                {
                    Arguments = { "/c", "findstr", ".*" }
                };

                // Both stdin and stdout use the same file handle
                using SafeProcessHandle processHandle = SafeProcessHandle.Start(
                    options,
                    input: fileHandle,
                    output: fileHandle,
                    error: andStdError ? fileHandle : null);

                // Close the file handle after starting the process to avoid file locking issues
                fileHandle.Dispose();

                // Wait for process to complete
                await processHandle.WaitForExitAsync();

                // Read the output from the file
                string output = File.ReadAllText(tempFile);

                Assert.Equal("Test Line\nTest Line\n", output, ignoreLineEndingDifferences: true);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task CanRedirectToInheritedHandles(bool useAsync)
        {
            ProcessStartOptions options = OperatingSystem.IsWindows()
                ? new("cmd") { Arguments = { "/c", "exit 42" } }
                : new("sh") { Arguments = { "-c", "exit 42" } };

            using SafeFileHandle inputHandle = Console.OpenStandardInputHandle();
            using SafeFileHandle outputHandle = Console.OpenStandardOutputHandle();
            using SafeFileHandle errorHandle = Console.OpenStandardErrorHandle();

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, inputHandle, outputHandle, errorHandle);

            ProcessExitStatus exitStatus = useAsync
                ? await processHandle.WaitForExitAsync()
                : processHandle.WaitForExit();

            Assert.Equal(42, exitStatus.ExitCode);
            Assert.Null(exitStatus.Signal);
            Assert.False(exitStatus.Canceled);
        }

        [Fact]
        public static async Task CanImplementPiping()
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe);
            string? tempFile = null;

            try
            {
                tempFile = Path.GetTempFileName();

                ProcessStartOptions producer, consumer;
                string expectedOutput;

                if (OperatingSystem.IsWindows())
                {
                    producer = new("cmd")
                    {
                        Arguments = { "/c", "echo hello world & echo test line & echo another test" }
                    };
                    consumer = new("findstr")
                    {
                        Arguments = { "test" }
                    };
                    // findstr adds a trailing space on Windows
                    expectedOutput = "test line \nanother test\n";
                }
                else
                {
                    // Unix: use sh with printf to avoid echo implementation differences
                    producer = new("sh")
                    {
                        Arguments = { "-c", "printf 'hello world\\ntest line\\nanother test\\n'" }
                    };
                    consumer = new("grep")
                    {
                        Arguments = { "test" }
                    };
                    // grep doesn't add trailing spaces
                    expectedOutput = "test line\nanother test\n";
                }

                using (SafeProcessHandle producerHandle = SafeProcessHandle.Start(producer, input: null, output: writePipe, error: null))
                using (SafeFileHandle outputHandle = File.OpenHandle(tempFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    writePipe.Close(); // close the parent copy of child handle

                    using (SafeProcessHandle consumerHandle = SafeProcessHandle.Start(consumer, readPipe, outputHandle, error: null))
                    {
                        readPipe.Close(); // close the parent copy of child handle 

                        await producerHandle.WaitForExitAsync();
                        await consumerHandle.WaitForExitAsync();
                    }
                }

                string result = File.ReadAllText(tempFile);
                Assert.Equal(expectedOutput, result, ignoreLineEndingDifferences: true);
            }
            finally
            {
                readPipe.Dispose();
                writePipe.Dispose();

                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
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
            RemoteInvokeHandle invokeHandle = RemoteExecutor.Invoke(method, arg: arg, remoteInvokeOptions);

            // RemoteInvokeHandle requires the users to Dispose it, otherwise its finalizer throws.
            // There is no process associated with this object (because we have not started any), sod Dispose throws.
            // That is why we disable this check by supressing the finalizer.
            GC.SuppressFinalize(invokeHandle);

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
    }
}
