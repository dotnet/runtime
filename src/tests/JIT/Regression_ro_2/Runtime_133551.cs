// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133551
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestDot128(bool reverse)
    {
        // Dot consumes undefined upper lanes. Only successful compilation and execution
        // are required; there is no specified numerical result to assert.
        Dot128(3.0f, Vector128.Create(2.0f), reverse);
        Dot128(3.0, Vector128.Create(2.0), reverse);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void TestDot256(bool reverse)
    {
        Dot256(3.0f, Vector256.Create(2.0f), reverse);
        Dot256(3.0, Vector256.Create(2.0), reverse);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Dot128<T>(T value, Vector128<T> vector, bool reverse)
    {
        return reverse ? Vector128.Dot(vector, Vector128.CreateScalarUnsafe(value))
                       : Vector128.Dot(Vector128.CreateScalarUnsafe(value), vector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Dot256<T>(T value, Vector256<T> vector, bool reverse)
    {
        return reverse ? Vector256.Dot(vector, Vector256.CreateScalarUnsafe(value))
                       : Vector256.Dot(Vector256.CreateScalarUnsafe(value), vector);
    }

    [Fact]
    public static void ScalarBroadcast()
    {
        Vector128<float> vector = Vector128.Create(2.0f, 4.0f, 6.0f, 8.0f);
        Assert.Equal(Vector128.Create(6.0f, 12.0f, 18.0f, 24.0f), MultiplyLeft(3.0f, vector));
        Assert.Equal(Vector128.Create(6.0f, 12.0f, 18.0f, 24.0f), MultiplyRight(vector, 3.0f));
        Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), Divide(vector, 2.0f));

        Vector128<double> doubles = Vector128.Create(2.0, 4.0);
        Assert.Equal(Vector128.Create(6.0, 12.0), MultiplyLeft(3.0, doubles));
        Assert.Equal(Vector128.Create(6.0, 12.0), MultiplyRight(doubles, 3.0));
        Assert.Equal(Vector128.Create(1.0, 2.0), Divide(doubles, 2.0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<T> MultiplyLeft<T>(T scalar, Vector128<T> vector) => scalar * vector;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<T> MultiplyRight<T>(Vector128<T> vector, T scalar) => vector * scalar;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<T> Divide<T>(Vector128<T> vector, T scalar) => vector / scalar;
}
