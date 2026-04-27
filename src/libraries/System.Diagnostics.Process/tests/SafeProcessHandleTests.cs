// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class SafeProcessHandleTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CanStartProcess()
        {
            Process process = CreateProcess(static () =>
            {
                Console.WriteLine("ping");

                Assert.Equal("pong", Console.ReadLine()); // this will block until we receive input

                return RemoteExecutor.SuccessExitCode;
            });

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadPipe, out SafeFileHandle outputWritePipe);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle inputReadPipe, out SafeFileHandle inputWritePipe);

            using (outputReadPipe)
            using (outputWritePipe)
            using (inputReadPipe)
            using (inputWritePipe)
            {
                process.StartInfo.StandardInputHandle = inputReadPipe;
                process.StartInfo.StandardOutputHandle = outputWritePipe;

                using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
                // close the parent copies of child handles
                outputWritePipe.Close();
                inputReadPipe.Close();

                using StreamReader streamReader = new(new FileStream(outputReadPipe, FileAccess.Read, bufferSize: 1, outputReadPipe.IsAsync));
                Assert.Equal("ping", streamReader.ReadLine());

                using StreamWriter streamWriter = new(new FileStream(inputWritePipe, FileAccess.Write, bufferSize: 1, inputWritePipe.IsAsync))
                {
                    AutoFlush = true
                };

                streamWriter.WriteLine("pong");

                ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
                Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
                Assert.False(exitStatus.Canceled);
            }
        }

        [Fact]
        public void ProcessId_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.ProcessId);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // We don't use pidfd on Unix yet
        public void CanGetProcessIdForCopyOfTheHandle()
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "exit 42" } }
                : new("sh") { ArgumentList = { "-c", "exit 42" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);
            Assert.NotEqual(0, processHandle.ProcessId);

            using SafeProcessHandle copy = new(processHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(processHandle.ProcessId, copy.ProcessId);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void Start_WithRedirectedStreams_ThrowsInvalidOperationException(
            bool redirectInput, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("hostname")
            {
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError,
            };

            Assert.Throws<InvalidOperationException>(() => SafeProcessHandle.Start(startInfo));
        }

        [Fact]
        public void Kill_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.Kill());
        }

        [Fact]
        public void Signal_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.Signal(PosixSignal.SIGKILL));
        }

        [Fact]
        public void WaitForExit_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.WaitForExit());
        }

        [Fact]
        public void TryWaitForExit_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.TryWaitForExit(TimeSpan.Zero, out _));
        }

        [Fact]
        public void WaitForExitOrKillOnTimeout_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.WaitForExitOrKillOnTimeout(TimeSpan.Zero));
        }

        [Fact]
        public async Task WaitForExitAsync_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            await Assert.ThrowsAsync<InvalidOperationException>(() => invalidHandle.WaitForExitAsync());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Kill_RunningProcess_Terminates()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            processHandle.Kill();

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Kill_AlreadyExited_DoesNotThrow()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.Start();
            SafeProcessHandle handle = process.SafeHandle;

            Assert.True(process.WaitForExit(WaitInMS));

            // Kill after the process has exited should not throw.
            handle.Kill();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Signal_SIGKILL_RunningProcess_ReturnsTrue()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

            bool delivered = processHandle.Signal(PosixSignal.SIGKILL);

            Assert.True(delivered);
            Assert.True(fetchedProcess.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Signal_SIGKILL_AlreadyExited_ReturnsFalse()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.Start();
            SafeProcessHandle handle = process.SafeHandle;

            Assert.True(process.WaitForExit(WaitInMS));

            // Signal after the process has exited should return false.
            Assert.False(handle.Signal(PosixSignal.SIGKILL));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Signal_NonSIGKILL_OnWindows_ThrowsPlatformNotSupportedException()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => processHandle.Signal(PosixSignal.SIGTERM));
            }
            finally
            {
                processHandle.Kill();
                fetchedProcess.WaitForExit(WaitInMS);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.Windows, "SIGTERM is not supported on Windows.")]
        public void Signal_SIGTERM_RunningProcess_ReturnsTrue()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

            bool delivered = processHandle.Signal(PosixSignal.SIGTERM);

            Assert.True(delivered);
            Assert.True(fetchedProcess.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Kill_HandleWithoutTerminatePermission_ThrowsWin32Exception()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            process.Start();

            try
            {
                // Open a handle with PROCESS_QUERY_LIMITED_INFORMATION only (no PROCESS_TERMINATE)
                const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
                using SafeProcessHandle limitedHandle = Interop.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);
                Assert.False(limitedHandle.IsInvalid);

                Assert.Throws<Win32Exception>(() => limitedHandle.Kill());
            }
            finally
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WaitForExit_ProcessExitsNormally_ReturnsExitCode(bool useAsync)
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            ProcessExitStatus exitStatus = useAsync
                ? await processHandle.WaitForExitAsync(cts.Token)
                : processHandle.WaitForExit();

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TryWaitForExit_ProcessExitsBeforeTimeout_ReturnsTrue()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            bool exited = processHandle.TryWaitForExit(TimeSpan.FromSeconds(30), out ProcessExitStatus? exitStatus);

            Assert.True(exited);
            Assert.NotNull(exitStatus);
            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TryWaitForExit_ProcessDoesNotExitBeforeTimeout_ReturnsFalse()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
                stopwatch.Stop();

                Assert.False(exited);
                Assert.Null(exitStatus);
                Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(5000));
            }
            finally
            {
                processHandle.Kill();
                processHandle.WaitForExit();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WaitForExitOrKill_ProcessExitsBeforeTimeout_DoesNotKill(bool useAsync)
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            ProcessExitStatus exitStatus = useAsync
                ? await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token)
                : processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WaitForExitOrKill_ProcessDoesNotExit_KillsAndReturns(bool useAsync)
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            Stopwatch stopwatch = Stopwatch.StartNew();
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));

            ProcessExitStatus exitStatus = useAsync
                ? await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token)
                : processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(300));
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10));
            Assert.True(exitStatus.Canceled);
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitForExitAsync_WithoutCancellationToken_CompletesNormally()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            ProcessExitStatus exitStatus = await processHandle.WaitForExitAsync();

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitForExitAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await processHandle.WaitForExitAsync(cts.Token));
            }
            finally
            {
                processHandle.Kill();
                processHandle.WaitForExit();
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitForExit_CalledAfterKill_ReturnsImmediately()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            processHandle.Kill();

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(5));
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Assert.NotEqual(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.Windows, "Signal property is Unix-specific")]
        [InlineData(PosixSignal.SIGKILL)]
        [InlineData(PosixSignal.SIGTERM)]
        public void WaitForExit_ProcessKilledBySignal_ReportsSignal(PosixSignal signal)
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            processHandle.Signal(signal);

            ProcessExitStatus exitStatus = processHandle.WaitForExit();

            Assert.NotNull(exitStatus.Signal);
            Assert.Equal(signal, exitStatus.Signal);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitForExit_NonChildProcess_NotSupportedOnUnix()
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                () =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.Start();

                    Console.WriteLine(grandChild.Id);

                    // Keep it alive to avoid any kind of re-parenting on Unix
                    _ = Console.ReadLine();

                    return RemoteExecutor.SuccessExitCode;
                }, remoteInvokeOptions);

            int grandChildPid = int.Parse(childHandle.Process.StandardOutput.ReadLine());

            // Obtain a Process instance before the child exits to avoid PID reuse issues.
            using Process grandchild = Process.GetProcessById(grandChildPid);

            try
            {
                await Verify(grandchild.SafeHandle);
            }
            finally
            {
                grandchild.Kill();
                childHandle.Process.Kill();
            }

            static async Task Verify(SafeProcessHandle handle)
            {
                if (OperatingSystem.IsWindows())
                {
                    Assert.False(handle.TryWaitForExit(TimeSpan.Zero, out _));

                    handle.Kill();
                    ProcessExitStatus processExitStatus = await handle.WaitForExitAsync();
                    Assert.Equal(-1, processExitStatus.ExitCode);
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => handle.WaitForExit());
                    Assert.Throws<PlatformNotSupportedException>(() => handle.TryWaitForExit(TimeSpan.FromSeconds(1), out _));
                    await Assert.ThrowsAsync<PlatformNotSupportedException>(async () => await handle.WaitForExitAsync());
                }
            }
        }
    }
}
