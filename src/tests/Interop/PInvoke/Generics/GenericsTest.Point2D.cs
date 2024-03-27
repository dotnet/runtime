// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double> GetPoint2D(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2DOut(double e00, double e01, Point2<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2DOut(double e00, double e01, out Point2<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double>* GetPoint2DPtr(double e00, double e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2DPtr")]
    public static extern ref readonly Point2<double> GetPoint2DRef(double e00, double e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double> AddPoint2D(Point2<double> lhs, Point2<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double> AddPoint2Ds(Point2<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double> AddPoint2Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<double> AddPoint2Ds(in Point2<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint2D()
    {
        GenericsNative.Point2<double> value = GenericsNative.GetPoint2D(1.0, 2.0);
        Assert.Equal(1.0, value.e00);
        Assert.Equal(2.0, value.e01);

        GenericsNative.Point2<double> value2;
        GenericsNative.GetPoint2DOut(1.0, 2.0, &value2);
        Assert.Equal(1.0, value2.e00);
        Assert.Equal(2.0, value2.e01);

        GenericsNative.GetPoint2DOut(1.0, 2.0, out GenericsNative.Point2<double> value3);
        Assert.Equal(1.0, value3.e00);
        Assert.Equal(2.0, value3.e01);

        GenericsNative.Point2<double>* value4 = GenericsNative.GetPoint2DPtr(1.0, 2.0);
        Assert.Equal(1.0, value4->e00);
        Assert.Equal(2.0, value4->e01);

        ref readonly GenericsNative.Point2<double> value5 = ref GenericsNative.GetPoint2DRef(1.0, 2.0);
        Assert.Equal(1.0, value5.e00);
        Assert.Equal(2.0, value5.e01);

        GenericsNative.Point2<double> result = GenericsNative.AddPoint2D(value, value);
        Assert.Equal(2.0, result.e00);
        Assert.Equal(4.0, result.e01);

        GenericsNative.Point2<double>[] values = new GenericsNative.Point2<double>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point2<double>* pValues = &values[0])
        {
            GenericsNative.Point2<double> result2 = GenericsNative.AddPoint2Ds(pValues, values.Length);
            Assert.Equal(5.0, result2.e00);
            Assert.Equal(10.0, result2.e01);
        }

        GenericsNative.Point2<double> result3 = GenericsNative.AddPoint2Ds(values, values.Length);
        Assert.Equal(5.0, result3.e00);
        Assert.Equal(10.0, result3.e01);

        GenericsNative.Point2<double> result4 = GenericsNative.AddPoint2Ds(in values[0], values.Length);
        Assert.Equal(5.0, result4.e00);
        Assert.Equal(10.0, result4.e01);
    }
}
