// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<uint> GetSpanU(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanUOut(uint e00, out Span<uint> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanUPtr")]
    public static extern ref readonly Span<uint> GetSpanURef(uint e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<uint> AddSpanU(Span<uint> lhs, Span<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<uint> AddSpanUs(in Span<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestSpanU()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanU(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanUOut(1u, out Span<uint> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanURef(1u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanU(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<uint> value = default;
            GenericsNative.AddSpanUs(in value, 1);
        });
    }
}
