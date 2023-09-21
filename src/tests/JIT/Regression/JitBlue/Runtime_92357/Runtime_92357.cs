// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class Runtime_92357
{
    [Fact]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Problem()
    {
        if (!Avx2.IsSupported)
        {
            return;
        }

        int y = 5;

        Vector256<short> actual = Test(Vector256.Create((short)1), ref y);
        Vector256<short> expected = Vector256.Create(10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0, 10, 0);

        Assert.Equal(expected, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector256<short> Test(Vector256<short> x, ref int y)
    {
        return Avx2.MultiplyLow(x + x, Vector256.Create(y).AsInt16());
    }
}
