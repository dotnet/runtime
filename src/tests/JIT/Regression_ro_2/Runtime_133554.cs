// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133554
{
    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 2.0)]
    [InlineData(double.NaN, 1.0)]
    public static void TestSelect(double x, double y)
    {
        // Only the low lane is defined by CreateScalarUnsafe.
        Assert.Equal(x == y ? 0.0 : 7.0, Select(x, y, Vector512.Create(7.0)).ToScalar());
        Assert.Equal(x == y ? 0.0f : 7.0f, Select((float)x, (float)y, Vector512.Create(7.0f)).ToScalar());

        float singleX = (float)x;
        float singleY = (float)y;
        Assert.Equal(x == y ? 0.0 : 7.0, SelectMemory(ref x, ref y, Vector512.Create(7.0)).ToScalar());
        Assert.Equal(x == y ? 0.0f : 7.0f, SelectMemory(ref singleX, ref singleY, Vector512.Create(7.0f)).ToScalar());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<T> Select<T>(T x, T y, Vector512<T> vector)
    {
        return Vector512.ConditionalSelect(
            Vector512.Equals(Vector512.CreateScalarUnsafe(x), Vector512.CreateScalarUnsafe(y)),
            Vector512<T>.Zero, vector);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<T> SelectMemory<T>(ref T x, ref T y, Vector512<T> vector)
    {
        return Vector512.ConditionalSelect(
            Vector512.Equals(Vector512.CreateScalarUnsafe(x), Vector512.CreateScalarUnsafe(y)),
            Vector512<T>.Zero, vector);
    }
}
