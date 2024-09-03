// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<double> GetSpanD(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanDOut(double e00, out Span<double> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanDPtr")]
    public static extern ref readonly Span<double> GetSpanDRef(double e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<double> AddSpanD(Span<double> lhs, Span<double> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<double> AddSpanDs(in Span<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestSpanD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanD(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanDOut(1.0, out Span<double> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanDRef(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanD(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<double> value = default;
            GenericsNative.AddSpanDs(in value, 1);
        });
    }
}
