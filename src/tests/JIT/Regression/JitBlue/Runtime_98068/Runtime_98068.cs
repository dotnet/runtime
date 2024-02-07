// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public static class Runtime_98068
{
    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMax()
    {
        // Double

        Assert.Equal(Max(double.NaN, +1.0), Max_Value_One(double.NaN));
        Assert.Equal(Max(double.NaN, +0.0), Max_Value_Zero(double.NaN));
        Assert.Equal(Max(double.NaN, -0.0), Max_Value_NegZero(double.NaN));

        Assert.Equal(Max(+1.0, double.NaN), Max_Value_NaN(+1.0));
        Assert.Equal(Max(+0.0, double.NaN), Max_Value_NaN(+0.0));
        Assert.Equal(Max(-0.0, double.NaN), Max_Value_NaN(-0.0));

        // Single

        Assert.Equal(Max(float.NaN, +1.0f), Max_Value_One(float.NaN));
        Assert.Equal(Max(float.NaN, +0.0f), Max_Value_Zero(float.NaN));
        Assert.Equal(Max(float.NaN, -0.0f), Max_Value_NegZero(float.NaN));

        Assert.Equal(Max(+1.0f, float.NaN), Max_Value_NaN(+1.0f));
        Assert.Equal(Max(+0.0f, float.NaN), Max_Value_NaN(+0.0f));
        Assert.Equal(Max(-0.0f, float.NaN), Max_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMaxMagnitude()
    {
        // Double

        Assert.Equal(MaxMagnitude(double.NaN, +1.0), MaxMagnitude_Value_One(double.NaN));
        Assert.Equal(MaxMagnitude(double.NaN, +0.0), MaxMagnitude_Value_Zero(double.NaN));
        Assert.Equal(MaxMagnitude(double.NaN, -0.0), MaxMagnitude_Value_NegZero(double.NaN));

        Assert.Equal(MaxMagnitude(+1.0, double.NaN), MaxMagnitude_Value_NaN(+1.0));
        Assert.Equal(MaxMagnitude(+0.0, double.NaN), MaxMagnitude_Value_NaN(+0.0));
        Assert.Equal(MaxMagnitude(-0.0, double.NaN), MaxMagnitude_Value_NaN(-0.0));

        // Single

        Assert.Equal(MaxMagnitude(float.NaN, +1.0f), MaxMagnitude_Value_One(float.NaN));
        Assert.Equal(MaxMagnitude(float.NaN, +0.0f), MaxMagnitude_Value_Zero(float.NaN));
        Assert.Equal(MaxMagnitude(float.NaN, -0.0f), MaxMagnitude_Value_NegZero(float.NaN));

        Assert.Equal(MaxMagnitude(+1.0f, float.NaN), MaxMagnitude_Value_NaN(+1.0f));
        Assert.Equal(MaxMagnitude(+0.0f, float.NaN), MaxMagnitude_Value_NaN(+0.0f));
        Assert.Equal(MaxMagnitude(-0.0f, float.NaN), MaxMagnitude_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMaxMagnitudeNumber()
    {
        // Double

        Assert.Equal(MaxMagnitudeNumber(double.NaN, +1.0), MaxMagnitudeNumber_Value_One(double.NaN));
        Assert.Equal(MaxMagnitudeNumber(double.NaN, +0.0), MaxMagnitudeNumber_Value_Zero(double.NaN));
        Assert.Equal(MaxMagnitudeNumber(double.NaN, -0.0), MaxMagnitudeNumber_Value_NegZero(double.NaN));

        Assert.Equal(MaxMagnitudeNumber(+1.0, double.NaN), MaxMagnitudeNumber_Value_NaN(+1.0));
        Assert.Equal(MaxMagnitudeNumber(+0.0, double.NaN), MaxMagnitudeNumber_Value_NaN(+0.0));
        Assert.Equal(MaxMagnitudeNumber(-0.0, double.NaN), MaxMagnitudeNumber_Value_NaN(-0.0));

        // Single

        Assert.Equal(MaxMagnitudeNumber(float.NaN, +1.0f), MaxMagnitudeNumber_Value_One(float.NaN));
        Assert.Equal(MaxMagnitudeNumber(float.NaN, +0.0f), MaxMagnitudeNumber_Value_Zero(float.NaN));
        Assert.Equal(MaxMagnitudeNumber(float.NaN, -0.0f), MaxMagnitudeNumber_Value_NegZero(float.NaN));

        Assert.Equal(MaxMagnitudeNumber(+1.0f, float.NaN), MaxMagnitudeNumber_Value_NaN(+1.0f));
        Assert.Equal(MaxMagnitudeNumber(+0.0f, float.NaN), MaxMagnitudeNumber_Value_NaN(+0.0f));
        Assert.Equal(MaxMagnitudeNumber(-0.0f, float.NaN), MaxMagnitudeNumber_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMaxNumber()
    {
        // Double

        Assert.Equal(MaxNumber(double.NaN, +1.0), MaxNumber_Value_One(double.NaN));
        Assert.Equal(MaxNumber(double.NaN, +0.0), MaxNumber_Value_Zero(double.NaN));
        Assert.Equal(MaxNumber(double.NaN, -0.0), MaxNumber_Value_NegZero(double.NaN));

        Assert.Equal(MaxNumber(+1.0, double.NaN), MaxNumber_Value_NaN(+1.0));
        Assert.Equal(MaxNumber(+0.0, double.NaN), MaxNumber_Value_NaN(+0.0));
        Assert.Equal(MaxNumber(-0.0, double.NaN), MaxNumber_Value_NaN(-0.0));

        // Single

        Assert.Equal(MaxNumber(float.NaN, +1.0f), MaxNumber_Value_One(float.NaN));
        Assert.Equal(MaxNumber(float.NaN, +0.0f), MaxNumber_Value_Zero(float.NaN));
        Assert.Equal(MaxNumber(float.NaN, -0.0f), MaxNumber_Value_NegZero(float.NaN));

        Assert.Equal(MaxNumber(+1.0f, float.NaN), MaxNumber_Value_NaN(+1.0f));
        Assert.Equal(MaxNumber(+0.0f, float.NaN), MaxNumber_Value_NaN(+0.0f));
        Assert.Equal(MaxNumber(-0.0f, float.NaN), MaxNumber_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMin()
    {
        // Double

        Assert.Equal(Min(double.NaN, +1.0), Min_Value_One(double.NaN));
        Assert.Equal(Min(double.NaN, +0.0), Min_Value_Zero(double.NaN));
        Assert.Equal(Min(double.NaN, -0.0), Min_Value_NegZero(double.NaN));

        Assert.Equal(Min(+1.0, double.NaN), Min_Value_NaN(+1.0));
        Assert.Equal(Min(+0.0, double.NaN), Min_Value_NaN(+0.0));
        Assert.Equal(Min(-0.0, double.NaN), Min_Value_NaN(-0.0));

        // Single

        Assert.Equal(Min(float.NaN, +1.0f), Min_Value_One(float.NaN));
        Assert.Equal(Min(float.NaN, +0.0f), Min_Value_Zero(float.NaN));
        Assert.Equal(Min(float.NaN, -0.0f), Min_Value_NegZero(float.NaN));

        Assert.Equal(Min(+1.0f, float.NaN), Min_Value_NaN(+1.0f));
        Assert.Equal(Min(+0.0f, float.NaN), Min_Value_NaN(+0.0f));
        Assert.Equal(Min(-0.0f, float.NaN), Min_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMinMagnitude()
    {
        // Double

        Assert.Equal(MinMagnitude(double.NaN, +1.0), MinMagnitude_Value_One(double.NaN));
        Assert.Equal(MinMagnitude(double.NaN, +0.0), MinMagnitude_Value_Zero(double.NaN));
        Assert.Equal(MinMagnitude(double.NaN, -0.0), MinMagnitude_Value_NegZero(double.NaN));

        Assert.Equal(MinMagnitude(+1.0, double.NaN), MinMagnitude_Value_NaN(+1.0));
        Assert.Equal(MinMagnitude(+0.0, double.NaN), MinMagnitude_Value_NaN(+0.0));
        Assert.Equal(MinMagnitude(-0.0, double.NaN), MinMagnitude_Value_NaN(-0.0));

        // Single

        Assert.Equal(MinMagnitude(float.NaN, +1.0f), MinMagnitude_Value_One(float.NaN));
        Assert.Equal(MinMagnitude(float.NaN, +0.0f), MinMagnitude_Value_Zero(float.NaN));
        Assert.Equal(MinMagnitude(float.NaN, -0.0f), MinMagnitude_Value_NegZero(float.NaN));

        Assert.Equal(MinMagnitude(+1.0f, float.NaN), MinMagnitude_Value_NaN(+1.0f));
        Assert.Equal(MinMagnitude(+0.0f, float.NaN), MinMagnitude_Value_NaN(+0.0f));
        Assert.Equal(MinMagnitude(-0.0f, float.NaN), MinMagnitude_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMinMagnitudeNumber()
    {
        // Double

        Assert.Equal(MinMagnitudeNumber(double.NaN, +1.0), MinMagnitudeNumber_Value_One(double.NaN));
        Assert.Equal(MinMagnitudeNumber(double.NaN, +0.0), MinMagnitudeNumber_Value_Zero(double.NaN));
        Assert.Equal(MinMagnitudeNumber(double.NaN, -0.0), MinMagnitudeNumber_Value_NegZero(double.NaN));

        Assert.Equal(MinMagnitudeNumber(+1.0, double.NaN), MinMagnitudeNumber_Value_NaN(+1.0));
        Assert.Equal(MinMagnitudeNumber(+0.0, double.NaN), MinMagnitudeNumber_Value_NaN(+0.0));
        Assert.Equal(MinMagnitudeNumber(-0.0, double.NaN), MinMagnitudeNumber_Value_NaN(-0.0));

        // Single

        Assert.Equal(MinMagnitudeNumber(float.NaN, +1.0f), MinMagnitudeNumber_Value_One(float.NaN));
        Assert.Equal(MinMagnitudeNumber(float.NaN, +0.0f), MinMagnitudeNumber_Value_Zero(float.NaN));
        Assert.Equal(MinMagnitudeNumber(float.NaN, -0.0f), MinMagnitudeNumber_Value_NegZero(float.NaN));

        Assert.Equal(MinMagnitudeNumber(+1.0f, float.NaN), MinMagnitudeNumber_Value_NaN(+1.0f));
        Assert.Equal(MinMagnitudeNumber(+0.0f, float.NaN), MinMagnitudeNumber_Value_NaN(+0.0f));
        Assert.Equal(MinMagnitudeNumber(-0.0f, float.NaN), MinMagnitudeNumber_Value_NaN(-0.0f));
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMinNumber()
    {
        // Double

        Assert.Equal(MinNumber(double.NaN, +1.0), MinNumber_Value_One(double.NaN));
        Assert.Equal(MinNumber(double.NaN, +0.0), MinNumber_Value_Zero(double.NaN));
        Assert.Equal(MinNumber(double.NaN, -0.0), MinNumber_Value_NegZero(double.NaN));

        Assert.Equal(MinNumber(+1.0, double.NaN), MinNumber_Value_NaN(+1.0));
        Assert.Equal(MinNumber(+0.0, double.NaN), MinNumber_Value_NaN(+0.0));
        Assert.Equal(MinNumber(-0.0, double.NaN), MinNumber_Value_NaN(-0.0));

        // Single

        Assert.Equal(MinNumber(float.NaN, +1.0f), MinNumber_Value_One(float.NaN));
        Assert.Equal(MinNumber(float.NaN, +0.0f), MinNumber_Value_Zero(float.NaN));
        Assert.Equal(MinNumber(float.NaN, -0.0f), MinNumber_Value_NegZero(float.NaN));

        Assert.Equal(MinNumber(+1.0f, float.NaN), MinNumber_Value_NaN(+1.0f));
        Assert.Equal(MinNumber(+0.0f, float.NaN), MinNumber_Value_NaN(+0.0f));
        Assert.Equal(MinNumber(-0.0f, float.NaN), MinNumber_Value_NaN(-0.0f));
    }

    //
    // Max.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Max(double left, double right)
    {
        return double.Max(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double Max_Value_One(double value)
    {
        return double.Max(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Max_Value_NaN(double value)
    {
        return double.Max(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Max_Value_NegZero(double value)
    {
        return double.Max(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Max_Value_Zero(double value)
    {
        return double.Max(value, +0.0);
    }

    //
    // Max.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Max(float left, float right)
    {
        return float.Max(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float Max_Value_One(float value)
    {
        return float.Max(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Max_Value_NaN(float value)
    {
        return float.Max(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Max_Value_NegZero(float value)
    {
        return float.Max(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Max_Value_Zero(float value)
    {
        return float.Max(value, +0.0f);
    }

    //
    // MaxMagnitude.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitude(double left, double right)
    {
        return double.MaxMagnitude(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MaxMagnitude_Value_One(double value)
    {
        return double.MaxMagnitude(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitude_Value_NaN(double value)
    {
        return double.MaxMagnitude(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitude_Value_NegZero(double value)
    {
        return double.MaxMagnitude(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitude_Value_Zero(double value)
    {
        return double.MaxMagnitude(value, +0.0);
    }

    //
    // MaxMagnitude.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitude(float left, float right)
    {
        return float.MaxMagnitude(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MaxMagnitude_Value_One(float value)
    {
        return float.MaxMagnitude(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitude_Value_NaN(float value)
    {
        return float.MaxMagnitude(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitude_Value_NegZero(float value)
    {
        return float.MaxMagnitude(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitude_Value_Zero(float value)
    {
        return float.MaxMagnitude(value, +0.0f);
    }

    //
    // MaxMagnitudeNumber.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitudeNumber(double left, double right)
    {
        return double.MaxMagnitudeNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MaxMagnitudeNumber_Value_One(double value)
    {
        return double.MaxMagnitudeNumber(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitudeNumber_Value_NaN(double value)
    {
        return double.MaxMagnitudeNumber(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitudeNumber_Value_NegZero(double value)
    {
        return double.MaxMagnitudeNumber(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxMagnitudeNumber_Value_Zero(double value)
    {
        return double.MaxMagnitudeNumber(value, +0.0);
    }

    //
    // MaxMagnitudeNumber.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitudeNumber(float left, float right)
    {
        return float.MaxMagnitudeNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MaxMagnitudeNumber_Value_One(float value)
    {
        return float.MaxMagnitudeNumber(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitudeNumber_Value_NaN(float value)
    {
        return float.MaxMagnitudeNumber(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitudeNumber_Value_NegZero(float value)
    {
        return float.MaxMagnitudeNumber(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxMagnitudeNumber_Value_Zero(float value)
    {
        return float.MaxMagnitudeNumber(value, +0.0f);
    }

    //
    // MaxNumber.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxNumber(double left, double right)
    {
        return double.MaxNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MaxNumber_Value_One(double value)
    {
        return double.MaxNumber(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxNumber_Value_NaN(double value)
    {
        return double.MaxNumber(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxNumber_Value_NegZero(double value)
    {
        return double.MaxNumber(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MaxNumber_Value_Zero(double value)
    {
        return double.MaxNumber(value, +0.0);
    }

    //
    // MaxNumber.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxNumber(float left, float right)
    {
        return float.MaxNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MaxNumber_Value_One(float value)
    {
        return float.MaxNumber(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxNumber_Value_NaN(float value)
    {
        return float.MaxNumber(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxNumber_Value_NegZero(float value)
    {
        return float.MaxNumber(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MaxNumber_Value_Zero(float value)
    {
        return float.MaxNumber(value, +0.0f);
    }

    //
    // Min.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Min(double left, double right)
    {
        return double.Min(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double Min_Value_One(double value)
    {
        return double.Min(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Min_Value_NaN(double value)
    {
        return double.Min(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Min_Value_NegZero(double value)
    {
        return double.Min(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double Min_Value_Zero(double value)
    {
        return double.Min(value, +0.0);
    }

    //
    // Min.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Min(float left, float right)
    {
        return float.Min(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float Min_Value_One(float value)
    {
        return float.Min(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Min_Value_NaN(float value)
    {
        return float.Min(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Min_Value_NegZero(float value)
    {
        return float.Min(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Min_Value_Zero(float value)
    {
        return float.Min(value, +0.0f);
    }

    //
    // MinMagnitude.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitude(double left, double right)
    {
        return double.MinMagnitude(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MinMagnitude_Value_One(double value)
    {
        return double.MinMagnitude(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitude_Value_NaN(double value)
    {
        return double.MinMagnitude(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitude_Value_NegZero(double value)
    {
        return double.MinMagnitude(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitude_Value_Zero(double value)
    {
        return double.MinMagnitude(value, +0.0);
    }

    //
    // MinMagnitude.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitude(float left, float right)
    {
        return float.MinMagnitude(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MinMagnitude_Value_One(float value)
    {
        return float.MinMagnitude(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitude_Value_NaN(float value)
    {
        return float.MinMagnitude(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitude_Value_NegZero(float value)
    {
        return float.MinMagnitude(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitude_Value_Zero(float value)
    {
        return float.MinMagnitude(value, +0.0f);
    }

    //
    // MinMagnitudeNumber.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitudeNumber(double left, double right)
    {
        return double.MinMagnitudeNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MinMagnitudeNumber_Value_One(double value)
    {
        return double.MinMagnitudeNumber(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitudeNumber_Value_NaN(double value)
    {
        return double.MinMagnitudeNumber(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitudeNumber_Value_NegZero(double value)
    {
        return double.MinMagnitudeNumber(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinMagnitudeNumber_Value_Zero(double value)
    {
        return double.MinMagnitudeNumber(value, +0.0);
    }

    //
    // MinMagnitudeNumber.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitudeNumber(float left, float right)
    {
        return float.MinMagnitudeNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MinMagnitudeNumber_Value_One(float value)
    {
        return float.MinMagnitudeNumber(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitudeNumber_Value_NaN(float value)
    {
        return float.MinMagnitudeNumber(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitudeNumber_Value_NegZero(float value)
    {
        return float.MinMagnitudeNumber(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinMagnitudeNumber_Value_Zero(float value)
    {
        return float.MinMagnitudeNumber(value, +0.0f);
    }

    //
    // MinNumber.Double
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinNumber(double left, double right)
    {
        return double.MinNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static double MinNumber_Value_One(double value)
    {
        return double.MinNumber(value, +1.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinNumber_Value_NaN(double value)
    {
        return double.MinNumber(value, double.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinNumber_Value_NegZero(double value)
    {
        return double.MinNumber(value, -0.0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static double MinNumber_Value_Zero(double value)
    {
        return double.MinNumber(value, +0.0);
    }

    //
    // MinNumber.Single
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinNumber(float left, float right)
    {
        return float.MinNumber(left, right);
    }

    [MethodImpl(MethodImplOptions.NoInlining| MethodImplOptions.AggressiveOptimization)]
    public static float MinNumber_Value_One(float value)
    {
        return float.MinNumber(value, +1.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinNumber_Value_NaN(float value)
    {
        return float.MinNumber(value, float.NaN);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinNumber_Value_NegZero(float value)
    {
        return float.MinNumber(value, -0.0f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float MinNumber_Value_Zero(float value)
    {
        return float.MinNumber(value, +0.0f);
    }
}
