// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Stresses the cDAC ArgIterator encoder's <c>__arglist</c> support
/// (<see cref="GCRefMapToken"/>.<c>VASigCookie</c>).
///
/// <para>
/// Lives in its own debuggee because the CLI's native varargs calling
/// convention is only supported on Windows x86 / x64 / ARM64. The JIT
/// gates the feature in <c>src/coreclr/jit/target.h::compFeatureVarArg</c>:
/// <code>
///   return TargetOS::IsWindows &amp;&amp; !TargetArchitecture::IsArm32;
/// </code>
/// So this debuggee's methods will fail to JIT on Linux/macOS (all
/// architectures), Windows ARM32, RISC-V, LoongArch64, and WASM. The
/// xunit harness skips <c>VarArgs</c> on those targets via the
/// <c>WindowsOnly</c> flag on the <c>Debuggee</c> record.
/// </para>
///
/// <para>
/// The <c>VarArgs</c> entry in <c>CdacStressTests.Debuggees</c> also sets
/// <c>SkipGCRefs: true</c>: the cDAC's <c>GetStackReferences</c> does not
/// yet walk the VASigCookie signature blob to enumerate variadic-tail GC
/// refs, so the GCREFS sub-check reports false failures on vararg frames.
/// ARGITER has no such gap (the encoder emits
/// <c>GCRefMapToken.VASigCookie</c> and stops, matching the runtime's
/// <c>FakeGcScanRoots</c> short-circuit), so we still exercise this
/// debuggee under the <c>ArgIterStress_*</c> theory.
/// </para>
/// </summary>
internal static class Program
{
    private static object? s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocBurst()
    {
        for (int i = 0; i < 32; i++)
        {
            s_sink = new object();
        }
    }

    private static int Main()
    {
        for (int iter = 0; iter < 50; iter++)
        {
            Drive();
        }
        GC.KeepAlive(s_sink);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Drive()
    {
        VarargMixed(1, __arglist("a", 2, "b", 3.14));
        VarargAllRefs(1, __arglist("x", "y", "z"));
        VarargFixedPrimitive(__arglist(1, 2L, 3.0));

        var s = new InstanceVarargStruct { R = "this-ref" };
        s.Method(1, __arglist("inst-a", "inst-b"));

        DeepArglistOuter("outer", 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VarargMixed(int first, __arglist) { AllocBurst(); GC.KeepAlive((object)first); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VarargAllRefs(int first, __arglist) { AllocBurst(); GC.KeepAlive((object)first); }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VarargFixedPrimitive(__arglist) { AllocBurst(); }

    private struct InstanceVarargStruct
    {
        public object R;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Method(int first, __arglist)
        {
            AllocBurst();
            GC.KeepAlive(R);
            GC.KeepAlive((object)first);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepArglistOuter(string label, int n)
    {
        AllocBurst();
        DeepArglistInner(n, __arglist(label, n + 1, "tail"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DeepArglistInner(int n, __arglist)
    {
        AllocBurst();
        GC.KeepAlive((object)n);
    }
}
