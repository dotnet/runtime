// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double> GetPoint3D(double e00, double e01, double e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3DOut(double e00, double e01, double e02, Point3<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3DOut(double e00, double e01, double e02, out Point3<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double>* GetPoint3DPtr(double e00, double e01, double e02);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3DPtr")]
    public static extern ref readonly Point3<double> GetPoint3DRef(double e00, double e01, double e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double> AddPoint3D(Point3<double> lhs, Point3<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double> AddPoint3Ds(Point3<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double> AddPoint3Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<double> AddPoint3Ds(in Point3<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint3D()
    {
        GenericsNative.Point3<double> value = GenericsNative.GetPoint3D(1.0, 2.0, 3.0);
        Assert.Equal(1.0, value.e00);
        Assert.Equal(2.0, value.e01);
        Assert.Equal(3.0, value.e02);

        GenericsNative.Point3<double> value2;
        GenericsNative.GetPoint3DOut(1.0, 2.0, 3.0, &value2);
        Assert.Equal(1.0, value2.e00);
        Assert.Equal(2.0, value2.e01);
        Assert.Equal(3.0, value2.e02);

        GenericsNative.GetPoint3DOut(1.0, 2.0, 3.0, out GenericsNative.Point3<double> value3);
        Assert.Equal(1.0, value3.e00);
        Assert.Equal(2.0, value3.e01);
        Assert.Equal(3.0, value3.e02);

        GenericsNative.Point3<double>* value4 = GenericsNative.GetPoint3DPtr(1.0, 2.0, 3.0);
        Assert.Equal(1.0, value4->e00);
        Assert.Equal(2.0, value4->e01);
        Assert.Equal(3.0, value4->e02);

        ref readonly GenericsNative.Point3<double> value5 = ref GenericsNative.GetPoint3DRef(1.0, 2.0, 3.0);
        Assert.Equal(1.0, value5.e00);
        Assert.Equal(2.0, value5.e01);
        Assert.Equal(3.0, value5.e02);

        GenericsNative.Point3<double> result = GenericsNative.AddPoint3D(value, value);
        Assert.Equal(2.0, result.e00);
        Assert.Equal(4.0, result.e01);
        Assert.Equal(6.0, result.e02);

        GenericsNative.Point3<double>[] values = new GenericsNative.Point3<double>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point3<double>* pValues = &values[0])
        {
            GenericsNative.Point3<double> result2 = GenericsNative.AddPoint3Ds(pValues, values.Length);
            Assert.Equal(5.0, result2.e00);
            Assert.Equal(10.0, result2.e01);
            Assert.Equal(15.0, result2.e02);
        }

        GenericsNative.Point3<double> result3 = GenericsNative.AddPoint3Ds(values, values.Length);
        Assert.Equal(5.0, result3.e00);
        Assert.Equal(10.0, result3.e01);
        Assert.Equal(15.0, result3.e02);

        GenericsNative.Point3<double> result4 = GenericsNative.AddPoint3Ds(in values[0], values.Length);
        Assert.Equal(5.0, result4.e00);
        Assert.Equal(10.0, result4.e01);
        Assert.Equal(15.0, result4.e02);
    }
}
