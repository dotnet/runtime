// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

// Codegen for NI_AVX512_NotMask selected `knotb` for any mask with 8 or fewer
// lanes. For a 2- or 4-lane mask `knotb` inverts all 8 bits of the mask
// register, leaving the bits above the lane count set, and nothing cleared
// them. A following MoveMask handed those stale bits to PopCount, which then
// over-counted by (8 - laneCount).

public class Runtime_124154
{
    [ConditionalFact(typeof(Avx512F), nameof(Avx512F.IsSupported))]
    public static void TestEntryPoint()
    {
        // Vector128<int>: 4 lanes, `knotb` left 4 stale bits set.
        for (int n = 0; n <= 4; n++)
        {
            Assert.Equal(n, Count128Int32(Vector128<int>.Zero, Differing128Int32(n)));
        }

        // Vector128<long>: 2 lanes, the worst case at 6 stale bits.
        for (int n = 0; n <= 2; n++)
        {
            Assert.Equal(n, Count128Int64(Vector128<long>.Zero, Differing128Int64(n)));
        }

        // Vector256<long>: 4 lanes.
        for (int n = 0; n <= 4; n++)
        {
            Assert.Equal(n, Count256Int64(Vector256<long>.Zero, Differing256Int64(n)));
        }

        // Vector128<short>: 8 lanes, unaffected. Guards against a fix that
        // over-corrects the full-width case.
        for (int n = 0; n <= 8; n++)
        {
            Assert.Equal(n, Count128Int16(Vector128<short>.Zero, Differing128Int16(n)));
        }
    }

    private static Vector128<int> Differing128Int32(int n)
    {
        int[] values = new int[4];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1;
        }

        return Vector128.Create(values);
    }

    private static Vector128<long> Differing128Int64(int n)
    {
        long[] values = new long[2];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1;
        }

        return Vector128.Create(values);
    }

    private static Vector256<long> Differing256Int64(int n)
    {
        long[] values = new long[4];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1;
        }

        return Vector256.Create(values);
    }

    private static Vector128<short> Differing128Int16(int n)
    {
        short[] values = new short[8];
        for (int i = 0; i < n; i++)
        {
            values[i] = 1;
        }

        return Vector128.Create(values);
    }

    // ~Vector.Equals(a, b) imports as NotMask(CompareEqualMask(a, b)) and
    // ExtractMostSignificantBits as MoveMask, which is the miscompiled chain.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count128Int32(Vector128<int> a, Vector128<int> b) =>
        BitOperations.PopCount((~Vector128.Equals(a, b)).ExtractMostSignificantBits());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count128Int64(Vector128<long> a, Vector128<long> b) =>
        BitOperations.PopCount((~Vector128.Equals(a, b)).ExtractMostSignificantBits());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count256Int64(Vector256<long> a, Vector256<long> b) =>
        BitOperations.PopCount((~Vector256.Equals(a, b)).ExtractMostSignificantBits());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count128Int16(Vector128<short> a, Vector128<short> b) =>
        BitOperations.PopCount((~Vector128.Equals(a, b)).ExtractMostSignificantBits());
}
