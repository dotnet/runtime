// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_136304
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (!Avx512F.IsSupported)
        {
            return;
        }

        Vector512<float> a = Vector512.Create(1.0f);
        Vector512<float> b = Vector512.Create(2.0f);
        Vector512<float> c = Vector512.Create(0.5f);

        Assert.Equal(Vector512.Create(3.0f), Test(a, b, c, FloatRoundingMode.ToZero));
        Assert.Equal(Vector512<float>.Zero, Test(a, b, b, FloatRoundingMode.ToZero));
    }

    // Embedded masking over an embedded-rounding intrinsic with a non-constant rounding mode
    // needs internal registers for the rounding mode jump table.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Test(Vector512<float> a, Vector512<float> b, Vector512<float> c, FloatRoundingMode mode)
    {
        Vector512<float> sum = Avx512F.Add(a, b, mode);
        return Vector512.ConditionalSelect(Vector512.GreaterThan(a, c), sum, Vector512<float>.Zero);
    }
}
