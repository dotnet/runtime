// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<char> GetSpanC(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanCOut(char e00, out Span<char> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanCPtr")]
    public static extern ref readonly Span<char> GetSpanCRef(char e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<char> AddSpanC(Span<char> lhs, Span<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<char> AddSpanCs(in Span<char> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSpanC()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanC('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanCOut('1', out Span<char> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanCRef('1'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanC(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<char> value = default;
            GenericsNative.AddSpanCs(in value, 1);
        });
    }
}
