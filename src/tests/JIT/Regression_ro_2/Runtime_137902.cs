// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_137902;

public class Runtime_137902
{
    private struct S
    {
        public short A;
        public short B;
    }

    private struct U
    {
        public ushort A;
        public ushort B;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(10, Div(10, 100, 1));
        Assert.Equal(-10, Div(-10, 100, 0));
        Assert.Equal(3, Mod(7, 101, 1));
        Assert.Equal(10u, UDiv(10, 100, 1));
        Assert.Equal(3u, UMod(7, 101, 1));
        Assert.Throws<DivideByZeroException>(() => Div(0, 100, 1));
        Assert.Throws<DivideByZeroException>(() => UDiv(0, 100, 1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S MakeS(short b) => new S { A = 1, B = b };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static U MakeU(ushort b) => new U { A = 1, B = b };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T s) { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Div(int x, int y, short b)
    {
        S s = MakeS(b);
        s.A = (short)x;
        Consume(s);
        return y / s.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Mod(int x, int y, short b)
    {
        S s = MakeS(b);
        s.A = (short)x;
        Consume(s);
        return y % s.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint UDiv(int x, int y, ushort b)
    {
        U s = MakeU(b);
        s.A = (ushort)x;
        Consume(s);
        return (uint)y / s.A;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static uint UMod(int x, int y, ushort b)
    {
        U s = MakeU(b);
        s.A = (ushort)x;
        Consume(s);
        return (uint)y % s.A;
    }
}
