// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public sealed class Casal
{
    private static long s_value;

    [Fact]
    public static void TestEntryPoint()
    {
        Volatile.Write(ref s_value, 7);
        Assert.Equal(7L, CompareExchangeAcrossCall());
        Assert.Equal(1L, Volatile.Read(ref s_value));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CompareExchangeAcrossCall()
    {
        // The CAS result lives across the call and needs a callee-saved register.
        // Its comparand should be materialized directly into that register.
        // ARM64: mov {{x[0-9]+}}, #1
        // ARM64-NOT: mov {{x[0-9]+}}, {{x[0-9]+}}
        // ARM64: casal
        long old = Interlocked.CompareExchange(ref s_value, 1, 7);
        GetTicks();
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long GetTicks() => DateTime.UtcNow.Ticks;
}
