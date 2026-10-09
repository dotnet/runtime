// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134935
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(512f, Test(new float[64], 1f, 1f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector256<float> value)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static float Test(float[] array, float x, float y)
    {
        Vector256<float> accumulator = default;
        for (int i = 0; i < array.Length; i++)
        {
            accumulator = accumulator * Vector256.Create(x) + Vector256.Create(y);
            Consume(accumulator);
        }

        return Vector256.Sum(accumulator);
    }
}
