// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern uint? GetNullableU(bool hasValue, uint value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableUOut(bool hasValue, uint value, out uint? pValue);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableUPtr")]
    public static extern ref readonly uint? GetNullableURef(bool hasValue, uint value);

    [DllImport(nameof(GenericsNative))]
    public static extern uint? AddNullableU(uint? lhs, uint? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern uint? AddNullableUs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern uint? AddNullableUs(in uint? pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestNullableU()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableU(true, 1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableUOut(true, 1u, out uint? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableU(default, default));

        uint?[] values = new uint?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableUs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableUs(in values[0], values.Length));
    }
}
