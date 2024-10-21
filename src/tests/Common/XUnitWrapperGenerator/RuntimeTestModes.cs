// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Xunit
{
    [Flags]
    public enum RuntimeTestModes
    {
        // Disable always when using coreclr runtime.
        Any = ~0,

        // We're running regular tests with no runtime stress modes
        RegularRun = 1,

        // JitStress, JitStressRegs, JitMinOpts and TailcallStress enable
        // various modes in the JIT that cause us to exercise more code paths,
        // and generate different kinds of code
        JitStress = 1 << 1, // DOTNET_JitStress is set.
        JitStressRegs = 1 << 2, // DOTNET_JitStressRegs is set.
        JitMinOpts = 1 << 3, // DOTNET_JITMinOpts is set.
        TailcallStress = 1 << 4, // DOTNET_TailcallStress is set.

        // DisableR2R says to not use ReadyToRun images.
        // This means we JIT everything.
        DisableR2R = 1 << 5, // DOTNET_ReadyToRun=0

        // GCStress3 forces a GC at various locations, typically transitions
        // to/from the VM from managed code.
        GCStress3 = 1 << 6,  // DOTNET_GCStress includes mode 0x3.

        // GCStressC forces a GC at every JIT-generated code instruction,
        // including in NGEN/ReadyToRun code.
        GCStressC = 1 << 7, // DOTNET_GCStress includes mode 0xC.
        AnyGCStress = GCStress3 | GCStressC, // Disable when any GCStress is exercised.
        // TieredCompilation is on by default, but can cause some tests to fail
        // As TieredCompilation is on by default, it does not count as a stress mode for RegularRun.
        TieredCompilation = 1 << 8, // DOTNET_TieredCompilation (or COMPlus_TieredCompilation) is not set to 0.

        AnyJitStress = JitStress | JitStressRegs | JitMinOpts | TailcallStress, // Disable when any JIT stress mode is exercised.

        AnyJitOptimizationStress = AnyJitStress | TieredCompilation, // Disable when any JIT non-full optimization stress mode is exercised.

        HeapVerify = 1 << 9, // DOTNET_HeapVerify (or COMPlus_HeapVerify) is set.
    }
}
