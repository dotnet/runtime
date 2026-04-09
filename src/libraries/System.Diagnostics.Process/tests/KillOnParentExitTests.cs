// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class KillOnParentExitTests : ProcessTestBase
    {
        [Fact]
        public void KillOnParentExit_DefaultsToFalse()
        {
            ProcessStartInfo startInfo = new();
            Assert.False(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_CanBeSetToTrue()
        {
            ProcessStartInfo startInfo = new();
            startInfo.KillOnParentExit = true;
            Assert.True(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_WithUseShellExecute_Throws()
        {
            ProcessStartInfo startInfo = new("hostname")
            {
                KillOnParentExit = true,
                UseShellExecute = true
            };

            Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void KillOnParentExit_ProcessStartsAndExitsNormally()
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "echo test" }, KillOnParentExit = true }
                : new("sh") { ArgumentList = { "-c", "echo test" }, KillOnParentExit = true };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);
            using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);
            Assert.True(fetchedProcess.WaitForExit(WaitInMS));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true)]
        [InlineData(false)]
        public void KillOnParentExit_KillsTheChild_WhenParentExits(bool enabled)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr) =>
                {
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);

                    return started.ProcessId; // return grand child pid as exit code
                },
                arg: enabled.ToString(),
                remoteInvokeOptions);

            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, remoteHandle.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
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
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);
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
        [PlatformSpecific(TestPlatforms.Windows)]
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
                    ProcessStartInfo processStartInfo = CreateLongRunningStartInfo();
                    processStartInfo.KillOnParentExit = bool.Parse(enabledStr);

                    using SafeProcessHandle started = SafeProcessHandle.Start(processStartInfo);
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

        private static ProcessStartInfo CreateLongRunningStartInfo()
        {
            // Create a process that sleeps for approximately 10 seconds
            return OperatingSystem.IsWindows()
                ? new("ping") { ArgumentList = { "127.0.0.1", "-n", "11" } }
                : new("sleep") { ArgumentList = { "10" } };
        }

        private static void VerifyProcessIsRunning(bool shouldBeKilled, int processId)
        {
            // Give the OS a moment to clean up
            Thread.Sleep(500);

            if (shouldBeKilled)
            {
                // When KillOnParentExit is enabled, the process should have been terminated.
                Assert.Throws<ArgumentException>(() => Process.GetProcessById(processId));
            }
            else
            {
                // When KillOnParentExit is disabled, the process should still be running.
                using Process process = Process.GetProcessById(processId);
                try
                {
                    Assert.False(process.HasExited);
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }
    }
}
