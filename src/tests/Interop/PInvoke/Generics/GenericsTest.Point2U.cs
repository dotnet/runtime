// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint> GetPoint2U(uint e00, uint e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2UOut(uint e00, uint e01, Point2<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint2UOut(uint e00, uint e01, out Point2<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint>* GetPoint2UPtr(uint e00, uint e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint2UPtr")]
    public static extern ref readonly Point2<uint> GetPoint2URef(uint e00, uint e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint> AddPoint2U(Point2<uint> lhs, Point2<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint> AddPoint2Us(Point2<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint> AddPoint2Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point2<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point2<uint> AddPoint2Us(in Point2<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint2U()
    {
        GenericsNative.Point2<uint> value = GenericsNative.GetPoint2U(1u, 2u);
        Assert.Equal(1u, value.e00);
        Assert.Equal(2u, value.e01);

        GenericsNative.Point2<uint> value2;
        GenericsNative.GetPoint2UOut(1u, 2u, &value2);
        Assert.Equal(1u, value2.e00);
        Assert.Equal(2u, value2.e01);

        GenericsNative.GetPoint2UOut(1u, 2u, out GenericsNative.Point2<uint> value3);
        Assert.Equal(1u, value3.e00);
        Assert.Equal(2u, value3.e01);

        GenericsNative.Point2<uint>* value4 = GenericsNative.GetPoint2UPtr(1u, 2u);
        Assert.Equal(1u, value4->e00);
        Assert.Equal(2u, value4->e01);

        ref readonly GenericsNative.Point2<uint> value5 = ref GenericsNative.GetPoint2URef(1u, 2u);
        Assert.Equal(1u, value5.e00);
        Assert.Equal(2u, value5.e01);

        GenericsNative.Point2<uint> result = GenericsNative.AddPoint2U(value, value);
        Assert.Equal(2u, result.e00);
        Assert.Equal(4u, result.e01);

        GenericsNative.Point2<uint>[] values = new GenericsNative.Point2<uint>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point2<uint>* pValues = &values[0])
        {
            GenericsNative.Point2<uint> result2 = GenericsNative.AddPoint2Us(pValues, values.Length);
            Assert.Equal(5u, result2.e00);
            Assert.Equal(10u, result2.e01);
        }

        GenericsNative.Point2<uint> result3 = GenericsNative.AddPoint2Us(values, values.Length);
        Assert.Equal(5u, result3.e00);
        Assert.Equal(10u, result3.e01);

        GenericsNative.Point2<uint> result4 = GenericsNative.AddPoint2Us(in values[0], values.Length);
        Assert.Equal(5u, result4.e00);
        Assert.Equal(10u, result4.e01);
    }
}
