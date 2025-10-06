// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_120414
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> DuplicateFromVec2(Vector2 s) =>
        Vector128.Create(Unsafe.As<Vector2, double>(ref s)).AsSingle();

    [Fact]
    public static void TestEntryPoint()
    {
        Vector2 testVec = new Vector2(1.0f, 0.5f);
        Vector128<float> result = DuplicateFromVec2(testVec);
        Assert.Equal(1.0f, result[0]);
        Assert.Equal(0.5f, result[1]);
        Assert.Equal(1.0f, result[2]);
        Assert.Equal(0.5f, result[3]);
    }
}
