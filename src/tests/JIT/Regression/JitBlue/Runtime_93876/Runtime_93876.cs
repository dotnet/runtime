// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;
using Xunit;

public static class Runtime_93876
{
    [Fact]
    public static void Problem()
    {
        Vector4 v = Mul(0, 1);
        Assert.Equal(Vector4.One, v);
        Vector64<float> v64 = Mul64(0, 1);
        Assert.Equal(Vector64<float>.One, v64);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Mul(float a, float b) => Vector4.Multiply(a + b, Vector4.One);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Mul64(float a, float b) => Vector64.Multiply(a + b, Vector64<float>.One);
}
