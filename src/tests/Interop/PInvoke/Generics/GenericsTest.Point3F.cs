// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

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

unsafe partial class GenericsTest
{
    private static void TestPoint3F()
    {
        GenericsNative.Point3<float> value = GenericsNative.GetPoint3F(1.0f, 2.0f, 3.0f);
        Assert.AreEqual(value.e00, 1.0f);
        Assert.AreEqual(value.e01, 2.0f);
        Assert.AreEqual(value.e02, 3.0f);

        GenericsNative.Point3<float> value2;
        GenericsNative.GetPoint3FOut(1.0f, 2.0f, 3.0f, &value2);
        Assert.AreEqual(value2.e00, 1.0f);
        Assert.AreEqual(value2.e01, 2.0f);
        Assert.AreEqual(value2.e02, 3.0f);

        GenericsNative.GetPoint3FOut(1.0f, 2.0f, 3.0f, out GenericsNative.Point3<float> value3);
        Assert.AreEqual(value3.e00, 1.0f);
        Assert.AreEqual(value3.e01, 2.0f);
        Assert.AreEqual(value3.e02, 3.0f);

        GenericsNative.Point3<float>* value4 = GenericsNative.GetPoint3FPtr(1.0f, 2.0f, 3.0f);
        Assert.AreEqual(value4->e00, 1.0f);
        Assert.AreEqual(value4->e01, 2.0f);
        Assert.AreEqual(value4->e02, 3.0f);

        ref readonly GenericsNative.Point3<float> value5 = ref GenericsNative.GetPoint3FRef(1.0f, 2.0f, 3.0f);
        Assert.AreEqual(value5.e00, 1.0f);
        Assert.AreEqual(value5.e01, 2.0f);
        Assert.AreEqual(value5.e02, 3.0f);

        GenericsNative.Point3<float> result = GenericsNative.AddPoint3F(value, value);
        Assert.AreEqual(result.e00, 2.0f);
        Assert.AreEqual(result.e01, 4.0f);
        Assert.AreEqual(result.e02, 6.0f);

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
            Assert.AreEqual(result2.e00, 5.0f);
            Assert.AreEqual(result2.e01, 10.0f);
            Assert.AreEqual(result2.e02, 15.0f);
        }

        GenericsNative.Point3<float> result3 = GenericsNative.AddPoint3Fs(values, values.Length);
        Assert.AreEqual(result3.e00, 5.0f);
        Assert.AreEqual(result3.e01, 10.0f);
        Assert.AreEqual(result3.e02, 15.0f);

        GenericsNative.Point3<float> result4 = GenericsNative.AddPoint3Fs(in values[0], values.Length);
        Assert.AreEqual(result4.e00, 5.0f);
        Assert.AreEqual(result4.e01, 10.0f);
        Assert.AreEqual(result4.e02, 15.0f);
    }
}
