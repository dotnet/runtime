// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public partial class CancelKeyPressTests
{
    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerInvokedForSigInt(bool redirectStandardInput)
    {
        // .NET Core respects ignored disposition for SIGINT/SIGQUIT.
        if (IsSignalIgnored(SIGINT))
        {
            return;
        }

        HandlerInvokedForSignal(SIGINT, redirectStandardInput);
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/38998", TestPlatforms.OSX)]
    [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    [SkipOnMono("Mono doesn't call CancelKeyPress for SIGQUIT.")]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerInvokedForSigQuit(bool redirectStandardInput)
    {
        // .NET Core respects ignored disposition for SIGINT/SIGQUIT.
        if (IsSignalIgnored(SIGQUIT))
        {
            return;
        }

        HandlerInvokedForSignal(SIGQUIT, redirectStandardInput);
    }

    [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
    public void ExitDetectionNotBlockedByHandler()
    {
        // .NET Core respects ignored disposition for SIGINT/SIGQUIT.
        if (IsSignalIgnored(SIGINT))
        {
            return;
        }

        RemoteExecutor.Invoke(() =>
        {
            var mre = new ManualResetEventSlim();
            var tcs = new TaskCompletionSource();

            // CancelKeyPress is triggered by SIGINT/SIGQUIT
            Console.CancelKeyPress += (sender, e) =>
            {
                tcs.SetResult();
                // Block CancelKeyPress
                Assert.True(mre.Wait(WaitFailTestTimeoutSeconds * 1000));
            };

            // Generate CancelKeyPress
            Assert.Equal(0, kill(Environment.ProcessId, SIGINT));
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
        }).Dispose();
    }

    private void HandlerInvokedForSignal(int signalOuter, bool redirectStandardInput)
    {
        // On Windows we could use GenerateConsoleCtrlEvent to send a ctrl-C to the process,
        // however that'll apply to all processes associated with the same group, which will
        // include processes like the code coverage tool when doing code coverage runs, causing
        // those other processes to exit.  As such, we test this only on Unix, where we can
        // send a SIGINT signal to this specific process only.

        // This test sends a SIGINT back to itself... if run in the xunit process, this would end
        // up canceling the rest of xunit's tests.  So we run the test itself in a separate process.
        RemoteInvokeOptions options = new RemoteInvokeOptions();
        options.StartInfo.RedirectStandardInput = redirectStandardInput;
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
                Assert.Equal(0, kill(Environment.ProcessId, signalInner));
                Assert.True(tcs.Task.Wait(WaitFailTestTimeoutSeconds * 1000));
                Assert.Equal(
                    signalInner == SIGINT ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak,
                    tcs.Task.Result);
            }
            finally
            {
                Console.CancelKeyPress -= handler;
            }
        }, signalOuter.ToString(), options).Dispose();
    }

    private unsafe static bool IsSignalIgnored(int signal)
    {
        struct_sigaction current;
        if (sigaction(signal, null, &current) == 0)
        {
            return current.handler == SIG_IGN;
        }
        else
        {
            throw new Win32Exception();
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    [DllImport("libc", SetLastError = true)]
    private static unsafe extern int sigaction(int signum, struct_sigaction* act, struct_sigaction* oldact);

    private const int SIGINT = 2;
    private const int SIGQUIT = 3;
    private unsafe static void* SIG_IGN => (void*)1;

    private unsafe struct struct_sigaction
    {
        public void* handler;
        private fixed long __pad[128]; // ensure this struct is larger than native 'struct sigaction'
    }
}
