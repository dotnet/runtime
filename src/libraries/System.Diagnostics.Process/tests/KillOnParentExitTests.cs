// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
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
            ProcessStartInfo startInfo = new()
            {
                KillOnParentExit = true
            };

            Assert.True(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_WithUseShellExecute_Throws()
        {
            ProcessStartInfo startInfo = new("dummy")
            {
                KillOnParentExit = true,
                UseShellExecute = true
            };

            Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void KillOnParentExit_ProcessStartsAndExitsNormally()
        {
            Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.KillOnParentExit = true;
            process.Start();

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        public void KillOnParentExit_KillsTheChild_WhenParentExits(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInheritanceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInheritanceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            string firstLine = remoteHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            remoteHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        public void KillOnParentExit_KillsTheChild_WhenParentIsKilled(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInherianceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInherianceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);

                    // This will block the child until parent kills it.
                    _ = Console.ReadLine();
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            string firstLine = childHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            childHandle.Process.Kill();
            childHandle.Process.WaitForExit();

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        public void KillOnParentExit_KillsTheChild_WhenParentCrashes(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardError = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;
            // Don't create (and upload) a memory dump for this test, as AV is intentional and expected.
            remoteInvokeOptions.StartInfo.Environment["HELIX_WORKITEM_UPLOAD_ROOT"] = null;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInherianceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInherianceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);

                    // This will block the child until parent writes input.
                    _ = Console.ReadLine();

                    // Guaranteed Access Violation - write to null pointer
                    Marshal.WriteInt32(IntPtr.Zero, 42);
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            string firstLine = childHandle.Process.StandardOutput.ReadLine();
            int grandChildPid = int.Parse(firstLine);
            childHandle.Process.StandardInput.WriteLine("One AccessViolationException please.");
            childHandle.Process.WaitForExit();
            Assert.NotEqual(0, childHandle.Process.ExitCode);

            VerifyProcessIsRunning(enabled, grandChildPid);
        }

        private static void VerifyProcessIsRunning(bool shouldBeKilled, int processId)
        {
            if (shouldBeKilled)
            {
                const int timeoutMilliseconds = 10_000;
                long deadline = Environment.TickCount64 + timeoutMilliseconds;

                while (Environment.TickCount64 < deadline)
                {
                    try
                    {
                        using Process process = Process.GetProcessById(processId);
                        if (process.HasExited || process.WaitForExit(100))
                        {
                            return;
                        }
                    }
                    catch (ArgumentException)
                    {
                        return;
                    }

                    Thread.Sleep(100);
                }

                using Process finalCheck = Process.GetProcessById(processId);
                Assert.True(finalCheck.HasExited || finalCheck.WaitForExit(0),
                    $"Process {processId} was expected to exit within {timeoutMilliseconds}ms.");
            }
            else
            {
                // Give the OS a moment to clean up
                Thread.Sleep(500);

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
