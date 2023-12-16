// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double> GetPoint4D(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4DOut(double e00, double e01, double e02, double e03, Point4<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4DOut(double e00, double e01, double e02, double e03, out Point4<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double>* GetPoint4DPtr(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4DPtr")]
    public static extern ref readonly Point4<double> GetPoint4DRef(double e00, double e01, double e02, double e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double> AddPoint4D(Point4<double> lhs, Point4<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double> AddPoint4Ds(Point4<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double> AddPoint4Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<double> AddPoint4Ds(in Point4<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint4D()
    {
        GenericsNative.Point4<double> value = GenericsNative.GetPoint4D(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, value.e00);
        Assert.Equal(2.0, value.e01);
        Assert.Equal(3.0, value.e02);
        Assert.Equal(4.0, value.e03);

        GenericsNative.Point4<double> value2;
        GenericsNative.GetPoint4DOut(1.0, 2.0, 3.0, 4.0, &value2);
        Assert.Equal(1.0, value2.e00);
        Assert.Equal(2.0, value2.e01);
        Assert.Equal(3.0, value2.e02);
        Assert.Equal(4.0, value2.e03);

        GenericsNative.GetPoint4DOut(1.0, 2.0, 3.0, 4.0, out GenericsNative.Point4<double> value3);
        Assert.Equal(1.0, value3.e00);
        Assert.Equal(2.0, value3.e01);
        Assert.Equal(3.0, value3.e02);
        Assert.Equal(4.0, value3.e03);

        GenericsNative.Point4<double>* value4 = GenericsNative.GetPoint4DPtr(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, value4->e00);
        Assert.Equal(2.0, value4->e01);
        Assert.Equal(3.0, value4->e02);
        Assert.Equal(4.0, value4->e03);

        ref readonly GenericsNative.Point4<double> value5 = ref GenericsNative.GetPoint4DRef(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(1.0, value5.e00);
        Assert.Equal(2.0, value5.e01);
        Assert.Equal(3.0, value5.e02);
        Assert.Equal(4.0, value5.e03);

        GenericsNative.Point4<double> result = GenericsNative.AddPoint4D(value, value);
        Assert.Equal(2.0, result.e00);
        Assert.Equal(4.0, result.e01);
        Assert.Equal(6.0, result.e02);
        Assert.Equal(8.0, result.e03);

        GenericsNative.Point4<double>[] values = new GenericsNative.Point4<double>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point4<double>* pValues = &values[0])
        {
            GenericsNative.Point4<double> result2 = GenericsNative.AddPoint4Ds(pValues, values.Length);
            Assert.Equal(5.0, result2.e00);
            Assert.Equal(10.0, result2.e01);
            Assert.Equal(15.0, result2.e02);
            Assert.Equal(20.0, result2.e03);
        }

        GenericsNative.Point4<double> result3 = GenericsNative.AddPoint4Ds(values, values.Length);
        Assert.Equal(5.0, result3.e00);
        Assert.Equal(10.0, result3.e01);
        Assert.Equal(15.0, result3.e02);
        Assert.Equal(20.0, result3.e03);

        GenericsNative.Point4<double> result4 = GenericsNative.AddPoint4Ds(in values[0], values.Length);
        Assert.Equal(5.0, result4.e00);
        Assert.Equal(10.0, result4.e01);
        Assert.Equal(15.0, result4.e02);
        Assert.Equal(20.0, result4.e03);
    }
}
