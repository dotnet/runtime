// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long> GetPoint3L(long e00, long e01, long e02);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3LOut(long e00, long e01, long e02, Point3<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint3LOut(long e00, long e01, long e02, out Point3<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long>* GetPoint3LPtr(long e00, long e01, long e02);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint3LPtr")]
    public static extern ref readonly Point3<long> GetPoint3LRef(long e00, long e01, long e02);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long> AddPoint3L(Point3<long> lhs, Point3<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long> AddPoint3Ls(Point3<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long> AddPoint3Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point3<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point3<long> AddPoint3Ls(in Point3<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint3L()
    {
        GenericsNative.Point3<long> value = GenericsNative.GetPoint3L(1L, 2L, 3L);
        Assert.Equal(1L, value.e00);
        Assert.Equal(2L, value.e01);
        Assert.Equal(3L, value.e02);

        GenericsNative.Point3<long> value2;
        GenericsNative.GetPoint3LOut(1L, 2L, 3L, &value2);
        Assert.Equal(1L, value2.e00);
        Assert.Equal(2L, value2.e01);
        Assert.Equal(3L, value2.e02);

        GenericsNative.GetPoint3LOut(1L, 2L, 3L, out GenericsNative.Point3<long> value3);
        Assert.Equal(1L, value3.e00);
        Assert.Equal(2L, value3.e01);
        Assert.Equal(3L, value3.e02);

        GenericsNative.Point3<long>* value4 = GenericsNative.GetPoint3LPtr(1L, 2L, 3L);
        Assert.Equal(1L, value4->e00);
        Assert.Equal(2L, value4->e01);
        Assert.Equal(3L, value4->e02);

        ref readonly GenericsNative.Point3<long> value5 = ref GenericsNative.GetPoint3LRef(1L, 2L, 3L);
        Assert.Equal(1L, value5.e00);
        Assert.Equal(2L, value5.e01);
        Assert.Equal(3L, value5.e02);

        GenericsNative.Point3<long> result = GenericsNative.AddPoint3L(value, value);
        Assert.Equal(2L, result.e00);
        Assert.Equal(4L, result.e01);
        Assert.Equal(6l, result.e02);

        GenericsNative.Point3<long>[] values = new GenericsNative.Point3<long>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point3<long>* pValues = &values[0])
        {
            GenericsNative.Point3<long> result2 = GenericsNative.AddPoint3Ls(pValues, values.Length);
            Assert.Equal(5l, result2.e00);
            Assert.Equal(10l, result2.e01);
            Assert.Equal(15l, result2.e02);
        }

        GenericsNative.Point3<long> result3 = GenericsNative.AddPoint3Ls(values, values.Length);
        Assert.Equal(5l, result3.e00);
        Assert.Equal(10l, result3.e01);
        Assert.Equal(15l, result3.e02);

        GenericsNative.Point3<long> result4 = GenericsNative.AddPoint3Ls(in values[0], values.Length);
        Assert.Equal(5l, result4.e00);
        Assert.Equal(10l, result4.e01);
        Assert.Equal(15l, result4.e02);
    }
}
