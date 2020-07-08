// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint> GetPoint3U(uint e00, uint e01, uint e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3UOut(uint e00, uint e01, uint e02, Point3<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3UOut(uint e00, uint e01, uint e02, out Point3<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint>* GetPoint3UPtr(uint e00, uint e01, uint e02);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3UPtr")]
    public static extern ref readonly Point3<uint> GetPoint3URef(uint e00, uint e01, uint e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint> AddPoint3U(Point3<uint> lhs, Point3<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint> AddPoint3Us(Point3<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint> AddPoint3Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<uint> AddPoint3Us(in Point3<uint> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint3U()
    {
        GenericsNative.Point3<uint> value = GenericsNative.GetPoint3U(1u, 2u, 3u);
        Assert.AreEqual(value.e00, 1u);
        Assert.AreEqual(value.e01, 2u);
        Assert.AreEqual(value.e02, 3u);

        GenericsNative.Point3<uint> value2;
        GenericsNative.GetPoint3UOut(1u, 2u, 3u, &value2);
        Assert.AreEqual(value2.e00, 1u);
        Assert.AreEqual(value2.e01, 2u);
        Assert.AreEqual(value2.e02, 3u);

        GenericsNative.GetPoint3UOut(1u, 2u, 3u, out GenericsNative.Point3<uint> value3);
        Assert.AreEqual(value3.e00, 1u);
        Assert.AreEqual(value3.e01, 2u);
        Assert.AreEqual(value3.e02, 3u);

        GenericsNative.Point3<uint>* value4 = GenericsNative.GetPoint3UPtr(1u, 2u, 3u);
        Assert.AreEqual(value4->e00, 1u);
        Assert.AreEqual(value4->e01, 2u);
        Assert.AreEqual(value4->e02, 3u);

        ref readonly GenericsNative.Point3<uint> value5 = ref GenericsNative.GetPoint3URef(1u, 2u, 3u);
        Assert.AreEqual(value5.e00, 1u);
        Assert.AreEqual(value5.e01, 2u);
        Assert.AreEqual(value5.e02, 3u);

        GenericsNative.Point3<uint> result = GenericsNative.AddPoint3U(value, value);
        Assert.AreEqual(result.e00, 2u);
        Assert.AreEqual(result.e01, 4u);
        Assert.AreEqual(result.e02, 6u);

        GenericsNative.Point3<uint>[] values = new GenericsNative.Point3<uint>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point3<uint>* pValues = &values[0])
        {
            GenericsNative.Point3<uint> result2 = GenericsNative.AddPoint3Us(pValues, values.Length);
            Assert.AreEqual(result2.e00, 5u);
            Assert.AreEqual(result2.e01, 10u);
            Assert.AreEqual(result2.e02, 15u);
        }

        GenericsNative.Point3<uint> result3 = GenericsNative.AddPoint3Us(values, values.Length);
        Assert.AreEqual(result3.e00, 5u);
        Assert.AreEqual(result3.e01, 10u);
        Assert.AreEqual(result3.e02, 15u);

        GenericsNative.Point3<uint> result4 = GenericsNative.AddPoint3Us(in values[0], values.Length);
        Assert.AreEqual(result4.e00, 5u);
        Assert.AreEqual(result4.e01, 10u);
        Assert.AreEqual(result4.e02, 15u);
    }
}
