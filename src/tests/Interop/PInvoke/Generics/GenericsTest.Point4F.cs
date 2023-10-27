// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float> GetPoint4F(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4FOut(float e00, float e01, float e02, float e03, Point4<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4FOut(float e00, float e01, float e02, float e03, out Point4<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float>* GetPoint4FPtr(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4FPtr")]
    public static extern ref readonly Point4<float> GetPoint4FRef(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float> AddPoint4F(Point4<float> lhs, Point4<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float> AddPoint4Fs(Point4<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float> AddPoint4Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<float> AddPoint4Fs(in Point4<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint4F()
    {
        GenericsNative.Point4<float> value = GenericsNative.GetPoint4F(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.Equal(1.0f, value.e00);
        Assert.Equal(2.0f, value.e01);
        Assert.Equal(3.0f, value.e02);
        Assert.Equal(4.0f, value.e03);

        GenericsNative.Point4<float> value2;
        GenericsNative.GetPoint4FOut(1.0f, 2.0f, 3.0f, 4.0f, &value2);
        Assert.Equal(1.0f, value2.e00);
        Assert.Equal(2.0f, value2.e01);
        Assert.Equal(3.0f, value2.e02);
        Assert.Equal(4.0f, value2.e03);

        GenericsNative.GetPoint4FOut(1.0f, 2.0f, 3.0f, 4.0f, out GenericsNative.Point4<float> value3);
        Assert.Equal(1.0f, value3.e00);
        Assert.Equal(2.0f, value3.e01);
        Assert.Equal(3.0f, value3.e02);
        Assert.Equal(4.0f, value3.e03);

        GenericsNative.Point4<float>* value4 = GenericsNative.GetPoint4FPtr(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.Equal(1.0f, value4->e00);
        Assert.Equal(2.0f, value4->e01);
        Assert.Equal(3.0f, value4->e02);
        Assert.Equal(4.0f, value4->e03);

        ref readonly GenericsNative.Point4<float> value5 = ref GenericsNative.GetPoint4FRef(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.Equal(1.0f, value5.e00);
        Assert.Equal(2.0f, value5.e01);
        Assert.Equal(3.0f, value5.e02);
        Assert.Equal(4.0f, value5.e03);

        GenericsNative.Point4<float> result = GenericsNative.AddPoint4F(value, value);
        Assert.Equal(2.0f, result.e00);
        Assert.Equal(4.0f, result.e01);
        Assert.Equal(6.0f, result.e02);
        Assert.Equal(8.0f, result.e03);

        GenericsNative.Point4<float>[] values = new GenericsNative.Point4<float>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point4<float>* pValues = &values[0])
        {
            GenericsNative.Point4<float> result2 = GenericsNative.AddPoint4Fs(pValues, values.Length);
            Assert.Equal(5.0f, result2.e00);
            Assert.Equal(10.0f, result2.e01);
            Assert.Equal(15.0f, result2.e02);
            Assert.Equal(20.0f, result2.e03);
        }

        GenericsNative.Point4<float> result3 = GenericsNative.AddPoint4Fs(values, values.Length);
        Assert.Equal(5.0f, result3.e00);
        Assert.Equal(10.0f, result3.e01);
        Assert.Equal(15.0f, result3.e02);
        Assert.Equal(20.0f, result3.e03);

        GenericsNative.Point4<float> result4 = GenericsNative.AddPoint4Fs(in values[0], values.Length);
        Assert.Equal(5.0f, result4.e00);
        Assert.Equal(10.0f, result4.e01);
        Assert.Equal(15.0f, result4.e02);
        Assert.Equal(20.0f, result4.e03);
    }
}
