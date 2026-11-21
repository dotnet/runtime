// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137681
{
    private struct S8
    {
        public short Hot;
        public byte A, B, C, D, E, F;
    }

    private class Holder
    {
        public S8 Value;
    }

    private static Holder s_holder = new Holder();
    private static int s_seen;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe(S8 b)
    {
        s_seen = b.Hot;
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Holder GetHolder(int x) => s_holder;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(short v)
    {
        S8 s = default;
        s.Hot = v;
        s.Hot += v;
        s.Hot ^= 7;
        GetHolder(Observe(s)).Value = s;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Test(5);
        Assert.Equal(13, s_seen);
        Assert.Equal(13, s_holder.Value.Hot);
    }
}
