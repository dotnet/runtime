// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_134197
{
    private class Box<T>
    {
        public T V;
    }

    // Comparing these inputs with zero produces empty, partial, and full masks.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(0, 0)]
    public static void TestBinary512(int first, int rest)
    {
        Vector512<int> comparison = Vector512.Create(rest).WithElement(0, first);
        Vector512<int> a = Vector512.Create(3);
        Vector512<int> fallback = Vector512.Create(4);
        Vector512<int> expected = Vector512.Create(rest == 0 ? 12 : 4).WithElement(0, first == 0 ? 12 : 4);
        Assert.Equal(expected, Add512(comparison, a, new Box<Vector512<int>> { V = Vector512.Create(9) }, fallback));
        Assert.Throws<NullReferenceException>(() => Add512(comparison, a, null, fallback));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(0, 0)]
    public static void TestBroadcast512(int first, int rest)
    {
        Vector512<int> comparison = Vector512.Create(rest).WithElement(0, first);
        Vector512<int> a = Vector512.Create(3);
        Vector512<int> fallback = Vector512.Create(4);
        Vector512<int> expected = Vector512.Create(rest == 0 ? 12 : 4).WithElement(0, first == 0 ? 12 : 4);
        Assert.Equal(expected, Broadcast512(comparison, a, new Box<int> { V = 9 }, fallback));
        Assert.Throws<NullReferenceException>(() => Broadcast512(comparison, a, null, fallback));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(0, 0)]
    public static void TestDirectLoad128(int first, int rest)
    {
        Vector128<int> comparison = Vector128.Create(rest).WithElement(0, first);
        Vector128<int> fallback = Vector128.Create(4);
        Vector128<int> expected = Vector128.Create(rest == 0 ? 9 : 4).WithElement(0, first == 0 ? 9 : 4);
        Assert.Equal(expected, Load128(comparison, new Box<Vector128<int>> { V = Vector128.Create(9) }, fallback));
        Assert.Throws<NullReferenceException>(() => Load128(comparison, null, fallback));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(0, 0)]
    public static void TestDirectBroadcast256(int first, int rest)
    {
        Vector256<int> comparison = Vector256.Create(rest).WithElement(0, first);
        Vector256<int> fallback = Vector256.Create(4);
        Vector256<int> expected = Vector256.Create(rest == 0 ? 9 : 4).WithElement(0, first == 0 ? 9 : 4);
        Assert.Equal(expected, LoadBroadcast256(comparison, new Box<int> { V = 9 }, fallback));
        Assert.Throws<NullReferenceException>(() => LoadBroadcast256(comparison, null, fallback));
    }

    [ConditionalTheory(typeof(Avx512F), nameof(Avx512F.IsSupported))]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(0, 0)]
    public static unsafe void TestAlignedLoad512(int first, int rest)
    {
        const int Alignment = 64;
        byte* buffer = stackalloc byte[2 * Alignment - 1];
        int* source = (int*)(((nuint)buffer + Alignment - 1) & ~(nuint)(Alignment - 1));
        Vector512.Create(9).StoreAligned(source);
        Vector512<int> comparison = Vector512.Create(rest).WithElement(0, first);
        Vector512<int> fallback = Vector512.Create(4);
        Vector512<int> expected = Vector512.Create(rest == 0 ? 9 : 4).WithElement(0, first == 0 ? 9 : 4);
        Assert.Equal(expected, AlignedLoad512(comparison, source, fallback));
        Assert.Throws<NullReferenceException>(() => AlignedLoad512(comparison, null, fallback));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<int> Add512(Vector512<int> comparison, Vector512<int> a, Box<Vector512<int>> b, Vector512<int> fallback) =>
        Vector512.ConditionalSelect(Vector512.Equals(comparison, Vector512<int>.Zero), a + b.V, fallback);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<int> Broadcast512(Vector512<int> comparison, Vector512<int> a, Box<int> b, Vector512<int> fallback) =>
        Vector512.ConditionalSelect(Vector512.Equals(comparison, Vector512<int>.Zero), a + Vector512.Create(b.V), fallback);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> Load128(Vector128<int> comparison, Box<Vector128<int>> b, Vector128<int> fallback) =>
        Vector128.ConditionalSelect(Vector128.Equals(comparison, Vector128<int>.Zero), b.V, fallback);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<int> LoadBroadcast256(Vector256<int> comparison, Box<int> b, Vector256<int> fallback) =>
        Vector256.ConditionalSelect(Vector256.Equals(comparison, Vector256<int>.Zero), Vector256.Create(b.V), fallback);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe Vector512<int> AlignedLoad512(Vector512<int> comparison, int* b, Vector512<int> fallback) =>
        Vector512.ConditionalSelect(Vector512.Equals(comparison, Vector512<int>.Zero), Avx512F.LoadAlignedVector512(b), fallback);
}
