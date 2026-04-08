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
            ProcessStartInfo psi = new ProcessStartInfo("dummy")
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.Windows, "SIGHUP test is Unix-specific")]
        public void StartDetached_GrandchildSurvivesSIGHUP()
        {
            // Verify that the grandchild started with StartDetached=true survives SIGHUP sent to the child.
            // A detached process starts a new session (via setsid), so it won't receive SIGHUP when
            // the parent's session leader exits.
            int grandchildPid = -1;

            using (RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(static () =>
            {
                using RemoteInvokeHandle grandchildHandle = RemoteExecutor.Invoke(
                    static () => { Thread.Sleep(Timeout.Infinite); return RemoteExecutor.SuccessExitCode; },
                    new RemoteInvokeOptions { Start = false, CheckExitCode = false });

                grandchildHandle.Process.StartInfo.StartDetached = true;
                grandchildHandle.Process.Start();

                Console.WriteLine(grandchildHandle.Process.Id);
                Console.Out.Flush();

                // Transfer ownership.
                grandchildHandle.Process = null;

                // Sleep so the test can send SIGHUP.
                Thread.Sleep(Timeout.Infinite);

                return RemoteExecutor.SuccessExitCode;
            }, new RemoteInvokeOptions
            {
                StartInfo = new ProcessStartInfo { RedirectStandardOutput = true },
                CheckExitCode = false
            }))
            {
                string pidLine = childHandle.Process.StandardOutput.ReadLine();
                Assert.True(int.TryParse(pidLine, out grandchildPid), $"Could not parse grandchild PID from: '{pidLine}'");

                // Send SIGHUP to the child process - this simulates terminal disconnect.
                childHandle.Process.SafeHandle.Signal(PosixSignal.SIGHUP);

                // Wait for the child to exit (SIGHUP terminates the process by default).
                childHandle.Process.WaitForExit(WaitInMS);
            }

            try
            {
                // Brief pause to allow any signal propagation.
                Thread.Sleep(200);

                // Verify the grandchild is still running after the child received SIGHUP.
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

                Assert.True(grandchildAlive, "Grandchild should survive SIGHUP to parent when StartDetached=true.");
            }
            finally
            {
                KillGrandchild(grandchildPid);
            }
        }

        private static void KillGrandchild(int pid)
        {
            if (pid <= 0)
                return;

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
