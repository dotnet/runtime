// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// On x86 the runtime decodes an EBP-frame epilog instruction by instruction (UnwindEbpDoubleAlignFrameEpilog in
// src/coreclr/vm/gc_unwind_x86.inl). A method that uses localloc and saves no callee-saved registers must
// restore ESP with "lea esp, [ebp]" (a zero displacement), which that decoder expects, and never with
// "mov esp, ebp": the decoder then misreads the epilog, which asserts in checked builds when a thread is
// suspended in such an epilog (ThreadSuspend::SuspendAllThreads -> Thread::Hijack) and mis-unwinds in release.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static unsafe class Runtime_135793
{
    [Fact]
    public static int TestEntryPoint()
    {
        int n = Environment.ProcessorCount > 0 ? 17 : 18;
        return (LocallocNonConst(n) == (byte)n && LocallocConst64(n) == (byte)n) ? 100 : 1;
    }

    // A non-constant size keeps this a real localloc in every JIT configuration. The method needs no
    // callee-saved register, so the epilog's "lea esp, [ebp-N]" has N == 0.
    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    private static int LocallocNonConst(int n)
    {
        byte* p = stackalloc byte[n];
        p[0] = (byte)n;
        // X86-FULL-LINE: lea esp, [ebp]
        // X86-FULL-LINE-NEXT: pop ebp
        return p[0];
    }

    // 64 bytes is above the threshold at which a constant-size stackalloc becomes an ordinary local.
    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    private static int LocallocConst64(int n)
    {
        byte* p = stackalloc byte[64];
        p[0] = (byte)n;
        // X86-FULL-LINE: lea esp, [ebp]
        // X86-FULL-LINE-NEXT: pop ebp
        return p[0];
    }
}
