// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_137638;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// An atomic intrinsic in an index expression may modify the local holding the array.
// The array must still be the one observed before the index was evaluated.
public class Runtime_137638
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int MDGet(int[,] a) =>
        a[Interlocked.Exchange(ref a, null) != null ? 0 : 1, 0];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int MDGetSecondIndex(int[,] a) =>
        a[0, Interlocked.CompareExchange(ref a, null, a) != null ? 0 : 1];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void MDSet(int[,] a) =>
        a[Interlocked.Exchange(ref Unsafe.As<int[,], object>(ref a), null) != null ? 0 : 1, 0] = 77;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SZGet(int[] a) =>
        a[Interlocked.Exchange(ref a, null) != null ? 0 : 1];

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SZGetObject(int[] a) =>
        a[Interlocked.Exchange(ref Unsafe.As<int[], object>(ref a), null) != null ? 0 : 1];

    [Fact]
    public static void TestEntryPoint()
    {
        int[,] md = { { 42, 43 }, { 84, 85 } };
        Assert.Equal(42, MDGet(md));
        Assert.Equal(42, MDGetSecondIndex(md));

        MDSet(md);
        Assert.Equal(77, md[0, 0]);

        int[] sz = { 42, 84 };
        Assert.Equal(42, SZGet(sz));
        Assert.Equal(42, SZGetObject(sz));
    }
}
