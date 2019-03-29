// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class CancelKeyPressTests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes
    public void HandlerInvokedForSigInt()
    {
        HandlerInvokedForSignal(SIGINT);
    }

    [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.OSX)] // Jenkins blocks SIGQUIT on OS X, causing the test to fail in CI
    public void HandlerInvokedForSigQuit()
    {
        HandlerInvokedForSignal(SIGQUIT);
    }

    [PlatformSpecific(TestPlatforms.AnyUnix)]  // events are triggered by Unix signals (SIGINT, SIGQUIT, SIGCHLD).
    public void ExitDetectionNotBlockedByHandler()
    {
        RemoteExecutor.Invoke(() =>
        {
            var mre = new ManualResetEventSlim();
            var tcs = new TaskCompletionSource<object>();

            // CancelKeyPress is triggered by SIGINT/SIGQUIT
            Console.CancelKeyPress += (sender, e) =>
            {
                tcs.SetResult(null);
                // Block CancelKeyPress
                Assert.True(mre.Wait(WaitFailTestTimeoutSeconds * 1000));
            };

            // Generate CancelKeyPress
            Assert.Equal(0, kill(Process.GetCurrentProcess().Id, SIGINT));
            // Wait till we block CancelKeyPress
            Assert.True(tcs.Task.Wait(WaitFailTestTimeoutSeconds * 1000));

            // Create a process and wait for it to exit.
            using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(() => RemoteExecutor.SuccessExitCode))
            {
                // Process exit is detected on SIGCHLD
                Assert.Equal(RemoteExecutor.SuccessExitCode, handle.ExitCode);
            }

            // Release CancelKeyPress
            mre.Set();

            return RemoteExecutor.SuccessExitCode;
        }).Dispose();
    }

    private void HandlerInvokedForSignal(int signalOuter)
    {
        // On Windows we could use GenerateConsoleCtrlEvent to send a ctrl-C to the process,
        // however that'll apply to all processes associated with the same group, which will
        // include processes like the code coverage tool when doing code coverage runs, causing
        // those other processes to exit.  As such, we test this only on Unix, where we can
        // send a SIGINT signal to this specific process only.

        // This test sends a SIGINT back to itself... if run in the xunit process, this would end
        // up canceling the rest of xunit's tests.  So we run the test itself in a separate process.
        RemoteExecutor.Invoke(signalStr =>
        {
            var tcs = new TaskCompletionSource<ConsoleSpecialKey>();

            ConsoleCancelEventHandler handler = (sender, e) =>
            {
                e.Cancel = true;
                tcs.SetResult(e.SpecialKey);
            };

            Console.CancelKeyPress += handler;
            try
            {
                int signalInner = int.Parse(signalStr);
                Assert.Equal(0, kill(Process.GetCurrentProcess().Id, signalInner));
                Assert.True(tcs.Task.Wait(WaitFailTestTimeoutSeconds * 1000));
                Assert.Equal(
                    signalInner == SIGINT ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak,
                    tcs.Task.Result);
            }
            finally
            {
                Console.CancelKeyPress -= handler;
            }

            return RemoteExecutor.SuccessExitCode;
        }, signalOuter.ToString()).Dispose();
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    private const int SIGINT = 2;
    private const int SIGQUIT = 3;
}
