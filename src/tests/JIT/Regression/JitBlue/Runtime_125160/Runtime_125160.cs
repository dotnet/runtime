// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_125160
{
    // The True/False float comparison modes always produce the same result regardless of the
    // inputs. The JIT constant folds these when the operands are constant. For the scalar form
    // (CompareScalar) only the lowest element is set to the comparison result; the upper elements
    // must be copied unchanged from op1. This test ensures the folded result matches the result
    // computed at runtime from non-constant operands.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Opaque<T>(T value) => value;

    [Fact]
    public static void TestEntryPoint()
    {
        if (Avx.IsSupported)
        {
            TestCompareScalarSingle();
            TestCompareScalarDouble();
        }

        if (Avx512F.IsSupported)
        {
            TestCompareSingle();
            TestCompareDouble();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestCompareScalarSingle()
    {
        Vector128<float> left = Vector128.Create(1f, 2f, 3f, 4f);
        Vector128<float> right = Vector128.Create(5f, 6f, 7f, 8f);

        // OrderedFalseNonSignaling: lowest element is all-zero, upper elements copied from op1.
        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseNonSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseNonSignaling));

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseSignaling));

        // UnorderedTrueNonSignaling: lowest element is all-ones, upper elements copied from op1.
        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueNonSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueNonSignaling));

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueSignaling));

        // Explicitly validate that the upper elements are preserved and the low element is set.
        Vector128<float> foldedFalse = Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseNonSignaling);
        Assert.Equal(0f, foldedFalse.GetElement(0));
        Assert.Equal(2f, foldedFalse.GetElement(1));
        Assert.Equal(3f, foldedFalse.GetElement(2));
        Assert.Equal(4f, foldedFalse.GetElement(3));

        Vector128<float> foldedTrue = Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueNonSignaling);
        Assert.Equal(-1, foldedTrue.AsInt32().GetElement(0));
        Assert.Equal(2f, foldedTrue.GetElement(1));
        Assert.Equal(3f, foldedTrue.GetElement(2));
        Assert.Equal(4f, foldedTrue.GetElement(3));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestCompareScalarDouble()
    {
        Vector128<double> left = Vector128.Create(1d, 2d);
        Vector128<double> right = Vector128.Create(3d, 4d);

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseNonSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseNonSignaling));

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseSignaling));

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueNonSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueNonSignaling));

        Assert.Equal(
            Avx.CompareScalar(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueSignaling),
            Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueSignaling));

        Vector128<double> foldedFalse = Avx.CompareScalar(left, right, FloatComparisonMode.OrderedFalseNonSignaling);
        Assert.Equal(0d, foldedFalse.GetElement(0));
        Assert.Equal(2d, foldedFalse.GetElement(1));

        Vector128<double> foldedTrue = Avx.CompareScalar(left, right, FloatComparisonMode.UnorderedTrueNonSignaling);
        Assert.Equal(-1L, foldedTrue.AsInt64().GetElement(0));
        Assert.Equal(2d, foldedTrue.GetElement(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestCompareSingle()
    {
        Vector512<float> left = Vector512.Create(1f);
        Vector512<float> right = Vector512.Create(2f);

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseNonSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.OrderedFalseNonSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.OrderedFalseSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueNonSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.UnorderedTrueNonSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.UnorderedTrueSignaling));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestCompareDouble()
    {
        Vector512<double> left = Vector512.Create(1d);
        Vector512<double> right = Vector512.Create(2d);

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseNonSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.OrderedFalseNonSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.OrderedFalseSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.OrderedFalseSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueNonSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.UnorderedTrueNonSignaling));

        Assert.Equal(
            Avx512F.Compare(Opaque(left), Opaque(right), FloatComparisonMode.UnorderedTrueSignaling),
            Avx512F.Compare(left, right, FloatComparisonMode.UnorderedTrueSignaling));
    }
}
