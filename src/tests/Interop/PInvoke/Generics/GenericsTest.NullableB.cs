// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern bool? GetNullableB(bool hasValue, bool value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableBOut(bool hasValue, bool value, out bool? pValue);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableBPtr")]
    public static extern ref readonly bool? GetNullableBRef(bool hasValue, bool value);

    [DllImport(nameof(GenericsNative))]
    public static extern bool? AddNullableB(bool? lhs, bool? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern bool? AddNullableBs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] bool?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern bool? AddNullableBs(in bool? pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestNullableB()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableB(true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableBOut(true, false, out bool? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableB(default, default));

        bool?[] values = new bool?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableBs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableBs(in values[0], values.Length));
    }
}
