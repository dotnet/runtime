// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanB")]
    public static extern ReadOnlySpan<bool> GetReadOnlySpanB(bool e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanBOut")]
    public static extern void GetReadOnlySpanBOut(bool e00, out ReadOnlySpan<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanBPtr")]
    public static extern ref readonly ReadOnlySpan<bool> GetReadOnlySpanBRef(bool e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanB")]
    public static extern ReadOnlySpan<bool> AddReadOnlySpanB(ReadOnlySpan<bool> lhs, ReadOnlySpan<bool> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanBs")]
    public static extern ReadOnlySpan<bool> AddReadOnlySpanBs(in ReadOnlySpan<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestReadOnlySpanB()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanB(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanBOut(true, out ReadOnlySpan<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanBRef(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanB(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<bool> value = default;
            GenericsNative.AddReadOnlySpanBs(in value, 1);
        });
    }
}
