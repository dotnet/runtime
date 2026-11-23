// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace SuperPmi.Shim;

// superpmi-shim-counter: a JIT that forwards the JIT-EE interface calls to the real JIT (SuperPMIShimPath), and
// counts them; the native side writes the counts to a .csv file in SuperPMIShimLogPath when the shim is unloaded.
internal static class Counter
{
    private static bool s_started;

    // The runtime calls jitStartup and then getJit, on the same thread.
    private static void Startup()
    {
        if (s_started)
            return;

        s_started = true;
        ShimEnvironment.SetLogPath();
        ShimInterop.Shim_EnableCallCounting(
            ShimEnvironment.GetResultFileName(ShimEnvironment.LogPath, ShimEnvironment.GetProcessCommandLine(), ".csv"));
    }

    [UnmanagedCallersOnly(EntryPoint = "jitStartup")]
    private static void jitStartup(IntPtr host)
    {
        Startup();
        ForwardingShim.JitStartup(host);
    }

    [UnmanagedCallersOnly(EntryPoint = "getJit")]
    private static IntPtr getJit()
    {
        Startup();
        return ForwardingShim.GetJit();
    }
}
