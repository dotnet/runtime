// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_138258
{
    class Holder
    {
        public long Value;
        public int IntValue;
    }

    static long s_value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Consume(long value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ConsumeInt(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OrAndPassToCall(Holder h) => Consume(Interlocked.Or(ref h.Value, 1L << 61));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long AndAndPassToCall(Holder h) => Consume(Interlocked.And(ref h.Value, ~(1L << 61)));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int OrIntAndPassToCall(Holder h) => ConsumeInt(Interlocked.Or(ref h.IntValue, 1 << 30));

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long OrStaticAndPassToCall() => Consume(Interlocked.Or(ref s_value, 1L << 61));

    [Fact]
    public static void TestEntryPoint()
    {
        Holder h = new Holder { Value = 5, IntValue = 5 };

        Assert.Equal(5, OrAndPassToCall(h));
        Assert.Equal(0x2000000000000005, h.Value);

        Assert.Equal(0x2000000000000005, AndAndPassToCall(h));
        Assert.Equal(5, h.Value);

        Assert.Equal(5, OrIntAndPassToCall(h));
        Assert.Equal(0x40000005, h.IntValue);

        s_value = 5;
        Assert.Equal(5, OrStaticAndPassToCall());
        Assert.Equal(0x2000000000000005, s_value);
    }
}
