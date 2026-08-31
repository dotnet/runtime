// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long> GetPoint1L(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1LOut(long e00, Point1<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1LOut(long e00, out Point1<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long>* GetPoint1LPtr(long e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1LPtr")]
    public static extern ref readonly Point1<long> GetPoint1LRef(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long> AddPoint1L(Point1<long> lhs, Point1<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long> AddPoint1Ls(Point1<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long> AddPoint1Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<long> AddPoint1Ls(in Point1<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint1L()
    {
        GenericsNative.Point1<long> value = GenericsNative.GetPoint1L(1L);
        Assert.Equal(1L, value.e00);

        GenericsNative.Point1<long> value2;
        GenericsNative.GetPoint1LOut(1L, &value2);
        Assert.Equal(1L, value2.e00);

        GenericsNative.GetPoint1LOut(1L, out GenericsNative.Point1<long> value3);
        Assert.Equal(1L, value3.e00);

        GenericsNative.Point1<long>* value4 = GenericsNative.GetPoint1LPtr(1L);
        Assert.Equal(1L, value4->e00);

        ref readonly GenericsNative.Point1<long> value5 = ref GenericsNative.GetPoint1LRef(1L);
        Assert.Equal(1L, value5.e00);

        GenericsNative.Point1<long> result = GenericsNative.AddPoint1L(value, value);
        Assert.Equal(2L, result.e00);

        GenericsNative.Point1<long>[] values = new GenericsNative.Point1<long>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point1<long>* pValues = &values[0])
        {
            GenericsNative.Point1<long> result2 = GenericsNative.AddPoint1Ls(pValues, values.Length);
            Assert.Equal(5l, result2.e00);
        }

        GenericsNative.Point1<long> result3 = GenericsNative.AddPoint1Ls(values, values.Length);
        Assert.Equal(5l, result3.e00);

        GenericsNative.Point1<long> result4 = GenericsNative.AddPoint1Ls(in values[0], values.Length);
        Assert.Equal(5l, result4.e00);
    }
}
