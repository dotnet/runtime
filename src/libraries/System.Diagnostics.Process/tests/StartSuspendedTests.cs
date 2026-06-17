// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StartSuspendedTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartSuspended_ResumeCompletes()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            // The process should not have exited yet because it is suspended.
            bool hasExited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(200), out _);
            Assert.False(hasExited, "Suspended process should not have exited yet.");

            processHandle.Resume();

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
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
                processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Resume_CalledTwice_ThrowsInvalidOperationException()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.StartSuspended = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            processHandle.Resume();

            Assert.Throws<InvalidOperationException>(() => processHandle.Resume());

            processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Resume_OnNonSuspendedProcess_ThrowsInvalidOperationException()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);

            Assert.Throws<InvalidOperationException>(() => processHandle.Resume());

            processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

            ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
            Assert.NotEqual(0, exitStatus.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
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
            try
            {
                using Process orphaned = Process.GetProcessById(processId);
                orphaned.Kill();
                orphaned.WaitForExit(WaitInMS);
            }
            catch (ArgumentException) { } // Process may have already exited.
            catch (InvalidOperationException) { }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartSuspended_WithUseShellExecute_ThrowsInvalidOperationException()
        {
            ProcessStartInfo startInfo = new("cmd")
            {
                StartSuspended = true,
                UseShellExecute = true,
            };

            Assert.Throws<InvalidOperationException>(() => SafeProcessHandle.Start(startInfo));
        }

        [Fact]
        public void StartSuspended_PropertyDefaultsToFalse()
        {
            ProcessStartInfo startInfo = new();
            Assert.False(startInfo.StartSuspended);
        }

        [Fact]
        public void StartSuspended_CanSetAndGet()
        {
            ProcessStartInfo startInfo = new()
            {
                StartSuspended = true,
            };

            Assert.True(startInfo.StartSuspended);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "Resume throws PlatformNotSupportedException on non-Windows")]
        public void Resume_OnNonWindows_ThrowsPlatformNotSupportedException()
        {
            using SafeProcessHandle handle = new();
            Assert.Throws<PlatformNotSupportedException>(() => handle.Resume());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
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

                // Process is suspended, should not have written anything yet.
                processHandle.Resume();

                using System.IO.StreamReader streamReader = new(new System.IO.FileStream(outputReadPipe, System.IO.FileAccess.Read, bufferSize: 1, outputReadPipe.IsAsync));
                Assert.Equal("hello", streamReader.ReadLine());

                ProcessExitStatus exitStatus = processHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromSeconds(30));
                Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            }
        }
    }
}
