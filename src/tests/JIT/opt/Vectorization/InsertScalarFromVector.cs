// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validates the JIT folding of Insert(vector, vector2.GetElement(idx1), idx2)
// into a single insertps that selects the source element directly via the
// count_s field of the immediate. Both the register-source form (which the fold
// rewrites) and the memory-source form (already handled) must match a scalar
// reference for every combination of source and destination indices.
//
// The indices must be JIT-time constants for the insertps immediate (and hence
// the fold) to be formed, so each combination is dispatched through a switch of
// literal WithElement/GetElement calls rather than passing the indices as
// arguments.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class InsertScalarFromVector
{
    private static readonly float[] s_dst = { 10f, 11f, 12f, 13f };
    private static readonly float[] s_src = { 20f, 21f, 22f, 23f };

    // src is produced by an arithmetic op so it stays in a register, forcing the
    // register GetElement path that the fold collapses into insertps' count_s.
    // combo == dstIdx * 4 + srcIdx, with both indices as compile-time constants.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> FromRegister(Vector128<float> dst, Vector128<float> a, Vector128<float> b, int combo)
    {
        Vector128<float> src = a + b;
        return combo switch
        {
            0  => dst.WithElement(0, src.GetElement(0)),
            1  => dst.WithElement(0, src.GetElement(1)),
            2  => dst.WithElement(0, src.GetElement(2)),
            3  => dst.WithElement(0, src.GetElement(3)),
            4  => dst.WithElement(1, src.GetElement(0)),
            5  => dst.WithElement(1, src.GetElement(1)),
            6  => dst.WithElement(1, src.GetElement(2)),
            7  => dst.WithElement(1, src.GetElement(3)),
            8  => dst.WithElement(2, src.GetElement(0)),
            9  => dst.WithElement(2, src.GetElement(1)),
            10 => dst.WithElement(2, src.GetElement(2)),
            11 => dst.WithElement(2, src.GetElement(3)),
            12 => dst.WithElement(3, src.GetElement(0)),
            13 => dst.WithElement(3, src.GetElement(1)),
            14 => dst.WithElement(3, src.GetElement(2)),
            15 => dst.WithElement(3, src.GetElement(3)),
            _  => throw new ArgumentOutOfRangeException(nameof(combo)),
        };
    }

    // src is passed directly and is typically consumed from memory, exercising the
    // pre-existing insertps-with-memory-operand path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> FromMemory(Vector128<float> dst, Vector128<float> src, int combo)
    {
        return combo switch
        {
            0  => dst.WithElement(0, src.GetElement(0)),
            1  => dst.WithElement(0, src.GetElement(1)),
            2  => dst.WithElement(0, src.GetElement(2)),
            3  => dst.WithElement(0, src.GetElement(3)),
            4  => dst.WithElement(1, src.GetElement(0)),
            5  => dst.WithElement(1, src.GetElement(1)),
            6  => dst.WithElement(1, src.GetElement(2)),
            7  => dst.WithElement(1, src.GetElement(3)),
            8  => dst.WithElement(2, src.GetElement(0)),
            9  => dst.WithElement(2, src.GetElement(1)),
            10 => dst.WithElement(2, src.GetElement(2)),
            11 => dst.WithElement(2, src.GetElement(3)),
            12 => dst.WithElement(3, src.GetElement(0)),
            13 => dst.WithElement(3, src.GetElement(1)),
            14 => dst.WithElement(3, src.GetElement(2)),
            15 => dst.WithElement(3, src.GetElement(3)),
            _  => throw new ArgumentOutOfRangeException(nameof(combo)),
        };
    }

    private static Vector128<float> Reference(int dstIdx, int srcIdx)
    {
        float[] result = (float[])s_dst.Clone();
        result[dstIdx] = s_src[srcIdx];
        return Vector128.Create(result[0], result[1], result[2], result[3]);
    }

    [Fact]
    public static void TestAllCombinations()
    {
        Vector128<float> dst = Vector128.Create(s_dst[0], s_dst[1], s_dst[2], s_dst[3]);
        Vector128<float> src = Vector128.Create(s_src[0], s_src[1], s_src[2], s_src[3]);
        Vector128<float> zero = Vector128<float>.Zero;

        for (int dstIdx = 0; dstIdx < 4; dstIdx++)
        {
            for (int srcIdx = 0; srcIdx < 4; srcIdx++)
            {
                int combo = (dstIdx * 4) + srcIdx;
                Vector128<float> expected = Reference(dstIdx, srcIdx);

                Assert.Equal(expected, FromRegister(dst, src, zero, combo));
                Assert.Equal(expected, FromMemory(dst, src, combo));
            }
        }
    }

    // Chained WithElement using elements pulled from another vector exercises
    // composing the source-selection fold with the existing insert chains.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Chained(Vector128<float> dst, Vector128<float> a, Vector128<float> b)
    {
        Vector128<float> src = a + b;
        return dst.WithElement(0, src.GetElement(3))
                  .WithElement(2, src.GetElement(1));
    }

    [Fact]
    public static void TestChained()
    {
        Vector128<float> dst = Vector128.Create(s_dst[0], s_dst[1], s_dst[2], s_dst[3]);
        Vector128<float> src = Vector128.Create(s_src[0], s_src[1], s_src[2], s_src[3]);
        Vector128<float> zero = Vector128<float>.Zero;

        Vector128<float> actual = Chained(dst, src, zero);
        Vector128<float> expected = Vector128.Create(s_src[3], s_dst[1], s_src[1], s_dst[3]);

        Assert.Equal(expected, actual);
    }

    private static readonly float[] s_src8 = { 20f, 21f, 22f, 23f, 24f, 25f, 26f, 27f };

    // Extracting from a vector wider than 128 bits is also foldable when the element
    // is one of the source's first four (which live in the low 128 bits): the fold
    // feeds the wide register straight to insertps for GetElement(0)/ToScalar, while
    // the existing GetElement lowering narrows the higher elements via GetLower/GetUpper.
    // combo == dstIdx * 8 + srcIdx, with both indices as compile-time constants.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> FromWideRegister(Vector128<float> dst, Vector256<float> a, Vector256<float> b, int combo)
    {
        Vector256<float> src = a + b;
        return combo switch
        {
            0  => dst.WithElement(0, src.GetElement(0)),
            9  => dst.WithElement(1, src.GetElement(1)),
            18 => dst.WithElement(2, src.GetElement(2)),
            27 => dst.WithElement(3, src.GetElement(3)),
            4  => dst.WithElement(0, src.GetElement(4)),
            13 => dst.WithElement(1, src.GetElement(5)),
            22 => dst.WithElement(2, src.GetElement(6)),
            31 => dst.WithElement(3, src.GetElement(7)),
            _  => throw new ArgumentOutOfRangeException(nameof(combo)),
        };
    }

    private static Vector128<float> WideReference(int dstIdx, int srcIdx)
    {
        float[] result = (float[])s_dst.Clone();
        result[dstIdx] = s_src8[srcIdx];
        return Vector128.Create(result[0], result[1], result[2], result[3]);
    }

    [Fact]
    public static void TestWideSource()
    {
        Vector128<float> dst = Vector128.Create(s_dst[0], s_dst[1], s_dst[2], s_dst[3]);
        Vector256<float> src = Vector256.Create(s_src8[0], s_src8[1], s_src8[2], s_src8[3],
                                                s_src8[4], s_src8[5], s_src8[6], s_src8[7]);
        Vector256<float> zero = Vector256<float>.Zero;

        for (int i = 0; i < 8; i++)
        {
            int dstIdx = i % 4;
            int combo = (dstIdx * 8) + i;

            Assert.Equal(WideReference(dstIdx, i), FromWideRegister(dst, src, zero, combo));
        }
    }
}
