// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint> GetPoint4U(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4UOut(uint e00, uint e01, uint e02, uint e03, Point4<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint4UOut(uint e00, uint e01, uint e02, uint e03, out Point4<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint>* GetPoint4UPtr(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint4UPtr")]
    public static extern ref readonly Point4<uint> GetPoint4URef(uint e00, uint e01, uint e02, uint e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint> AddPoint4U(Point4<uint> lhs, Point4<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint> AddPoint4Us(Point4<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint> AddPoint4Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point4<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point4<uint> AddPoint4Us(in Point4<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint4U()
    {
        GenericsNative.Point4<uint> value = GenericsNative.GetPoint4U(1u, 2u, 3u, 4u);
        Assert.Equal(1u, value.e00);
        Assert.Equal(2u, value.e01);
        Assert.Equal(3u, value.e02);
        Assert.Equal(4u, value.e03);

        GenericsNative.Point4<uint> value2;
        GenericsNative.GetPoint4UOut(1u, 2u, 3u, 4u, &value2);
        Assert.Equal(1u, value2.e00);
        Assert.Equal(2u, value2.e01);
        Assert.Equal(3u, value2.e02);
        Assert.Equal(4u, value2.e03);

        GenericsNative.GetPoint4UOut(1u, 2u, 3u, 4u, out GenericsNative.Point4<uint> value3);
        Assert.Equal(1u, value3.e00);
        Assert.Equal(2u, value3.e01);
        Assert.Equal(3u, value3.e02);
        Assert.Equal(4u, value3.e03);

        GenericsNative.Point4<uint>* value4 = GenericsNative.GetPoint4UPtr(1u, 2u, 3u, 4u);
        Assert.Equal(1u, value4->e00);
        Assert.Equal(2u, value4->e01);
        Assert.Equal(3u, value4->e02);
        Assert.Equal(4u, value4->e03);

        ref readonly GenericsNative.Point4<uint> value5 = ref GenericsNative.GetPoint4URef(1u, 2u, 3u, 4u);
        Assert.Equal(1u, value5.e00);
        Assert.Equal(2u, value5.e01);
        Assert.Equal(3u, value5.e02);
        Assert.Equal(4u, value5.e03);

        GenericsNative.Point4<uint> result = GenericsNative.AddPoint4U(value, value);
        Assert.Equal(2u, result.e00);
        Assert.Equal(4u, result.e01);
        Assert.Equal(6u, result.e02);
        Assert.Equal(8u, result.e03);

        GenericsNative.Point4<uint>[] values = new GenericsNative.Point4<uint>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point4<uint>* pValues = &values[0])
        {
            GenericsNative.Point4<uint> result2 = GenericsNative.AddPoint4Us(pValues, values.Length);
            Assert.Equal(5u, result2.e00);
            Assert.Equal(10u, result2.e01);
            Assert.Equal(15u, result2.e02);
            Assert.Equal(20u, result2.e03);
        }

        GenericsNative.Point4<uint> result3 = GenericsNative.AddPoint4Us(values, values.Length);
        Assert.Equal(5u, result3.e00);
        Assert.Equal(10u, result3.e01);
        Assert.Equal(15u, result3.e02);
        Assert.Equal(20u, result3.e03);

        GenericsNative.Point4<uint> result4 = GenericsNative.AddPoint4Us(in values[0], values.Length);
        Assert.Equal(5u, result4.e00);
        Assert.Equal(10u, result4.e01);
        Assert.Equal(15u, result4.e02);
        Assert.Equal(20u, result4.e03);
    }
}
