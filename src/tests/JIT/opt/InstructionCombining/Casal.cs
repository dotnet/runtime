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
        if (CompareExchangeAcrossCall() <= 0 || Volatile.Read(ref s_value) != 1)
        {
            throw new Exception("CompareExchange returned an unexpected result.");
        }
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
        long current = GetTicks();
        return old + current;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long GetTicks() => DateTime.UtcNow.Ticks;
}
