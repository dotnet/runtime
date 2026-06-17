// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [ConditionalClass(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public class StartSuspendedTests : ProcessTestBase
    {
        [ConditionalFact]
        public void StartSuspended_ResumeCompletes()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            // The process should not have exited yet because it is suspended.
            bool hasExited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(200), out _);
            Assert.False(hasExited, "Suspended process should not have exited yet.");

            processHandle.Resume();

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
        }

        [ConditionalFact]
        public void StartSuspended_ProcessIdIsValid()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            try
            {
                Assert.NotEqual(0, processHandle.ProcessId);
            }
            finally
            {
                processHandle.Resume();
                processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
            }
        }

        [ConditionalFact]
        public void Resume_CalledTwice_ThrowsInvalidOperationException()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            processHandle.Resume();

            Assert.Throws<InvalidOperationException>(() => processHandle.Resume());

            processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
        }

        [ConditionalFact]
        public void Resume_OnNonSuspendedProcess_ThrowsInvalidOperationException()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            Assert.Throws<InvalidOperationException>(() => processHandle.Resume());

            processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
        }

        [ConditionalFact]
        public void StartSuspended_KillWithoutResume_Succeeds()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            // Kill the suspended process without resuming it first.
            processHandle.Kill();

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact]
        public void StartSuspended_DisposeWithoutResume_DoesNotThrow()
        {
            Process process = CreateProcess(static () =>
            {
                Thread.Sleep(Timeout.Infinite);
                return RemoteExecutor.SuccessExitCode;
            });
            process.StartInfo.StartSuspended = true;

            SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
            int processId = processHandle.ProcessId;

            // Dispose without resuming should not throw.
            processHandle.Dispose();

            // Clean up the orphaned process.
            Assert.True(Process.TryGetProcessById(processId, out Process? orphaned));

            using (orphaned)
            {
                orphaned.Kill();
                orphaned.WaitForExit(WaitInMS);
            }
        }

        [ConditionalFact]
        public void StartSuspended_WithUseShellExecute_ThrowsInvalidOperationException()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                StartSuspended = true,
                UseShellExecute = true,
            };

            Assert.Throws<InvalidOperationException>(() => SafeProcessHandle.Start(startInfo));
        }

        [ConditionalFact]
        public void StartSuspended_PropertyDefaultsToFalse()
        {
            ProcessStartInfo startInfo = new();
            Assert.False(startInfo.StartSuspended);
        }

        [ConditionalFact]
        public void StartSuspended_CanSetAndGet()
        {
            ProcessStartInfo startInfo = new()
            {
                StartSuspended = true,
            };

            Assert.True(startInfo.StartSuspended);
        }

        [ConditionalFact]
        public void StartSuspended_WithPipeRedirection_Works()
        {
            Process process = CreateProcess(static () =>
            {
                Console.WriteLine("hello");
                return RemoteExecutor.SuccessExitCode;
            });

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadPipe, out SafeFileHandle outputWritePipe);

            using (outputReadPipe)
            using (outputWritePipe)
            {
                process.StartInfo.StandardOutputHandle = outputWritePipe;
                process.StartInfo.StartSuspended = true;

                using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
                outputWritePipe.Close();

                // Verify nothing has been written yet while the process is suspended.
                using FileStream readStream = new(outputReadPipe, FileAccess.Read, bufferSize: 1, outputReadPipe.IsAsync);
                byte[] buffer = new byte[1];
                Task<int> readTask = readStream.ReadAsync(buffer, 0, 1);
                Assert.False(readTask.Wait(50), "Suspended process should not have written any output yet.");

                processHandle.Resume();

                using StreamReader streamReader = new(readStream);
                // The first byte was already read into the buffer by the async read.
                string firstChar = Encoding.UTF8.GetString(buffer, 0, readTask.GetAwaiter().GetResult());
                string? rest = streamReader.ReadLine();
                Assert.Equal("hello", firstChar + rest);

                ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMilliseconds(WaitInMS));
                Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            }
        }
    }

    public class StartSuspendedTests_NonWindows : ProcessTestBase
    {
        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Resume throws PlatformNotSupportedException on non-Windows")]
        public void Resume_OnNonWindows_ThrowsPlatformNotSupportedException()
        {
            using SafeProcessHandle handle = new();
            Assert.Throws<PlatformNotSupportedException>(() => handle.Resume());
        }
    }
}
