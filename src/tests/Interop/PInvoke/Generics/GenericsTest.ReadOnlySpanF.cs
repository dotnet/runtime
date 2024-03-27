// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanF")]
    public static extern ReadOnlySpan<float> GetReadOnlySpanF(float e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanFOut")]
    public static extern void GetReadOnlySpanFOut(float e00, out ReadOnlySpan<float> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanFPtr")]
    public static extern ref readonly ReadOnlySpan<float> GetReadOnlySpanFRef(float e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanF")]
    public static extern ReadOnlySpan<float> AddReadOnlySpanF(ReadOnlySpan<float> lhs, ReadOnlySpan<float> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanFs")]
    public static extern ReadOnlySpan<float> AddReadOnlySpanFs(in ReadOnlySpan<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestReadOnlySpanF()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanF(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanFOut(1.0f, out ReadOnlySpan<float> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanFRef(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanF(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<float> value = default;
            GenericsNative.AddReadOnlySpanFs(in value, 1);
        });
    }
}
