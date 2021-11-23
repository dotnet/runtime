// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float> GetPoint2F(float e00, float e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2FOut(float e00, float e01, Point2<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2FOut(float e00, float e01, out Point2<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float>* GetPoint2FPtr(float e00, float e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2FPtr")]
    public static extern ref readonly Point2<float> GetPoint2FRef(float e00, float e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float> AddPoint2F(Point2<float> lhs, Point2<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float> AddPoint2Fs(Point2<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float> AddPoint2Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<float> AddPoint2Fs(in Point2<float> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint2F()
    {
        GenericsNative.Point2<float> value = GenericsNative.GetPoint2F(1.0f, 2.0f);
        Assert.Equal(value.e00, 1.0f);
        Assert.Equal(value.e01, 2.0f);

        GenericsNative.Point2<float> value2;
        GenericsNative.GetPoint2FOut(1.0f, 2.0f, &value2);
        Assert.Equal(value2.e00, 1.0f);
        Assert.Equal(value2.e01, 2.0f);

        GenericsNative.GetPoint2FOut(1.0f, 2.0f, out GenericsNative.Point2<float> value3);
        Assert.Equal(value3.e00, 1.0f);
        Assert.Equal(value3.e01, 2.0f);

        GenericsNative.Point2<float>* value4 = GenericsNative.GetPoint2FPtr(1.0f, 2.0f);
        Assert.Equal(value4->e00, 1.0f);
        Assert.Equal(value4->e01, 2.0f);

        ref readonly GenericsNative.Point2<float> value5 = ref GenericsNative.GetPoint2FRef(1.0f, 2.0f);
        Assert.Equal(value5.e00, 1.0f);
        Assert.Equal(value5.e01, 2.0f);

        GenericsNative.Point2<float> result = GenericsNative.AddPoint2F(value, value);
        Assert.Equal(result.e00, 2.0f);
        Assert.Equal(result.e01, 4.0f);

        GenericsNative.Point2<float>[] values = new GenericsNative.Point2<float>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point2<float>* pValues = &values[0])
        {
            GenericsNative.Point2<float> result2 = GenericsNative.AddPoint2Fs(pValues, values.Length);
            Assert.Equal(result2.e00, 5.0f);
            Assert.Equal(result2.e01, 10.0f);
        }

        GenericsNative.Point2<float> result3 = GenericsNative.AddPoint2Fs(values, values.Length);
        Assert.Equal(result3.e00, 5.0f);
        Assert.Equal(result3.e01, 10.0f);

        GenericsNative.Point2<float> result4 = GenericsNative.AddPoint2Fs(in values[0], values.Length);
        Assert.Equal(result4.e00, 5.0f);
        Assert.Equal(result4.e01, 10.0f);
    }
}
