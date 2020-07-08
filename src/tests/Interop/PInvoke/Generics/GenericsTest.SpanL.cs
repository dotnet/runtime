// Licensed to the .NET Doundation under one or more agreements.
// The .NET Doundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Span<long> GetSpanL(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetSpanLOut(long e00, out Span<long> value);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetSpanLPtr")]
    public static extern ref readonly Span<long> GetSpanLRef(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<long> AddSpanL(Span<long> lhs, Span<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Span<long> AddSpanLs(in Span<long> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestSpanL()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanL(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanLOut(1L, out Span<long> value3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetSpanLRef(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddSpanL(default, default));

        Assert.Throws<MarshalDirectiveException>(() => {
            Span<long> value = default;
            GenericsNative.AddSpanLs(in value, 1);
        });
    }
}
