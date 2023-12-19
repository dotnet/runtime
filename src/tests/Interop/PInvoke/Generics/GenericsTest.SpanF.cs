// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<float> GetSpanF(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanFOut(float e00, out Span<float> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanFPtr")]
    public static extern ref readonly Span<float> GetSpanFRef(float e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<float> AddSpanF(Span<float> lhs, Span<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<float> AddSpanFs(in Span<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestSpanF()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanF(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanFOut(1.0f, out Span<float> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanFRef(1.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanF(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<float> value = default;
            GenericsNative.AddSpanFs(in value, 1);
        });
    }
}
