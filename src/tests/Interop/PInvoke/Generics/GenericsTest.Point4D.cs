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

unsafe partial class GenericsTest
{
    private static void TestPoint4D()
    {
        GenericsNative.Point4<double> value = GenericsNative.GetPoint4D(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(value.e00, 1.0);
        Assert.Equal(value.e01, 2.0);
        Assert.Equal(value.e02, 3.0);
        Assert.Equal(value.e03, 4.0);

        GenericsNative.Point4<double> value2;
        GenericsNative.GetPoint4DOut(1.0, 2.0, 3.0, 4.0, &value2);
        Assert.Equal(value2.e00, 1.0);
        Assert.Equal(value2.e01, 2.0);
        Assert.Equal(value2.e02, 3.0);
        Assert.Equal(value2.e03, 4.0);

        GenericsNative.GetPoint4DOut(1.0, 2.0, 3.0, 4.0, out GenericsNative.Point4<double> value3);
        Assert.Equal(value3.e00, 1.0);
        Assert.Equal(value3.e01, 2.0);
        Assert.Equal(value3.e02, 3.0);
        Assert.Equal(value3.e03, 4.0);

        GenericsNative.Point4<double>* value4 = GenericsNative.GetPoint4DPtr(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(value4->e00, 1.0);
        Assert.Equal(value4->e01, 2.0);
        Assert.Equal(value4->e02, 3.0);
        Assert.Equal(value4->e03, 4.0);

        ref readonly GenericsNative.Point4<double> value5 = ref GenericsNative.GetPoint4DRef(1.0, 2.0, 3.0, 4.0);
        Assert.Equal(value5.e00, 1.0);
        Assert.Equal(value5.e01, 2.0);
        Assert.Equal(value5.e02, 3.0);
        Assert.Equal(value5.e03, 4.0);

        GenericsNative.Point4<double> result = GenericsNative.AddPoint4D(value, value);
        Assert.Equal(result.e00, 2.0);
        Assert.Equal(result.e01, 4.0);
        Assert.Equal(result.e02, 6.0);
        Assert.Equal(result.e03, 8.0);

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
            Assert.Equal(result2.e00, 5.0);
            Assert.Equal(result2.e01, 10.0);
            Assert.Equal(result2.e02, 15.0);
            Assert.Equal(result2.e03, 20.0);
        }

        GenericsNative.Point4<double> result3 = GenericsNative.AddPoint4Ds(values, values.Length);
        Assert.Equal(result3.e00, 5.0);
        Assert.Equal(result3.e01, 10.0);
        Assert.Equal(result3.e02, 15.0);
        Assert.Equal(result3.e03, 20.0);

        GenericsNative.Point4<double> result4 = GenericsNative.AddPoint4Ds(in values[0], values.Length);
        Assert.Equal(result4.e00, 5.0);
        Assert.Equal(result4.e01, 10.0);
        Assert.Equal(result4.e02, 15.0);
        Assert.Equal(result4.e03, 20.0);
    }
}
