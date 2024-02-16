// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanD")]
    public static extern ReadOnlySpan<double> GetReadOnlySpanD(double e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanDOut")]
    public static extern void GetReadOnlySpanDOut(double e00, out ReadOnlySpan<double> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanDPtr")]
    public static extern ref readonly ReadOnlySpan<double> GetReadOnlySpanDRef(double e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanD")]
    public static extern ReadOnlySpan<double> AddReadOnlySpanD(ReadOnlySpan<double> lhs, ReadOnlySpan<double> rhs);

    [DllImport(nameof(GenericsNative), EntryPoint = "AddSpanDs")]
    public static extern ReadOnlySpan<double> AddReadOnlySpanDs(in ReadOnlySpan<double> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestReadOnlySpanD()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanD(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanDOut(1.0, out ReadOnlySpan<double> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetReadOnlySpanDRef(1.0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddReadOnlySpanD(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            ReadOnlySpan<double> value = default;
            GenericsNative.AddReadOnlySpanDs(in value, 1);
        });
    }
}
