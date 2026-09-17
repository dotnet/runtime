// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134150
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector256<long>.Zero, IsNegative(Vector256<double>.Zero).AsInt64());
        Assert.Equal(Vector256<int>.AllBitsSet, IsPositive(Vector256<float>.Zero).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> IsNegative(Vector256<double> vector) => Vector256.IsNegative(vector);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> IsPositive(Vector256<float> vector) => Vector256.IsPositive(vector);
}
