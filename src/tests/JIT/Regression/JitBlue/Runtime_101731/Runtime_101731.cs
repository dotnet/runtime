// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_101731
{
    [Theory]
    [InlineData(double.MaxValue)]
    public static void TestConvertToInt32NativeDouble(double value)
    {
        Func<double, int> func = double.ConvertToIntegerNative<int>;
        int expectedValue = double.ConvertToIntegerNative<int>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    public static void TestConvertToInt32NativeSingle(float value)
    {
        Func<float, int> func = float.ConvertToIntegerNative<int>;
        int expectedValue = float.ConvertToIntegerNative<int>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(double.MaxValue)]
    public static void TestConvertToInt64NativeDouble(double value)
    {
        Func<double, long> func = double.ConvertToIntegerNative<long>;
        long expectedValue = double.ConvertToIntegerNative<long>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    public static void TestConvertToInt64NativeSingle(float value)
    {
        Func<float, long> func = float.ConvertToIntegerNative<long>;
        long expectedValue = float.ConvertToIntegerNative<long>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(double.MaxValue)]
    public static void TestConvertToUInt32NativeDouble(double value)
    {
        Func<double, uint> func = double.ConvertToIntegerNative<uint>;
        uint expectedValue = double.ConvertToIntegerNative<uint>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    public static void TestConvertToUInt32NativeSingle(float value)
    {
        Func<float, uint> func = float.ConvertToIntegerNative<uint>;
        uint expectedValue = float.ConvertToIntegerNative<uint>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(double.MaxValue)]
    public static void TestConvertToUInt64NativeDouble(double value)
    {
        Func<double, ulong> func = double.ConvertToIntegerNative<ulong>;
        ulong expectedValue = double.ConvertToIntegerNative<ulong>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    public static void TestConvertToUInt64NativeSingle(float value)
    {
        Func<float, ulong> func = float.ConvertToIntegerNative<ulong>;
        ulong expectedValue = float.ConvertToIntegerNative<ulong>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(5)]
    public static void TestReciprocalEstimateDouble(double value)
    {
        Func<double, double> func = double.ReciprocalEstimate;
        double expectedValue = double.ReciprocalEstimate(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(5)]
    public static void TestReciprocalEstimateSingle(float value)
    {
        Func<float, float> func = float.ReciprocalEstimate;
        float expectedValue = float.ReciprocalEstimate(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(-double.Epsilon)]
    public static void TestReciprocalSqrtEstimateDouble(double value)
    {
        Func<double, double> func = double.ReciprocalSqrtEstimate;
        double expectedValue = double.ReciprocalSqrtEstimate(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(-float.Epsilon)]
    public static void TestReciprocalSqrtEstimateSingle(float value)
    {
        Func<float, float> func = float.ReciprocalSqrtEstimate;
        float expectedValue = float.ReciprocalSqrtEstimate(value);
        Assert.Equal(expectedValue, func(value));
    }
}
