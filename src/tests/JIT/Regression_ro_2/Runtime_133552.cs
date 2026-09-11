// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133552
{
    [Fact]
    public static void TestRuntimeBroadcast()
    {
        // A widened load must not read the different neighboring word.
        float[] source = [-1.0f, 2.0f];
        Vector128<float> value = Vector128.Create(0x12345678u).AsSingle();
        Vector128<double> left = Vector128.Create(1.0, 2.0);
        Vector128<double> right = Vector128.Create(1.0, 3.0);
        Vector128<double> fallback = Vector128.Create(9.0);
        Vector128<double> expected = Vector128.Create(0x1200000012000000ul, 0).AsDouble().WithElement(1, 9.0);

        Assert.Equal(expected, SelectRuntime(value, ref source[0], left, right, fallback));
    }

    [Fact]
    public static void TestWideConstantBroadcast()
    {
        Vector256<float> value = Vector256.Create(0x12345678u).AsSingle();
        Vector256<double> left = Vector256.Create(1.0, 2.0, 1.0, 2.0);
        Vector256<double> right = Vector256.Create(1.0, 3.0, 1.0, 3.0);
        Vector256<double> fallback = Vector256.Create(9.0);
        Vector256<double> expected = Vector256.Create(0xADB45678ADB45678ul).AsDouble().WithElement(1, 9.0).WithElement(3, 9.0);

        Assert.Equal(expected, SelectConstant(value, left, right, fallback));
    }

    [Fact]
    public static void TestBroadcastAndZero()
    {
        int[] source = [0x12345678, 0];
        Assert.True(BroadcastAndZero(Vector512<int>.Zero, ref source[0]));
        Assert.False(BroadcastAndZero(Vector512<int>.AllBitsSet, ref source[0]));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> SelectRuntime(Vector128<float> value, ref float source, Vector128<double> left, Vector128<double> right, Vector128<double> fallback) =>
        Vector128.ConditionalSelect(Vector128.Equals(left, right), (value & Vector128.Create(source)).AsDouble(), fallback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> SelectConstant(Vector256<float> value, Vector256<double> left, Vector256<double> right, Vector256<double> fallback) =>
        Vector256.ConditionalSelect(Vector256.Equals(left, right), (value ^ Vector256.Create(-1.0f)).AsDouble(), fallback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BroadcastAndZero(Vector512<int> value, ref int source) =>
        (value & Vector512.Create(source)).AsInt64() == Vector512<long>.Zero;
}
