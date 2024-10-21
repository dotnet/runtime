// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanU")]
    public static extern ReadOnlySpan<uint> GetReadOnlySpanU(uint e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanUOut")]
    public static extern void GetReadOnlySpanUOut(uint e00, out ReadOnlySpan<uint> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanUPtr")]
    public static extern ref readonly ReadOnlySpan<uint> GetReadOnlySpanURef(uint e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanU")]
    public static extern ReadOnlySpan<uint> AddReadOnlySpanU(ReadOnlySpan<uint> lhs, ReadOnlySpan<uint> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanUs")]
    public static extern ReadOnlySpan<uint> AddReadOnlySpanUs(in ReadOnlySpan<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestReadOnlySpanU()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanU(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanUOut(1u, out ReadOnlySpan<uint> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanURef(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanU(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<uint> value = default;
            GenericsNative.AddReadOnlySpanUs(in value, 1);
        });
    }
}
