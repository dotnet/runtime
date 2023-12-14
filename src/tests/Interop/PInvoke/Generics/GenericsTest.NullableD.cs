// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern double? GetNullableD(bool hasValue, double value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableDOut(bool hasValue, double value, out double? pValue);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableDPtr")]
    public static extern ref readonly double? GetNullableDRef(bool hasValue, double value);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableD(double? lhs, double? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableDs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern double? AddNullableDs(in double? pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestNullableD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableD(true, 1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableDOut(true, 1.0, out double? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableD(default, default));

        double?[] values = new double?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableDs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableDs(in values[0], values.Length));
    }
}
