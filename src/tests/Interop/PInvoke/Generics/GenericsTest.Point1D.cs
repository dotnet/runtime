// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double> GetPoint1D(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1DOut(double e00, Point1<double>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1DOut(double e00, out Point1<double> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double>* GetPoint1DPtr(double e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1DPtr")]
    public static extern ref readonly Point1<double> GetPoint1DRef(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double> AddPoint1D(Point1<double> lhs, Point1<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double> AddPoint1Ds(Point1<double>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double> AddPoint1Ds([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<double>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<double> AddPoint1Ds(in Point1<double> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint1D()
    {
        GenericsNative.Point1<double> value = GenericsNative.GetPoint1D(1.0);
        Assert.AreEqual(value.e00, 1.0);

        GenericsNative.Point1<double> value2;
        GenericsNative.GetPoint1DOut(1.0, &value2);
        Assert.AreEqual(value2.e00, 1.0);

        GenericsNative.GetPoint1DOut(1.0, out GenericsNative.Point1<double> value3);
        Assert.AreEqual(value3.e00, 1.0);

        GenericsNative.Point1<double>* value4 = GenericsNative.GetPoint1DPtr(1.0);
        Assert.AreEqual(value4->e00, 1.0);

        ref readonly GenericsNative.Point1<double> value5 = ref GenericsNative.GetPoint1DRef(1.0);
        Assert.AreEqual(value5.e00, 1.0);

        GenericsNative.Point1<double> result = GenericsNative.AddPoint1D(value, value);
        Assert.AreEqual(result.e00, 2.0);

        GenericsNative.Point1<double>[] values = new GenericsNative.Point1<double>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point1<double>* pValues = &values[0])
        {
            GenericsNative.Point1<double> result2 = GenericsNative.AddPoint1Ds(pValues, values.Length);
            Assert.AreEqual(result2.e00, 5.0);
        }

        GenericsNative.Point1<double> result3 = GenericsNative.AddPoint1Ds(values, values.Length);
        Assert.AreEqual(result3.e00, 5.0);

        GenericsNative.Point1<double> result4 = GenericsNative.AddPoint1Ds(in values[0], values.Length);
        Assert.AreEqual(result4.e00, 5.0);
    }
}
