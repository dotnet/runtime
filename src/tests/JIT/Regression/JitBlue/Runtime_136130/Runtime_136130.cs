// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_136130
{
    [ConditionalFact(typeof(Avx), nameof(Avx.IsSupported))]
    public static void TestEntryPoint()
    {
        Vector256<float> x = Vector256.Create(1.0f, 2.0f, -32768.0f, 4.0f, float.NaN, 6.0f, 7.0f, 8.0f);
        Vector256<float> sentinel = Vector256.Create(-32768.0f);
        Vector256<int> expectedUnorderedEqual = Vector256.Create(1.0f, 2.0f, 0.0f, 4.0f, 0.0f, 6.0f, 7.0f, 8.0f).AsInt32();
        Vector256<int> expectedOrderedNotEqual =
            Vector256.Create(0.0f, 0.0f, -32768.0f, 0.0f, float.NaN, 0.0f, 0.0f, 0.0f).AsInt32();

        Assert.Equal(expectedUnorderedEqual, AndNotUnorderedEqualNonSignaling(x, sentinel).AsInt32());
        Assert.Equal(expectedUnorderedEqual, AndNotUnorderedEqualSignaling(x, sentinel).AsInt32());
        Assert.Equal(expectedOrderedNotEqual, AndNotOrderedNotEqualNonSignaling(x, sentinel).AsInt32());
        Assert.Equal(expectedOrderedNotEqual, AndNotOrderedNotEqualSignaling(x, sentinel).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> AndNotUnorderedEqualNonSignaling(Vector256<float> x, Vector256<float> sentinel) =>
        Avx.AndNot(Avx.Compare(x, sentinel, FloatComparisonMode.UnorderedEqualNonSignaling), x);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> AndNotUnorderedEqualSignaling(Vector256<float> x, Vector256<float> sentinel) =>
        Avx.AndNot(Avx.Compare(x, sentinel, FloatComparisonMode.UnorderedEqualSignaling), x);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> AndNotOrderedNotEqualNonSignaling(Vector256<float> x, Vector256<float> sentinel) =>
        Avx.AndNot(Avx.Compare(x, sentinel, FloatComparisonMode.OrderedNotEqualNonSignaling), x);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> AndNotOrderedNotEqualSignaling(Vector256<float> x, Vector256<float> sentinel) =>
        Avx.AndNot(Avx.Compare(x, sentinel, FloatComparisonMode.OrderedNotEqualSignaling), x);
}
