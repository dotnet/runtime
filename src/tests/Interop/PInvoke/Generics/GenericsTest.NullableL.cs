// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern long? GetNullableL(bool hasValue, long value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetNullableLOut(bool hasValue, long value, out long? pValue);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetNullableLPtr")]
    public static extern ref readonly long? GetNullableLRef(bool hasValue, long value);

    [DllImport(nameof(GenericsNative))]
    public static extern long? AddNullableL(long? lhs, long? rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern long? AddNullableLs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] long?[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern long? AddNullableLs(in long? pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestNullableL()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableL(true, 1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetNullableLOut(true, 1L, out long? value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableL(default, default));

        long?[] values = new long?[] {
            default,
            default,
            default,
            default,
            default
        };

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableLs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddNullableLs(in values[0], values.Length));
    }
}
