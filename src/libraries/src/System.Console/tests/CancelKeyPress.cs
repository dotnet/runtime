// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

public class CancelKeyPressTests : RemoteExecutorTestBase
{
    private const int WaitFailTestTimeoutSeconds = 30;

    [Fact]
    public static void CanAddAndRemoveHandler()
    {
        ConsoleCancelEventHandler handler = (sender, e) =>
        {
            // We don't actually want to do anything here.  This will only get called on the off chance
            // that someone CTRL+C's the test run while the handler is hooked up.  This is just used to 
            // validate that we can add and remove a handler, we don't care about exercising it.
        };
        Console.CancelKeyPress += handler;
        Console.CancelKeyPress -= handler;
    }

    [Fact]
    public void CanAddAndRemoveHandler_Remote()
    {
        // xunit registers a CancelKeyPress handler at the beginning of the test run and never 
        // unregisters it, thus we can't execute all of the removal code in the same process.
        RemoteInvoke(() =>
        {
            CanAddAndRemoveHandler();
            CanAddAndRemoveHandler(); // add and remove again
            return SuccessExitCode;
        }).Dispose();
    }

    [PlatformSpecific(PlatformID.AnyUnix)]
    public void HandlerInvokedForSigInt()
    {
        HandlerInvokedForSignal(SIGINT);
    }

    [PlatformSpecific(PlatformID.AnyUnix & ~PlatformID.OSX)] // Jenkins blocks SIGQUIT on OS X, causing the test to fail in CI
    public void HandlerInvokedForSigQuit()
    {
        HandlerInvokedForSignal(SIGQUIT);
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
        RemoteInvoke(signalStr =>
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

            return SuccessExitCode;
        }, signalOuter.ToString()).Dispose();
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    private const int SIGINT = 2;
    private const int SIGQUIT = 3;
}
