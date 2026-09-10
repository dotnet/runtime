// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Exercises the INSERTPS (Sse41.Insert for float) lowering path where op2 is a
// zero vector and gets marked contained. Validates that the containment path
// produces correct results across a range of constant control bytes.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class Runtime_130846
{
    // Scalar reference for INSERTPS(a, b, imm8) with b == zero.
    private static Vector128<float> Reference(Vector128<float> a, int imm8)
    {
        int count_d = (imm8 >> 4) & 0x3;
        int zmask   = imm8 & 0xF;

        Span<float> r = stackalloc float[4];
        for (int i = 0; i < 4; i++)
        {
            r[i] = a.GetElement(i);
        }

        // op2 is zero, so the selected source element is 0.
        r[count_d] = 0.0f;

        for (int i = 0; i < 4; i++)
        {
            if (((zmask >> i) & 1) != 0)
            {
                r[i] = 0.0f;
            }
        }

        return Vector128.Create(r[0], r[1], r[2], r[3]);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins00(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x00);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins10(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x10);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins20(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x20);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins30(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x30);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins0E(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x0E);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins4D(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x4D);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins8B(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x8B);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> Ins39(Vector128<float> x) => Sse41.Insert(x, Vector128<float>.Zero, 0x39);

    [ConditionalFact(typeof(Sse41), nameof(Sse41.IsSupported))]
    public static void TestEntryPoint()
    {
        Vector128<float> x = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);

        Assert.Equal(Reference(x, 0x00), Ins00(x));
        Assert.Equal(Reference(x, 0x10), Ins10(x));
        Assert.Equal(Reference(x, 0x20), Ins20(x));
        Assert.Equal(Reference(x, 0x30), Ins30(x));
        Assert.Equal(Reference(x, 0x0E), Ins0E(x));
        Assert.Equal(Reference(x, 0x4D), Ins4D(x));
        Assert.Equal(Reference(x, 0x8B), Ins8B(x));
        Assert.Equal(Reference(x, 0x39), Ins39(x));
    }
}
