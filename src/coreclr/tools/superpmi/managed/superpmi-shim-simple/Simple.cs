// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace SuperPmi.Shim;

// superpmi-shim-simple: a JIT that only forwards the JIT-EE interface calls to the real JIT (SuperPMIShimPath),
// to measure the cost of the shims.
internal static class Simple
{
    [UnmanagedCallersOnly(EntryPoint = "jitStartup")]
    private static void jitStartup(IntPtr host) => ForwardingShim.JitStartup(host);

    [UnmanagedCallersOnly(EntryPoint = "getJit")]
    private static IntPtr getJit() => ForwardingShim.GetJit();
}
