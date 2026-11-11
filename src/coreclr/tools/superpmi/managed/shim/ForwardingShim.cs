// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SuperPmi.Shim;

// The shims that forward all the JIT-EE interface calls between the runtime and the real JIT (SuperPMIShimPath),
// optionally counting them. The native side forwards the calls; managed code only counts them.
internal static unsafe class ForwardingShim
{
    static ForwardingShim()
    {
        string logFilePath = Environment.GetEnvironmentVariable("SuperPMIShimLogFilePath");
        if (logFilePath != null)
            Logger.OpenLogFile(logFilePath);
    }

    // Counts the calls, with the name of the method, for the counter shim.
    public static delegate* unmanaged<byte*, void> OnCall { get; set; }

    // Called once the real JIT is loaded, before the shim's objects are created.
    public static Action OnStartup { get; set; }

    public static void JitStartup(IntPtr host)
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();

        if (!ShimEnvironment.LoadRealJitLib())
            return;

        var realJitStartup = (delegate* unmanaged<IntPtr, void>)ShimEnvironment.GetRealJitExport("jitStartup");
        if (realJitStartup == null)
        {
            // This portion of the interface is not used by the JIT under test.
            return;
        }

        OnStartup?.Invoke();
        realJitStartup(ShimInterop.Shim_CreateForwardingJitHost(host, OnCall));
    }

    public static IntPtr GetJit()
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();

        if (!ShimEnvironment.LoadRealJitLib())
            return 0;

        var realGetJit = (delegate* unmanaged<IntPtr>)ShimEnvironment.GetRealJitExport("getJit");
        if (realGetJit == null)
        {
            Logger.LogError("getJit() - GetProcAddress 'getJit' failed (0x0000007f)");
            return 0;
        }

        IntPtr original = realGetJit();
        if (original == 0)
        {
            Logger.LogError("getJit() - pngetJit gave us null");
            return 0;
        }

        // The JIT is used for the lifetime of the process.
        OnStartup?.Invoke();
        return ShimInterop.Shim_CreateForwardingJitCompiler(original, OnCall);
    }
}
