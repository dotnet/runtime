// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StartDetachedTests : ProcessTestBase
    {
        [Fact]
        public void StartDetached_ThrowsOnUseShellExecute()
        {
            ProcessStartInfo psi = new("dummy")
            {
                UseShellExecute = true,
                StartDetached = true
            };

            Assert.Throws<InvalidOperationException>(() => SafeProcessHandle.Start(psi));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartDetached_GrandchildOutlivesChild()
        {
            // The child process creates a grandchild with StartDetached=true, reports its PID via stdout,
            // then exits. We verify the grandchild is still alive after the child exits.
            int grandchildPid = -1;

            using (RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(static () =>
            {
                // Create a grandchild process that sleeps indefinitely.
                using RemoteInvokeHandle grandchildHandle = RemoteExecutor.Invoke(
                    static () => { Thread.Sleep(Timeout.Infinite); return RemoteExecutor.SuccessExitCode; },
                    new RemoteInvokeOptions { Start = false, CheckExitCode = false });

                grandchildHandle.Process.StartInfo.StartDetached = true;
                grandchildHandle.Process.Start();

                // Report the grandchild PID so the test process can verify it's still alive.
                Console.WriteLine(grandchildHandle.Process.Id);

                // Transfer ownership - prevent RemoteInvokeHandle from killing the grandchild on dispose.
                grandchildHandle.Process = null;

                return RemoteExecutor.SuccessExitCode;
            }, new RemoteInvokeOptions { StartInfo = new ProcessStartInfo { RedirectStandardOutput = true } }))
            {
                string pidLine = childHandle.Process.StandardOutput.ReadLine();
                Assert.True(int.TryParse(pidLine, out grandchildPid), $"Could not parse grandchild PID from: '{pidLine}'");

                // Wait for the child to exit.
                Assert.True(childHandle.Process.WaitForExit(WaitInMS));
            }

            try
            {
                // Verify the grandchild is still running after the child has exited.
                using Process grandchild = Process.GetProcessById(grandchildPid);
                Assert.False(grandchild.HasExited, "Grandchild process should still be running after parent exited.");
            }
            finally
            {
                KillGrandchild(grandchildPid);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.Windows, "SIGHUP test is Unix-specific")]
        [InlineData(true)]
        [InlineData(false)]
        public void StartDetached_GrandchildSurvivesSIGHUP(bool enable)
        {
            // Verify that a grandchild started with StartDetached=true survives SIGHUP sent to the child's
            // process group, whereas a non-detached grandchild (which inherits the child's process group) is killed.
            //
            // The outer child is started with StartDetached=true so it becomes the leader of its own
            // process group. When we send SIGHUP to -childPid (the process group), the non-detached
            // grandchild (enable=false) is in the same group and is killed; the detached grandchild
            // (enable=true) is in its own group and survives.
            int grandchildPid = -1;

            using (RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(static (arg) =>
            {
                using RemoteInvokeHandle grandchildHandle = RemoteExecutor.Invoke(
                    static () => { Thread.Sleep(Timeout.Infinite); return RemoteExecutor.SuccessExitCode; },
                    new RemoteInvokeOptions { Start = false, CheckExitCode = false });

                grandchildHandle.Process.StartInfo.StartDetached = bool.Parse(arg);
                grandchildHandle.Process.Start();

                Console.WriteLine(grandchildHandle.Process.Id);
                Console.Out.Flush();

                // Transfer ownership.
                grandchildHandle.Process = null;

                // Keep the child alive so the test can kill the process group.
                Thread.Sleep(Timeout.Infinite);

                return RemoteExecutor.SuccessExitCode;
            },
            enable.ToString(),
            new RemoteInvokeOptions
            {
                // Start the child in its own session (process group leader) so killing -childPid
                // targets only the child's process group, not the test runner's process group.
                StartInfo = new ProcessStartInfo { RedirectStandardOutput = true, StartDetached = true },
                CheckExitCode = false
            }))
            {
                string pidLine = childHandle.Process.StandardOutput.ReadLine();
                Assert.True(int.TryParse(pidLine, out grandchildPid), $"Could not parse grandchild PID from: '{pidLine}'");

                // Kill the child's entire process group with SIGHUP.
                // Passing a negative PID sends the signal to all processes in the process group.
                int sighup = Interop.Sys.GetPlatformSignalNumber(PosixSignal.SIGHUP);
                Interop.kill(-childHandle.Process.Id, sighup);

                // Wait for the child to exit (SIGHUP terminates the process by default).
                Assert.True(childHandle.Process.WaitForExit(WaitInMS));
            }

            try
            {
                // Brief pause to allow signal propagation.
                Thread.Sleep(200);

                // Verify the grandchild is still running after the child's process group received SIGHUP.
                bool grandchildAlive;
                try
                {
                    using Process grandchild = Process.GetProcessById(grandchildPid);
                    grandchildAlive = !grandchild.HasExited;
                }
                catch (ArgumentException)
                {
                    grandchildAlive = false;
                }

                // Detached grandchild (StartDetached=true) creates its own session/process group and survives.
                // Non-detached grandchild inherits the child's process group and is killed by SIGHUP.
                Assert.Equal(enable, grandchildAlive);
            }
            finally
            {
                KillGrandchild(grandchildPid);
            }
        }

        private static void KillGrandchild(int pid)
        {
            try
            {
                using Process grandchild = Process.GetProcessById(pid);
                grandchild.Kill();
                grandchild.WaitForExit(WaitInMS);
            }
            catch (ArgumentException) { } // process may have already exited
        }
    }
}
