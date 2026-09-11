// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133518
{
    [ConditionalTheory(typeof(Avx), nameof(Avx.IsSupported))]
    [InlineData(1.0, 1.0)]
    [InlineData(1.0, 2.0)]
    [InlineData(double.NaN, 1.0)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void TestEntryPoint(double left, double right)
    {
        bool unordered = double.IsNaN(left) || double.IsNaN(right);
        int notEqual = !unordered && (left != right) ? -1 : 0;
        int equal = unordered || (left == right) ? -1 : 0;
        Vector256<float> a = Vector256.Create((float)left);
        Vector256<float> b = Vector256.Create((float)right);
        Vector256<double> c = Vector256.Create(left);
        Vector256<double> d = Vector256.Create(right);

        Assert.Equal(Vector256.Create(notEqual), (~Avx.Compare(a, b, FloatComparisonMode.UnorderedEqualNonSignaling)).AsInt32());
        Assert.Equal(Vector256.Create(equal), (~Avx.Compare(a, b, FloatComparisonMode.OrderedNotEqualNonSignaling)).AsInt32());
        Assert.Equal(Vector256.Create(-1), (~Avx.Compare(a, b, FloatComparisonMode.OrderedFalseNonSignaling)).AsInt32());
        Assert.Equal(Vector256<int>.Zero, (~Avx.Compare(a, b, FloatComparisonMode.UnorderedTrueNonSignaling)).AsInt32());

        Assert.Equal(Vector256.Create((long)notEqual), (~Avx.Compare(c, d, FloatComparisonMode.UnorderedEqualNonSignaling)).AsInt64());
        Assert.Equal(Vector256.Create((long)equal), (~Avx.Compare(c, d, FloatComparisonMode.OrderedNotEqualNonSignaling)).AsInt64());
        Assert.Equal(Vector256.Create(-1L), (~Avx.Compare(c, d, FloatComparisonMode.OrderedFalseNonSignaling)).AsInt64());
        Assert.Equal(Vector256<long>.Zero, (~Avx.Compare(c, d, FloatComparisonMode.UnorderedTrueNonSignaling)).AsInt64());

        if (Avx512F.IsSupported)
        {
            Assert.Equal(Vector512.Create(equal),
                (~Avx512F.Compare(Vector512.Create((float)left), Vector512.Create((float)right), FloatComparisonMode.OrderedNotEqualNonSignaling)).AsInt32());
            Assert.Equal(Vector512.Create((long)equal),
                (~Avx512F.Compare(Vector512.Create(left), Vector512.Create(right), FloatComparisonMode.OrderedNotEqualNonSignaling)).AsInt64());
        }
    }
}
