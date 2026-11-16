// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_92357;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class Runtime_92357
{
    [ConditionalFact(typeof(Avx2), nameof(Avx2.IsSupported))]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Problem()
    {
        int y1 = 5;

        Vector256<short> actual1 = Test1(Vector256<short>.One, ref y1);
        Vector256<short> expected1 = Vector256.Create(10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0);

        Assert.Equal(expected1, actual1);

        long y2 = 5;

        Vector256<int> actual2 = Test2(Vector256<int>.One, ref y2);
        Vector256<int> expected2 = Vector256.Create(10, 0, 10, 0, 10, 0, 10, 0);

        Assert.Equal(expected2, actual2);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector256<short> Test1(Vector256<short> x, ref int y)
    {
        return Avx2.MultiplyLow(x + x, Vector256.Create(y).AsInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector256<int> Test2(Vector256<int> x, ref long y)
    {
        return Avx2.MultiplyLow(x + x, Vector256.Create(y).AsInt32());
    }
}
