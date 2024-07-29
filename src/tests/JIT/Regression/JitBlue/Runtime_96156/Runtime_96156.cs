// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_96156
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe static Vector128<float> BroadcastScalarToVector128(float value)
    {
        return Avx.BroadcastScalarToVector128(&value);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Vector128<float> c = Vector128.Create(1.0f);
        Vector128<float> r = Problem(2.0f, 0.5f, c);
        Assert.Equal(Vector128.Create(4.0f), r);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Problem(float a, float b, Vector128<float> c)
    {
        return Avx.Multiply(c, BroadcastScalarToVector128(a / b));
    }
}
