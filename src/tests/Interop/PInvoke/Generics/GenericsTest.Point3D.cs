// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

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

unsafe partial class GenericsTest
{
    private static void TestPoint3D()
    {
        GenericsNative.Point3<double> value = GenericsNative.GetPoint3D(1.0, 2.0, 3.0);
        Assert.AreEqual(value.e00, 1.0);
        Assert.AreEqual(value.e01, 2.0);
        Assert.AreEqual(value.e02, 3.0);

        GenericsNative.Point3<double> value2;
        GenericsNative.GetPoint3DOut(1.0, 2.0, 3.0, &value2);
        Assert.AreEqual(value2.e00, 1.0);
        Assert.AreEqual(value2.e01, 2.0);
        Assert.AreEqual(value2.e02, 3.0);

        GenericsNative.GetPoint3DOut(1.0, 2.0, 3.0, out GenericsNative.Point3<double> value3);
        Assert.AreEqual(value3.e00, 1.0);
        Assert.AreEqual(value3.e01, 2.0);
        Assert.AreEqual(value3.e02, 3.0);

        GenericsNative.Point3<double>* value4 = GenericsNative.GetPoint3DPtr(1.0, 2.0, 3.0);
        Assert.AreEqual(value4->e00, 1.0);
        Assert.AreEqual(value4->e01, 2.0);
        Assert.AreEqual(value4->e02, 3.0);

        ref readonly GenericsNative.Point3<double> value5 = ref GenericsNative.GetPoint3DRef(1.0, 2.0, 3.0);
        Assert.AreEqual(value5.e00, 1.0);
        Assert.AreEqual(value5.e01, 2.0);
        Assert.AreEqual(value5.e02, 3.0);

        GenericsNative.Point3<double> result = GenericsNative.AddPoint3D(value, value);
        Assert.AreEqual(result.e00, 2.0);
        Assert.AreEqual(result.e01, 4.0);
        Assert.AreEqual(result.e02, 6.0);

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
            Assert.AreEqual(result2.e00, 5.0);
            Assert.AreEqual(result2.e01, 10.0);
            Assert.AreEqual(result2.e02, 15.0);
        }

        GenericsNative.Point3<double> result3 = GenericsNative.AddPoint3Ds(values, values.Length);
        Assert.AreEqual(result3.e00, 5.0);
        Assert.AreEqual(result3.e01, 10.0);
        Assert.AreEqual(result3.e02, 15.0);

        GenericsNative.Point3<double> result4 = GenericsNative.AddPoint3Ds(in values[0], values.Length);
        Assert.AreEqual(result4.e00, 5.0);
        Assert.AreEqual(result4.e01, 10.0);
        Assert.AreEqual(result4.e02, 15.0);
    }
}
