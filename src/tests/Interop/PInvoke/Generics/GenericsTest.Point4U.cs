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

unsafe partial class GenericsTest
{
    private static void TestPoint4U()
    {
        GenericsNative.Point4<uint> value = GenericsNative.GetPoint4U(1u, 2u, 3u, 4u);
        Assert.Equal(value.e00, 1u);
        Assert.Equal(value.e01, 2u);
        Assert.Equal(value.e02, 3u);
        Assert.Equal(value.e03, 4u);

        GenericsNative.Point4<uint> value2;
        GenericsNative.GetPoint4UOut(1u, 2u, 3u, 4u, &value2);
        Assert.Equal(value2.e00, 1u);
        Assert.Equal(value2.e01, 2u);
        Assert.Equal(value2.e02, 3u);
        Assert.Equal(value2.e03, 4u);

        GenericsNative.GetPoint4UOut(1u, 2u, 3u, 4u, out GenericsNative.Point4<uint> value3);
        Assert.Equal(value3.e00, 1u);
        Assert.Equal(value3.e01, 2u);
        Assert.Equal(value3.e02, 3u);
        Assert.Equal(value3.e03, 4u);

        GenericsNative.Point4<uint>* value4 = GenericsNative.GetPoint4UPtr(1u, 2u, 3u, 4u);
        Assert.Equal(value4->e00, 1u);
        Assert.Equal(value4->e01, 2u);
        Assert.Equal(value4->e02, 3u);
        Assert.Equal(value4->e03, 4u);

        ref readonly GenericsNative.Point4<uint> value5 = ref GenericsNative.GetPoint4URef(1u, 2u, 3u, 4u);
        Assert.Equal(value5.e00, 1u);
        Assert.Equal(value5.e01, 2u);
        Assert.Equal(value5.e02, 3u);
        Assert.Equal(value5.e03, 4u);

        GenericsNative.Point4<uint> result = GenericsNative.AddPoint4U(value, value);
        Assert.Equal(result.e00, 2u);
        Assert.Equal(result.e01, 4u);
        Assert.Equal(result.e02, 6u);
        Assert.Equal(result.e03, 8u);

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
            Assert.Equal(result2.e00, 5u);
            Assert.Equal(result2.e01, 10u);
            Assert.Equal(result2.e02, 15u);
            Assert.Equal(result2.e03, 20u);
        }

        GenericsNative.Point4<uint> result3 = GenericsNative.AddPoint4Us(values, values.Length);
        Assert.Equal(result3.e00, 5u);
        Assert.Equal(result3.e01, 10u);
        Assert.Equal(result3.e02, 15u);
        Assert.Equal(result3.e03, 20u);

        GenericsNative.Point4<uint> result4 = GenericsNative.AddPoint4Us(in values[0], values.Length);
        Assert.Equal(result4.e00, 5u);
        Assert.Equal(result4.e01, 10u);
        Assert.Equal(result4.e02, 15u);
        Assert.Equal(result4.e03, 20u);
    }
}
