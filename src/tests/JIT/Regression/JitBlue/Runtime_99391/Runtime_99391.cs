// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public class Runtime_99391
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector2 result2a = Vector2.Normalize(Value2);
        Assert.Equal(new Vector2(0, 1), result2a);

        Vector2 result2b = Vector2.Normalize(new Vector2(0, 2));
        Assert.Equal(new Vector2(0, 1), result2b);

        Vector3 result3a = Vector3.Normalize(Value3);
        Assert.Equal(new Vector3(0, 0, 1), result3a);

        Vector3 result3b = Vector3.Normalize(new Vector3(0, 0, 2));
        Assert.Equal(new Vector3(0, 0, 1), result3b);

        Vector4 result4a = Vector4.Normalize(Value4);
        Assert.Equal(new Vector4(0, 0, 0, 1), result4a);

        Vector4 result4b = Vector4.Normalize(new Vector4(0, 0, 0, 2));
        Assert.Equal(new Vector4(0, 0, 0, 1), result4b);
    }

    private static Vector2 Value2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => new Vector2(0, 2);
    }

    private static Vector3 Value3
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => new Vector3(0, 0, 2);
    }

    private static Vector4 Value4
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => new Vector4(0, 0, 0, 2);
    }
}
