// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint> GetPoint1U(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1UOut(uint e00, Point1<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetPoint1UOut(uint e00, out Point1<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint>* GetPoint1UPtr(uint e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetPoint1UPtr")]
    public static extern ref readonly Point1<uint> GetPoint1URef(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint> AddPoint1U(Point1<uint> lhs, Point1<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint> AddPoint1Us(Point1<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint> AddPoint1Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Point1<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Point1<uint> AddPoint1Us(in Point1<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestPoint1U()
    {
        GenericsNative.Point1<uint> value = GenericsNative.GetPoint1U(1u);
        Assert.Equal(1u, value.e00);

        GenericsNative.Point1<uint> value2;
        GenericsNative.GetPoint1UOut(1u, &value2);
        Assert.Equal(1u, value2.e00);

        GenericsNative.GetPoint1UOut(1u, out GenericsNative.Point1<uint> value3);
        Assert.Equal(1u, value3.e00);

        GenericsNative.Point1<uint>* value4 = GenericsNative.GetPoint1UPtr(1u);
        Assert.Equal(1u, value4->e00);

        ref readonly GenericsNative.Point1<uint> value5 = ref GenericsNative.GetPoint1URef(1u);
        Assert.Equal(1u, value5.e00);

        GenericsNative.Point1<uint> result = GenericsNative.AddPoint1U(value, value);
        Assert.Equal(2u, result.e00);

        GenericsNative.Point1<uint>[] values = new GenericsNative.Point1<uint>[] {
            value,
            value2,
            value3,
            *value4,
            value5
        };

        fixed (GenericsNative.Point1<uint>* pValues = &values[0])
        {
            GenericsNative.Point1<uint> result2 = GenericsNative.AddPoint1Us(pValues, values.Length);
            Assert.Equal(5u, result2.e00);
        }

        GenericsNative.Point1<uint> result3 = GenericsNative.AddPoint1Us(values, values.Length);
        Assert.Equal(5u, result3.e00);

        GenericsNative.Point1<uint> result4 = GenericsNative.AddPoint1Us(in values[0], values.Length);
        Assert.Equal(5u, result4.e00);
    }
}
