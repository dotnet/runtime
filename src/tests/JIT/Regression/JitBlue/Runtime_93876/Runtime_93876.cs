// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public static class Runtime_93876
{
    [Fact]
    public static void Problem()
    {
        Vector4 v = Mul(0, 1);
        Assert.Equal(Vector4.One, v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 Mul(float a, float b) => Vector4.Multiply(a + b, Vector4.One);
}
