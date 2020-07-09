// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanL")]
    public static extern ReadOnlySpan<long> GetReadOnlySpanL(long e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanLOut")]
    public static extern void GetReadOnlySpanLOut(long e00, out ReadOnlySpan<long> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanLPtr")]
    public static extern ref readonly ReadOnlySpan<long> GetReadOnlySpanLRef(long e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanL")]
    public static extern ReadOnlySpan<long> AddReadOnlySpanL(ReadOnlySpan<long> lhs, ReadOnlySpan<long> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanLs")]
    public static extern ReadOnlySpan<long> AddReadOnlySpanLs(in ReadOnlySpan<long> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestReadOnlySpanL()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanL(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanLOut(1L, out ReadOnlySpan<long> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanLRef(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanL(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<long> value = default;
            GenericsNative.AddReadOnlySpanLs(in value, 1);
        });
    }
}
