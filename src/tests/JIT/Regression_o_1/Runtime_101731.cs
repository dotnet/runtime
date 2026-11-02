// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_101731;

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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/112557", TestRuntimes.Mono)]
    public static void TestConvertToInt64NativeSingle(float value)
    {
        Func<float, long> func = float.ConvertToIntegerNative<long>;
        long expectedValue = float.ConvertToIntegerNative<long>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/112557", TestRuntimes.Mono)]
    public static void TestConvertToUInt32NativeDouble(double value)
    {
        Func<double, uint> func = double.ConvertToIntegerNative<uint>;
        uint expectedValue = double.ConvertToIntegerNative<uint>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/112557", TestRuntimes.Mono)]
    public static void TestConvertToUInt32NativeSingle(float value)
    {
        Func<float, uint> func = float.ConvertToIntegerNative<uint>;
        uint expectedValue = float.ConvertToIntegerNative<uint>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/112557", TestRuntimes.Mono)]
    public static void TestConvertToUInt64NativeDouble(double value)
    {
        Func<double, ulong> func = double.ConvertToIntegerNative<ulong>;
        ulong expectedValue = double.ConvertToIntegerNative<ulong>(value);
        Assert.Equal(expectedValue, func(value));
    }

    [Theory]
    [InlineData(float.MaxValue)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/112557", TestRuntimes.Mono)]
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

    [Theory]
    [InlineData(4.0f, 0.25f, 0.5f, 2.0f)]
    public static void TestHalfEstimates(float value, float reciprocal, float reciprocalSqrt, float sqrt)
    {
        Half half = (Half)value;

        Assert.InRange((float)Half.ReciprocalEstimate(half), reciprocal - 0.01f, reciprocal + 0.01f);
        Assert.InRange((float)Half.ReciprocalSqrtEstimate(half), reciprocalSqrt - 0.01f, reciprocalSqrt + 0.01f);
        Assert.Equal((Half)sqrt, Half.Sqrt(half));
    }
}
