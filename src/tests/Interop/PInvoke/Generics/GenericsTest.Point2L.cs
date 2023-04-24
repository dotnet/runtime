// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long> GetPoint2L(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2LOut(long e00, long e01, Point2<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2LOut(long e00, long e01, out Point2<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long>* GetPoint2LPtr(long e00, long e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2LPtr")]
    public static extern ref readonly Point2<long> GetPoint2LRef(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long> AddPoint2L(Point2<long> lhs, Point2<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long> AddPoint2Ls(Point2<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long> AddPoint2Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<long> AddPoint2Ls(in Point2<long> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint2L()
    {
        GenericsNative.Point2<long> value = GenericsNative.GetPoint2L(1L, 2L);
        Assert.Equal(value.e00, 1L);
        Assert.Equal(value.e01, 2L);

        GenericsNative.Point2<long> value2;
        GenericsNative.GetPoint2LOut(1L, 2L, &value2);
        Assert.Equal(value2.e00, 1L);
        Assert.Equal(value2.e01, 2L);

        GenericsNative.GetPoint2LOut(1L, 2L, out GenericsNative.Point2<long> value3);
        Assert.Equal(value3.e00, 1L);
        Assert.Equal(value3.e01, 2L);

        GenericsNative.Point2<long>* value4 = GenericsNative.GetPoint2LPtr(1L, 2L);
        Assert.Equal(value4->e00, 1L);
        Assert.Equal(value4->e01, 2L);

        ref readonly GenericsNative.Point2<long> value5 = ref GenericsNative.GetPoint2LRef(1L, 2L);
        Assert.Equal(value5.e00, 1L);
        Assert.Equal(value5.e01, 2L);

        GenericsNative.Point2<long> result = GenericsNative.AddPoint2L(value, value);
        Assert.Equal(result.e00, 2L);
        Assert.Equal(result.e01, 4L);

        GenericsNative.Point2<long>[] values = new GenericsNative.Point2<long>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point2<long>* pValues = &values[0])
        {
            GenericsNative.Point2<long> result2 = GenericsNative.AddPoint2Ls(pValues, values.Length);
            Assert.Equal(result2.e00, 5l);
            Assert.Equal(result2.e01, 10l);
        }

        GenericsNative.Point2<long> result3 = GenericsNative.AddPoint2Ls(values, values.Length);
        Assert.Equal(result3.e00, 5l);
        Assert.Equal(result3.e01, 10l);

        GenericsNative.Point2<long> result4 = GenericsNative.AddPoint2Ls(in values[0], values.Length);
        Assert.Equal(result4.e00, 5l);
        Assert.Equal(result4.e01, 10l);
    }
}
