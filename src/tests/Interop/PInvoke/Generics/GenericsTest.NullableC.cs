// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern char? GetNullableC(bool hasValue, char value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableCOut(bool hasValue, char value, out char? pValue);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableCPtr")]
    public static extern ref readonly char? GetNullableCRef(bool hasValue, char value);

    [DllImport(nameof(GenericsNative))]
    public static extern char? AddNullableC(char? lhs, char? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern char? AddNullableCs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern char? AddNullableCs(in char? pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestNullableC()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableC(true, '1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableCOut(true, '1', out char? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableC(default, default));

        char?[] values = new char?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableCs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableCs(in values[0], values.Length));
    }
}
