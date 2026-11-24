// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using Xunit;

public class Runtime_137900
{
    [Fact]
    [SkipOnMono("CoreCLR JIT regression test")]
    public static void Vector256WithElementPreservesSourceVectorAcrossValueSideEffects()
    {
        Assert.Equal(Vector256.Create(11, 1, 1, 1, 1, 1, 1, 1), WithElement(1, 99));
    }

    [Fact]
    [SkipOnMono("CoreCLR JIT regression test")]
    public static void Vector512WithElementPreservesSourceVectorAcrossValueSideEffects()
    {
        Assert.Equal(Vector512.Create(11, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), WithElement512(1, 99));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<int> WithElement(int x, int y)
    {
        Vector256<int> vector = Vector256.Create(x);
        Span<int> lanes = MemoryMarshal.Cast<Vector256<int>, int>(new Span<Vector256<int>>(ref vector));
        return vector.WithElement(0, Interlocked.Exchange(ref lanes[1], y) + 10);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<int> WithElement512(int x, int y)
    {
        Vector512<int> vector = Vector512.Create(x);
        Span<int> lanes = MemoryMarshal.Cast<Vector512<int>, int>(new Span<Vector512<int>>(ref vector));
        return vector.WithElement(0, Interlocked.Exchange(ref lanes[1], y) + 10);
    }
}
