// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long> GetPoint4L(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4LOut(long e00, long e01, long e02, long e03, Point4<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4LOut(long e00, long e01, long e02, long e03, out Point4<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long>* GetPoint4LPtr(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4LPtr")]
    public static extern ref readonly Point4<long> GetPoint4LRef(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long> AddPoint4L(Point4<long> lhs, Point4<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long> AddPoint4Ls(Point4<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long> AddPoint4Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<long> AddPoint4Ls(in Point4<long> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestPoint4L()
    {
        GenericsNative.Point4<long> value = GenericsNative.GetPoint4L(1L, 2L, 3L, 4L);
        Assert.Equal(value.e00, 1L);
        Assert.Equal(value.e01, 2L);
        Assert.Equal(value.e02, 3L);
        Assert.Equal(value.e03, 4L);

        GenericsNative.Point4<long> value2;
        GenericsNative.GetPoint4LOut(1L, 2L, 3L, 4L, &value2);
        Assert.Equal(value2.e00, 1L);
        Assert.Equal(value2.e01, 2L);
        Assert.Equal(value2.e02, 3L);
        Assert.Equal(value2.e03, 4L);

        GenericsNative.GetPoint4LOut(1L, 2L, 3L, 4L, out GenericsNative.Point4<long> value3);
        Assert.Equal(value3.e00, 1L);
        Assert.Equal(value3.e01, 2L);
        Assert.Equal(value3.e02, 3L);
        Assert.Equal(value3.e03, 4L);

        GenericsNative.Point4<long>* value4 = GenericsNative.GetPoint4LPtr(1L, 2L, 3L, 4L);
        Assert.Equal(value4->e00, 1L);
        Assert.Equal(value4->e01, 2L);
        Assert.Equal(value4->e02, 3L);
        Assert.Equal(value4->e03, 4L);

        ref readonly GenericsNative.Point4<long> value5 = ref GenericsNative.GetPoint4LRef(1L, 2L, 3L, 4L);
        Assert.Equal(value5.e00, 1L);
        Assert.Equal(value5.e01, 2L);
        Assert.Equal(value5.e02, 3L);
        Assert.Equal(value5.e03, 4L);

        GenericsNative.Point4<long> result = GenericsNative.AddPoint4L(value, value);
        Assert.Equal(result.e00, 2L);
        Assert.Equal(result.e01, 4L);
        Assert.Equal(result.e02, 6l);
        Assert.Equal(result.e03, 8l);

        GenericsNative.Point4<long>[] values = new GenericsNative.Point4<long>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point4<long>* pValues = &values[0])
        {
            GenericsNative.Point4<long> result2 = GenericsNative.AddPoint4Ls(pValues, values.Length);
            Assert.Equal(result2.e00, 5l);
            Assert.Equal(result2.e01, 10l);
            Assert.Equal(result2.e02, 15l);
            Assert.Equal(result2.e03, 20l);
        }

        GenericsNative.Point4<long> result3 = GenericsNative.AddPoint4Ls(values, values.Length);
        Assert.Equal(result3.e00, 5l);
        Assert.Equal(result3.e01, 10l);
        Assert.Equal(result3.e02, 15l);
        Assert.Equal(result3.e03, 20l);

        GenericsNative.Point4<long> result4 = GenericsNative.AddPoint4Ls(in values[0], values.Length);
        Assert.Equal(result4.e00, 5l);
        Assert.Equal(result4.e01, 10l);
        Assert.Equal(result4.e02, 15l);
        Assert.Equal(result4.e03, 20l);
    }
}
