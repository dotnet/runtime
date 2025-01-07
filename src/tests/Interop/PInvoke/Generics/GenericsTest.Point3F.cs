// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float> GetPoint3F(float e00, float e01, float e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3FOut(float e00, float e01, float e02, Point3<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3FOut(float e00, float e01, float e02, out Point3<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float>* GetPoint3FPtr(float e00, float e01, float e02);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3FPtr")]
    public static extern ref readonly Point3<float> GetPoint3FRef(float e00, float e01, float e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float> AddPoint3F(Point3<float> lhs, Point3<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float> AddPoint3Fs(Point3<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float> AddPoint3Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<float> AddPoint3Fs(in Point3<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint3F()
    {
        GenericsNative.Point3<float> value = GenericsNative.GetPoint3F(1.0f, 2.0f, 3.0f);
        Assert.Equal(1.0f, value.e00);
        Assert.Equal(2.0f, value.e01);
        Assert.Equal(3.0f, value.e02);

        GenericsNative.Point3<float> value2;
        GenericsNative.GetPoint3FOut(1.0f, 2.0f, 3.0f, &value2);
        Assert.Equal(1.0f, value2.e00);
        Assert.Equal(2.0f, value2.e01);
        Assert.Equal(3.0f, value2.e02);

        GenericsNative.GetPoint3FOut(1.0f, 2.0f, 3.0f, out GenericsNative.Point3<float> value3);
        Assert.Equal(1.0f, value3.e00);
        Assert.Equal(2.0f, value3.e01);
        Assert.Equal(3.0f, value3.e02);

        GenericsNative.Point3<float>* value4 = GenericsNative.GetPoint3FPtr(1.0f, 2.0f, 3.0f);
        Assert.Equal(1.0f, value4->e00);
        Assert.Equal(2.0f, value4->e01);
        Assert.Equal(3.0f, value4->e02);

        ref readonly GenericsNative.Point3<float> value5 = ref GenericsNative.GetPoint3FRef(1.0f, 2.0f, 3.0f);
        Assert.Equal(1.0f, value5.e00);
        Assert.Equal(2.0f, value5.e01);
        Assert.Equal(3.0f, value5.e02);

        GenericsNative.Point3<float> result = GenericsNative.AddPoint3F(value, value);
        Assert.Equal(2.0f, result.e00);
        Assert.Equal(4.0f, result.e01);
        Assert.Equal(6.0f, result.e02);

        GenericsNative.Point3<float>[] values = new GenericsNative.Point3<float>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point3<float>* pValues = &values[0])
        {
            GenericsNative.Point3<float> result2 = GenericsNative.AddPoint3Fs(pValues, values.Length);
            Assert.Equal(5.0f, result2.e00);
            Assert.Equal(10.0f, result2.e01);
            Assert.Equal(15.0f, result2.e02);
        }

        GenericsNative.Point3<float> result3 = GenericsNative.AddPoint3Fs(values, values.Length);
        Assert.Equal(5.0f, result3.e00);
        Assert.Equal(10.0f, result3.e01);
        Assert.Equal(15.0f, result3.e02);

        GenericsNative.Point3<float> result4 = GenericsNative.AddPoint3Fs(in values[0], values.Length);
        Assert.Equal(5.0f, result4.e00);
        Assert.Equal(10.0f, result4.e01);
        Assert.Equal(15.0f, result4.e02);
    }
}
