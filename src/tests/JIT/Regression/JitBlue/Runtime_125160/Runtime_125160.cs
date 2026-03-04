// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_125160
{
    [ConditionalFact(typeof(Avx), nameof(Avx.IsSupported))]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128.Create(0f, 1f, 1f, 1f), CompareFalseFloat(Vector128<float>.One, Vector128<float>.Zero));
        Assert.Equal(Vector128.Create(0d, 1d), CompareFalseDouble(Vector128<double>.One, Vector128<double>.Zero));

        Vector128<float> trueFloat = CompareTrueFloat(Vector128<float>.One, Vector128<float>.Zero);
        Assert.Equal(BitConverter.UInt32BitsToSingle(0xFFFFFFFF), trueFloat.GetElement(0));
        Assert.Equal(1f, trueFloat.GetElement(1));
        Assert.Equal(1f, trueFloat.GetElement(2));
        Assert.Equal(1f, trueFloat.GetElement(3));

        Vector128<double> trueDouble = CompareTrueDouble(Vector128<double>.One, Vector128<double>.Zero);
        Assert.Equal(BitConverter.UInt64BitsToDouble(0xFFFFFFFFFFFFFFFF), trueDouble.GetElement(0));
        Assert.Equal(1d, trueDouble.GetElement(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> CompareFalseFloat(Vector128<float> x, Vector128<float> y)
        => Avx.CompareScalar(x, y, FloatComparisonMode.OrderedFalseSignaling);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> CompareFalseDouble(Vector128<double> x, Vector128<double> y)
        => Avx.CompareScalar(x, y, FloatComparisonMode.OrderedFalseNonSignaling);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> CompareTrueFloat(Vector128<float> x, Vector128<float> y)
        => Avx.CompareScalar(x, y, FloatComparisonMode.UnorderedTrueSignaling);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> CompareTrueDouble(Vector128<double> x, Vector128<double> y)
        => Avx.CompareScalar(x, y, FloatComparisonMode.UnorderedTrueNonSignaling);
}
