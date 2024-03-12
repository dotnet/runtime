// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanC")]
    public static extern ReadOnlySpan<char> GetReadOnlySpanC(char e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanCOut")]
    public static extern void GetReadOnlySpanCOut(char e00, out ReadOnlySpan<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanCPtr")]
    public static extern ref readonly ReadOnlySpan<char> GetReadOnlySpanCRef(char e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanC")]
    public static extern ReadOnlySpan<char> AddReadOnlySpanC(ReadOnlySpan<char> lhs, ReadOnlySpan<char> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanCs")]
    public static extern ReadOnlySpan<char> AddReadOnlySpanCs(in ReadOnlySpan<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestReadOnlySpanC()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanC('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanCOut('1', out ReadOnlySpan<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanCRef('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanC(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<char> value = default;
            GenericsNative.AddReadOnlySpanCs(in value, 1);
        });
    }
}
