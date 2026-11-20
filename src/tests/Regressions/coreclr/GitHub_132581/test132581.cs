// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class HandleMacosSAFlagsOddityAcrossExec
{
    private const string ChildEnvironmentVariable = "DOTNET_TEST_132581_CHILD";

    [DllImport("nativetest132581")]
    private static extern int InstallSignalHandlerAndExec(string executable, string managedAssembly);

    [DllImport("nativetest132581")]
    private static extern int SendSignalFromChildProcess();

    /// <summary>
    /// MacOS leaks the exec-ing process's sa_flags across execve, but clears the sa_handler and sa_sigaction function
    /// pointers, which creates a mismatched sa_flags in the process that is exec-ed into that says to use sa_sigaction
    /// to handle the signal (instead of sa_handler). If the runtime doesn't also check that sa_sigaction is not SIG_IGN
    /// or SIG_DFL, a crash occurs. This test ensures that the cleared signal handler, sa_sigaction, is not called when
    /// it is set to SIG_IGN (ignore signal) or SIG_DFL (use default handler) even if the sa_flags says to use it.
    /// Both a signal from another process and the runtime's own activation signal (SIGUSR1 on macOS, used to suspend
    /// threads for GC) are exercised, since the previous handler is consulted for both.
    /// </summary>
    [Fact]
    public static void DoNotCallPreviousSignalHandlerIfHandlerIsNotSet()
    {
        if (Environment.GetEnvironmentVariable(ChildEnvironmentVariable) is null)
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("The process path is unavailable.");
            string command = Environment.GetCommandLineArgs()[0];
            string managedAssembly = string.Equals(executable, command, StringComparison.Ordinal) ? string.Empty : command;

            int error = InstallSignalHandlerAndExec(executable, managedAssembly);
            throw new InvalidOperationException($"execv failed with error {error}.");
        }

        Assert.Equal(0, SendSignalFromChildProcess());
        SuspendSpinningThreadForGC();
    }

    private static volatile bool s_spinning;
    private static volatile bool s_stopSpinning;

    private static void SuspendSpinningThreadForGC()
    {
        var spinner = new Thread(Spin);
        spinner.Start();
        while (!s_spinning)
        {
            Thread.Sleep(1);
        }

        for (int i = 0; i < 100; i++)
        {
            GC.Collect();
            // Let the spinner resume its loop so that the next GC has to interrupt it again.
            Thread.Sleep(1);
        }

        s_stopSpinning = true;
        spinner.Join();
    }

    // Compiled with full optimizations up front so the loop has no tier-0 patchpoints or other safe points:
    // a thread spinning here can only be suspended for GC by injecting an activation signal.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void Spin()
    {
        s_spinning = true;
        while (!s_stopSpinning)
        {
        }
    }
}
