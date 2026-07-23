// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class WaitForExitStatusTests : ProcessTestBase
    {
        private const string Message = "Hello, World!";

        public enum TestTarget
        {
            StartedProcess,
            StartedHandle,
            FetchedProcess,
            FetchedHandle,
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

        public static TheoryData<TestTarget, bool> TestTarget_UseAsync_Data => new TheoryData<TestTarget, bool>
        {
            { TestTarget.StartedProcess, true },
            { TestTarget.StartedProcess, false },
            { TestTarget.StartedHandle, true },
            { TestTarget.StartedHandle, false },
            { TestTarget.FetchedProcess, true },
            { TestTarget.FetchedProcess, false },
            { TestTarget.FetchedHandle, true },
            { TestTarget.FetchedHandle, false },
        };

        public static TheoryData<TestTarget> TestTarget_Data => new TheoryData<TestTarget>
        {
            { TestTarget.StartedProcess },
            { TestTarget.StartedHandle },
            { TestTarget.FetchedProcess },
            { TestTarget.FetchedHandle },
        };

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestTarget_UseAsync_Data))]
        public async Task WaitForExit_ProcessExitsNormally_ReturnsExitCode(TestTarget testTarget, bool useAsync)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, false);

            using (readPipe)
            using (writePipe)
            using (Process process = CreateProcess(static () =>
            {
                // The process does not exit immediately, but waits for input from the pipe.
                // This allows us to obtain the process/handle before it exits.
                Assert.Equal(Message, Console.ReadLine());

                return RemoteExecutor.SuccessExitCode;
            }))
            {
                process.StartInfo.StandardInputHandle = readPipe;

                using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
                if (testTarget != TestTarget.StartedHandle)
                {
                    process.Start();
                }
                readPipe.Dispose();

                try
                {
                    ProcessExitStatus exitStatus = testTarget switch
                    {
                        TestTarget.StartedProcess => await TestProcess(process, writePipe, useAsync),
                        TestTarget.StartedHandle => await TestHandle(processHandle, writePipe, useAsync),
                        TestTarget.FetchedProcess => await TestProcess(Process.GetProcessById(process.Id), writePipe, useAsync),
                        TestTarget.FetchedHandle => await TestHandle(SafeProcessHandle.Open(process.Id), writePipe, useAsync),
                        _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                    };

                    Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
                    Assert.False(exitStatus.Canceled);
                    Assert.Null(exitStatus.Signal);
                }
                finally
                {
                    Kill(processHandle, process);
                }
            }

            static async Task<ProcessExitStatus> TestProcess(Process process, SafeFileHandle writePipe, bool useAsync)
            {
                await RandomAccess.WriteAsync(writePipe, Encoding.UTF8.GetBytes(Message + Environment.NewLine), 0);
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(WaitInMS));

                return useAsync
                    ? await process.WaitForExitStatusAsync(cts.Token)
                    : process.WaitForExitStatus();
            }

            static async Task<ProcessExitStatus> TestHandle(SafeProcessHandle processHandle, SafeFileHandle writePipe, bool useAsync)
            {
                await RandomAccess.WriteAsync(writePipe, Encoding.UTF8.GetBytes(Message + Environment.NewLine), 0);
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(WaitInMS));

                return useAsync
                    ? await processHandle.WaitForExitAsync(cts.Token)
                    : processHandle.WaitForExit();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestTarget_Data))]
        public void TryWaitForExit_ProcessExitsBeforeTimeout_ReturnsTrue(TestTarget testTarget)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, false);

            using (readPipe)
            using (writePipe)
            using (Process process = CreateProcess(static () =>
            {
                // The process does not exit immediately, but waits for input from the pipe.
                // This allows us to obtain the process/handle before it exits.
                Assert.Equal(Message, Console.ReadLine());

                return RemoteExecutor.SuccessExitCode;
            }))
            {
                process.StartInfo.StandardInputHandle = readPipe;

                using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
                if (testTarget != TestTarget.StartedHandle)
                {
                    process.Start();
                }
                readPipe.Dispose();

                try
                {
                    ProcessExitStatus? exitStatus = null;
                    bool exited = testTarget switch
                    {
                        TestTarget.StartedProcess => TestProcess(process, writePipe, out exitStatus),
                        TestTarget.StartedHandle => TestHandle(processHandle, writePipe, out exitStatus),
                        TestTarget.FetchedProcess => TestProcess(Process.GetProcessById(process.Id), writePipe, out exitStatus),
                        TestTarget.FetchedHandle => TestHandle(SafeProcessHandle.Open(process.Id), writePipe, out exitStatus),
                        _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                    };

                    Assert.True(exited);
                    Assert.NotNull(exitStatus);
                    Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
                    Assert.False(exitStatus.Canceled);
                    Assert.Null(exitStatus.Signal);
                }
                finally
                {
                    Kill(processHandle, process);
                }
            }

            static bool TestProcess(Process process, SafeFileHandle writePipe, out ProcessExitStatus? exitStatus)
            {
                RandomAccess.Write(writePipe, Encoding.UTF8.GetBytes(Message + Environment.NewLine), 0);
                return process.TryWaitForExitStatus(TimeSpan.FromMilliseconds(WaitInMS), out exitStatus);
            }

            static bool TestHandle(SafeProcessHandle processHandle, SafeFileHandle writePipe, out ProcessExitStatus? exitStatus)
            {
                RandomAccess.Write(writePipe, Encoding.UTF8.GetBytes(Message + Environment.NewLine), 0);
                return processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(WaitInMS), out exitStatus);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestTarget_Data))]
        public void TryWaitForExit_ProcessDoesNotExitBeforeTimeout_ReturnsFalse(TestTarget testTarget)
        {
            using Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
            if (testTarget != TestTarget.StartedHandle)
            {
                process.Start();
            }

            try
            {
                (bool exited, ProcessExitStatus? exitStatus, TimeSpan elapsed) = testTarget switch
                {
                    TestTarget.StartedProcess => TestProcess(process),
                    TestTarget.StartedHandle => TestHandle(processHandle!),
                    TestTarget.FetchedProcess => TestProcess(Process.GetProcessById(process.Id)),
                    TestTarget.FetchedHandle => TestHandle(SafeProcessHandle.Open(process.Id)),
                    _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                };

                Assert.False(exited);
                Assert.Null(exitStatus);
                Assert.InRange(elapsed, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(5000));
            }
            finally
            {
                Kill(processHandle, process);
            }

            static (bool exited, ProcessExitStatus? exitStatus, TimeSpan elapsed) TestHandle(SafeProcessHandle processHandle)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
                stopwatch.Stop();
                return (exited, exitStatus, stopwatch.Elapsed);
            }

            static (bool exited, ProcessExitStatus? exitStatus, TimeSpan elapsed) TestProcess(Process process)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = process.TryWaitForExitStatus(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
                stopwatch.Stop();
                return (exited, exitStatus, stopwatch.Elapsed);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(TestTarget.StartedHandle, true)]
        [InlineData(TestTarget.StartedHandle, false)]
        [InlineData(TestTarget.FetchedHandle, true)]
        [InlineData(TestTarget.FetchedHandle, false)]
        public async Task WaitForExitOrKill_ProcessExitsBeforeTimeout_DoesNotKill(TestTarget testTarget, bool useAsync)
        {
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, false);

            using (readPipe)
            using (writePipe)
            using (Process process = CreateProcess(static () =>
            {
                // The process does not exit immediately, but waits for input from the pipe.
                // This allows us to obtain the process/handle before it exits.
                Assert.Equal(Message, Console.ReadLine());

                return RemoteExecutor.SuccessExitCode;
            }))
            {
                process.StartInfo.StandardInputHandle = readPipe;

                SafeProcessHandle? processHandle = null;
                using SafeProcessHandle startedHandle = SafeProcessHandle.Start(process.StartInfo);
                readPipe.Dispose();

                try
                {
                    processHandle = testTarget switch
                    {
                        TestTarget.StartedHandle => startedHandle,
                        TestTarget.FetchedHandle => SafeProcessHandle.Open(startedHandle.ProcessId),
                        _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                    };

                    await RandomAccess.WriteAsync(writePipe, Encoding.UTF8.GetBytes(Message + Environment.NewLine), 0);
                    using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(WaitInMS));

                    ProcessExitStatus exitStatus = useAsync
                        ? await processHandle.WaitForExitOrKillOnCancellationAsync(cts.Token)
                        : processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));

                    Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
                    Assert.False(exitStatus.Canceled);
                    Assert.Null(exitStatus.Signal);
                }
                finally
                {
                    startedHandle.Kill();
                    processHandle?.Dispose();
                }
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(TestTarget.StartedHandle, true)]
        [InlineData(TestTarget.StartedHandle, false)]
        [InlineData(TestTarget.FetchedHandle, true)]
        [InlineData(TestTarget.FetchedHandle, false)]
        public async Task WaitForExitOrKill_ProcessDoesNotExit_KillsAndReturns(TestTarget testTarget, bool useAsync)
        {
            using Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            using SafeProcessHandle startedHandle = SafeProcessHandle.Start(process.StartInfo);
            SafeProcessHandle? processHandle = null;

            try
            {
                processHandle = testTarget switch
                {
                    TestTarget.StartedHandle => startedHandle,
                    TestTarget.FetchedHandle => SafeProcessHandle.Open(startedHandle.ProcessId),
                    _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                };

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
            finally
            {
                startedHandle.Kill();
                processHandle.Dispose();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WaitForExitAsync_WithoutCancellationToken_CompletesNormally(bool testHandle)
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            ProcessExitStatus exitStatus;

            if (testHandle)
            {
                using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
                exitStatus = await processHandle.WaitForExitAsync();
            }
            else
            {
                process.Start();
                exitStatus = await process.WaitForExitStatusAsync();
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
            Assert.Null(exitStatus.Signal);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestTarget_Data))]
        public async Task WaitForExitAsync_CancellationRequested_ThrowsOperationCanceledException(TestTarget testTarget)
        {
            using Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
            if (testTarget != TestTarget.StartedHandle)
            {
                process.Start();
            }

            try
            {
                OperationCanceledException ex = testTarget switch
                {
                    TestTarget.StartedProcess => await TestProcess(process),
                    TestTarget.StartedHandle => await TestHandle(processHandle),
                    TestTarget.FetchedProcess => await TestProcess(Process.GetProcessById(process.Id)),
                    TestTarget.FetchedHandle => await TestHandle(SafeProcessHandle.Open(process.Id)),
                    _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
                };
            }
            finally
            {
                Kill(processHandle, process);
            }

            static async Task<OperationCanceledException> TestHandle(SafeProcessHandle processHandle)
            {
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));
                return await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await processHandle.WaitForExitAsync(cts.Token));
            }

            static async Task<OperationCanceledException> TestProcess(Process process)
            {
                using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(300));
                return await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await process.WaitForExitStatusAsync(cts.Token));
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestTarget_UseAsync_Data))]
        public async Task WaitForExit_CalledAfterKill_ReturnsImmediately(TestTarget testTarget, bool useAsync)
        {
            using Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            int pid = 0;
            using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
            if (testTarget != TestTarget.StartedHandle)
            {
                process.Start();
                pid = process.Id;
            }
            else
            {
                pid = processHandle.ProcessId;
            }

            using Process fetchedProcess = Process.GetProcessById(pid);
            using SafeProcessHandle fetchedHandle = SafeProcessHandle.Open(pid);
            fetchedHandle.Kill();

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = (testTarget, useAsync) switch
            {
                (TestTarget.StartedHandle, true) => await processHandle.WaitForExitAsync(),
                (TestTarget.StartedHandle, false) => processHandle.WaitForExit(),
                (TestTarget.StartedProcess, true) => await process.WaitForExitStatusAsync(),
                (TestTarget.StartedProcess, false) => process.WaitForExitStatus(),
                (TestTarget.FetchedHandle, true) => await fetchedHandle.WaitForExitAsync(),
                (TestTarget.FetchedHandle, false) => fetchedHandle.WaitForExit(),
                (TestTarget.FetchedProcess, true) => await fetchedProcess.WaitForExitStatusAsync(),
                (TestTarget.FetchedProcess, false) => fetchedProcess.WaitForExitStatus(),
                _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
            };
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Assert.NotEqual(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.Windows, "Signal property is Unix-specific")]
        [MemberData(nameof(TestTarget_UseAsync_Data))]
        public async Task WaitForExit_ProcessKilledBySignal_ReportsSignal(TestTarget testTarget, bool useAsync)
        {
            const PosixSignal signal = PosixSignal.SIGTERM;
            using Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            int pid = 0;
            using SafeProcessHandle? processHandle = testTarget == TestTarget.StartedHandle ? SafeProcessHandle.Start(process.StartInfo) : null;
            if (testTarget != TestTarget.StartedHandle)
            {
                process.Start();
                pid = process.Id;
            }
            else
            {
                pid = processHandle.ProcessId;
            }

            using Process fetchedProcess = Process.GetProcessById(pid);
            using SafeProcessHandle fetchedHandle = SafeProcessHandle.Open(pid);
            bool delivered = testTarget switch
            {
                TestTarget.StartedHandle => processHandle.Signal(signal),
                TestTarget.StartedProcess => process.Signal(signal),
                TestTarget.FetchedHandle => fetchedHandle.Signal(signal),
                TestTarget.FetchedProcess => fetchedProcess.Signal(signal),
                _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
            };
            Assert.True(delivered);

            Stopwatch stopwatch = Stopwatch.StartNew();
            ProcessExitStatus exitStatus = (testTarget, useAsync) switch
            {
                (TestTarget.StartedHandle, true) => await processHandle.WaitForExitAsync(),
                (TestTarget.StartedHandle, false) => processHandle.WaitForExit(),
                (TestTarget.StartedProcess, true) => await process.WaitForExitStatusAsync(),
                (TestTarget.StartedProcess, false) => process.WaitForExitStatus(),
                (TestTarget.FetchedHandle, true) => await fetchedHandle.WaitForExitAsync(),
                (TestTarget.FetchedHandle, false) => fetchedHandle.WaitForExit(),
                (TestTarget.FetchedProcess, true) => await fetchedProcess.WaitForExitStatusAsync(),
                (TestTarget.FetchedProcess, false) => fetchedProcess.WaitForExitStatus(),
                _ => throw new NotSupportedException($"Test target {testTarget} is not supported.")
            };
            stopwatch.Stop();

            Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            Assert.NotEqual(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
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

        private static void Kill(SafeProcessHandle? processHandle, Process process)
        {
            if (processHandle is not null)
            {
                processHandle.Kill();
            }
            else
            {
                process.Kill();
            }
        }
    }
}
