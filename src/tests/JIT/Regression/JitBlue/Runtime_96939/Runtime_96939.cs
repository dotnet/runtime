// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_96939
{
    [Fact]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Problem()
    {
        Assert.Equal(new Vector2(13), TestVector2(new Vector2(2, 3)));
        Assert.Equal(new Vector3(29), TestVector3(new Vector3(2, 3, 4)));
        Assert.Equal(new Vector4(54), TestVector4(new Vector4(2, 3, 4, 5)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector2 TestVector2(Vector2 value)
    {
        return Vector2.Dot(value, value) * new Vector2(1, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3 TestVector3(Vector3 value)
    {
        return Vector3.Dot(value, value) * new Vector3(1, 1, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector4 TestVector4(Vector4 value)
    {
        return Vector4.Dot(value, value) * new Vector4(1, 1, 1, 1);
    }
}
