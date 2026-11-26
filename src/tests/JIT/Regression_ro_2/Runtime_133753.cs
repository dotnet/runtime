// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133753
{
    public static bool IsSupported128 => AvxVnni.IsSupported && Avx512F.VL.IsSupported;
    public static bool IsSupported512 => AvxVnni.V512.IsSupported && Avx512F.IsSupported;

    [ConditionalTheory(nameof(IsSupported128))]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void Test128(bool words, bool saturate)
    {
        Vector128<int> result = Blend128(words, saturate, Vector128.Create(7), Vector128.Create(5),
            Vector128<byte>.Zero, Vector128<sbyte>.Zero,
            Vector128.Create(1), Vector128.Create(0, 1, 0, 1));

        Assert.Equal(Vector128.Create(7, 5, 7, 5), result);
    }

    [ConditionalTheory(nameof(IsSupported128))]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void Test256(bool words, bool saturate)
    {
        Vector256<int> result = Blend256(words, saturate, Vector256.Create(7), Vector256.Create(5),
            Vector256<byte>.Zero, Vector256<sbyte>.Zero,
            Vector256.Create(1), Vector256.Create(0, 1, 0, 1, 0, 1, 0, 1));

        Assert.Equal(Vector256.Create(7, 5, 7, 5, 7, 5, 7, 5), result);
    }

    [ConditionalTheory(nameof(IsSupported512))]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void Test512(bool words, bool saturate)
    {
        Vector512<int> result = Blend512(words, saturate, Vector512.Create(7), Vector512.Create(5),
            Vector512<byte>.Zero, Vector512<sbyte>.Zero,
            Vector512.Create(1), Vector512.Create(0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1));

        Assert.Equal(Vector512.Create(7, 5, 7, 5, 7, 5, 7, 5, 7, 5, 7, 5, 7, 5, 7, 5), result);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> Blend128(bool words, bool saturate, Vector128<int> fallback, Vector128<int> addend,
        Vector128<byte> left, Vector128<sbyte> right, Vector128<int> a, Vector128<int> b)
    {
        Vector128<int> mask = Avx512F.VL.CompareEqual(a, b);

        return (words, saturate) switch
        {
            (false, false) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAdd(addend, left, right), mask),
            (false, true) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAddSaturate(addend, left, right), mask),
            (true, false) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAdd(addend, left.AsInt16(), right.AsInt16()), mask),
            (true, true) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAddSaturate(addend, left.AsInt16(), right.AsInt16()), mask),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<int> Blend256(bool words, bool saturate, Vector256<int> fallback, Vector256<int> addend,
        Vector256<byte> left, Vector256<sbyte> right, Vector256<int> a, Vector256<int> b)
    {
        Vector256<int> mask = Avx512F.VL.CompareEqual(a, b);

        return (words, saturate) switch
        {
            (false, false) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAdd(addend, left, right), mask),
            (false, true) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAddSaturate(addend, left, right), mask),
            (true, false) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAdd(addend, left.AsInt16(), right.AsInt16()), mask),
            (true, true) => Avx512F.VL.BlendVariable(fallback, AvxVnni.MultiplyWideningAndAddSaturate(addend, left.AsInt16(), right.AsInt16()), mask),
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<int> Blend512(bool words, bool saturate, Vector512<int> fallback, Vector512<int> addend,
        Vector512<byte> left, Vector512<sbyte> right, Vector512<int> a, Vector512<int> b)
    {
        Vector512<int> mask = Avx512F.CompareEqual(a, b);

        return (words, saturate) switch
        {
            (false, false) => Avx512F.BlendVariable(fallback, AvxVnni.V512.MultiplyWideningAndAdd(addend, left, right), mask),
            (false, true) => Avx512F.BlendVariable(fallback, AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, left, right), mask),
            (true, false) => Avx512F.BlendVariable(fallback, AvxVnni.V512.MultiplyWideningAndAdd(addend, left.AsInt16(), right.AsInt16()), mask),
            (true, true) => Avx512F.BlendVariable(fallback, AvxVnni.V512.MultiplyWideningAndAddSaturate(addend, left.AsInt16(), right.AsInt16()), mask),
        };
    }
}
