// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float> GetPoint1F(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1FOut(float e00, Point1<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1FOut(float e00, out Point1<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float>* GetPoint1FPtr(float e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1FPtr")]
    public static extern ref readonly Point1<float> GetPoint1FRef(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float> AddPoint1F(Point1<float> lhs, Point1<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float> AddPoint1Fs(Point1<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float> AddPoint1Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<float> AddPoint1Fs(in Point1<float> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint1F()
    {
        GenericsNative.Point1<float> value = GenericsNative.GetPoint1F(1.0f);
        Assert.AreEqual(value.e00, 1.0f);

        GenericsNative.Point1<float> value2;
        GenericsNative.GetPoint1FOut(1.0f, &value2);
        Assert.AreEqual(value2.e00, 1.0f);

        GenericsNative.GetPoint1FOut(1.0f, out GenericsNative.Point1<float> value3);
        Assert.AreEqual(value3.e00, 1.0f);

        GenericsNative.Point1<float>* value4 = GenericsNative.GetPoint1FPtr(1.0f);
        Assert.AreEqual(value4->e00, 1.0f);

        ref readonly GenericsNative.Point1<float> value5 = ref GenericsNative.GetPoint1FRef(1.0f);
        Assert.AreEqual(value5.e00, 1.0f);

        GenericsNative.Point1<float> result = GenericsNative.AddPoint1F(value, value);
        Assert.AreEqual(result.e00, 2.0f);

        GenericsNative.Point1<float>[] values = new GenericsNative.Point1<float>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point1<float>* pValues = &values[0])
        {
            GenericsNative.Point1<float> result2 = GenericsNative.AddPoint1Fs(pValues, values.Length);
            Assert.AreEqual(result2.e00, 5.0f);
        }

        GenericsNative.Point1<float> result3 = GenericsNative.AddPoint1Fs(values, values.Length);
        Assert.AreEqual(result3.e00, 5.0f);

        GenericsNative.Point1<float> result4 = GenericsNative.AddPoint1Fs(in values[0], values.Length);
        Assert.AreEqual(result4.e00, 5.0f);
    }
}
