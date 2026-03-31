// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
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

                // We can get the process by id only when it's still running,
                // so we wait with "pong" until we obtain the process instance.
                // When we introduce SafeProcessHandle.WaitForExit* APIs, it's not needed.
                using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

                using StreamWriter streamWriter = new(new FileStream(inputWritePipe, FileAccess.Write, bufferSize: 1, inputWritePipe.IsAsync))
                {
                    AutoFlush = true
                };

                try
                {
                    streamWriter.WriteLine("pong");
                }
                finally
                {
                    fetchedProcess.Kill();
                    fetchedProcess.WaitForExit();
                }
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Kill_RunningProcess_Terminates()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

            processHandle.Kill();

            Assert.True(fetchedProcess.WaitForExit(WaitInMS));
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
    }
}
