// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<bool> GetSpanB(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanBOut(bool e00, out Span<bool> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanBPtr")]
    public static extern ref readonly Span<bool> GetSpanBRef(bool e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<bool> AddSpanB(Span<bool> lhs, Span<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<bool> AddSpanBs(in Span<bool> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    public static void TestSpanB()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanB(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanBOut(true, out Span<bool> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanBRef(true));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanB(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<bool> value = default;
            GenericsNative.AddSpanBs(in value, 1);
        });
    }
}
