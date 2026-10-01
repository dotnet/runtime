// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;

// A native library runs managed code on an OS thread, then runs managed code again from a
// thread-destruction callback that fires after the runtime has already torn down its per-thread
// state. The runtime should fail fast instead of attaching another managed thread.

public static unsafe class ThreadStateDestroyed
{
    private const int Pass = 100;
    private const int Fail = -1;

    private const string NativeLib = "ThreadStateDestroyedNative";
    private const string RunScenarioArg = "--run-scenario";

    private static readonly TimeSpan s_subprocessTimeout = TimeSpan.FromSeconds(60);

    // In non-Release builds, also check the message to distinguish this failure from other crashes.
    private const string ExpectedMessage =
        "Attempt to execute managed code after the .NET runtime thread state has been destroyed.";
    private const string SecondCallbackMarker = "[managed] callback #2";

    [DllImport(NativeLib)]
    private static extern void RunCallbackOnThreadAndDuringItsDestruction(delegate* unmanaged<void> callback);

    private static int s_callbackCount;
    private static Thread s_firstThread;

    [UnmanagedCallersOnly]
    private static void Callback()
    {
        int count = Interlocked.Increment(ref s_callbackCount);
        Thread current = Thread.CurrentThread;

        if (count == 1)
        {
            s_firstThread = current;
            Console.WriteLine("[managed] callback #1 ran; the runtime attached a Thread to the OS thread.");
        }
        else
        {
            // Managed thread ids can be recycled, so compare the Thread objects themselves.
            bool newThread = !ReferenceEquals(current, s_firstThread);
            Console.WriteLine($"{SecondCallbackMarker} ran; attached to a new Thread: {newThread}.");
        }
    }

    public static int Main(string[] args)
    {
        return args.Length > 0 && args[0] == RunScenarioArg
            ? RunScenario()
            : RunController();
    }

    private static int RunScenario()
    {
        // Ensure that the OS doesn't generate core dump for this intentionally crashing process
        Utilities.DisableOSCoreDump();

        RunCallbackOnThreadAndDuringItsDestruction(&Callback);

        Console.WriteLine("[managed] The runtime did not fail fast.");
        return Fail;
    }

    private static int RunController()
    {
        string[] arguments = TestLibrary.Utilities.IsNativeAot
            ? [RunScenarioArg]
            : [typeof(ThreadStateDestroyed).Assembly.Location, RunScenarioArg];

        ProcessStartInfo psi = new ProcessStartInfo(Environment.ProcessPath, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        psi.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        psi.Environment["DOTNET_EnableCrashReport"] = "0";

        ProcessTextOutput subprocess;
        try
        {
            subprocess = Process.RunAndCaptureText(psi, s_subprocessTimeout);
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Subprocess timed out after {s_subprocessTimeout}.");
            return Fail;
        }

        string output = subprocess.StandardOutput + subprocess.StandardError;
        int exitCode = subprocess.ExitStatus.ExitCode;

        Console.WriteLine($"Subprocess exited with {exitCode}:");
        Console.WriteLine(output);

        if (output.Contains(SecondCallbackMarker))
        {
            Console.WriteLine("The runtime ran managed code on a thread whose runtime thread state was already destroyed.");
            return Fail;
        }

        if (output.Contains("[managed] The runtime did not fail fast.") ||
            !output.Contains("[native] invoking managed callback from the thread destruction callback"))
        {
            Console.WriteLine("The test scenario did not reach the expected thread-destruction failure.");
            return Fail;
        }

        if ((!OperatingSystem.IsWindows() || !TestLibrary.CoreClrConfigurationDetection.IsReleaseRuntime) && !output.Contains(ExpectedMessage))
        {
            Console.WriteLine($"The subprocess terminated for some other reason. Expected to find: {ExpectedMessage}");
            return Fail;
        }

        return Pass;
    }
}
