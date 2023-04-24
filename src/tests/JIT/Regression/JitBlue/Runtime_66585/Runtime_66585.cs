// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test would segfault/AV in Caller on arm32 because it put the address of
// Callee into r12, but then also tried to use r12 to unwind a large stack
// frame.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_66585
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        GetCaller()(0, 1, 2, 3);
        return 100;
    }

    private static SLarge s_s;
    internal static void Caller(int r0, int r1, int r2, int r3)
    {
        SLarge s = s_s;
        Consume(s);
        Callee(r0, r1, r2, r3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Callee(int r0, int r1, int r2, int r3)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(SLarge s)
    {
    }

    // We cannot mark Caller as noinline because it inhibits tailcalling as well
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static delegate*<int, int, int, int, void> GetCaller()
        => &Caller;

    [StructLayout(LayoutKind.Sequential, Size = 0x1008)]
    private struct SLarge
    {
        public int Value;
    }
}
