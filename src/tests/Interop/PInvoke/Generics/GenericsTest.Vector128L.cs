// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long> GetVector128L(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128LOut(long e00, long e01, Vector128<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128LOut(long e00, long e01, out Vector128<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long>* GetVector128LPtr(long e00, long e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128LPtr")]
    public static extern ref readonly Vector128<long> GetVector128LRef(long e00, long e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long> AddVector128L(Vector128<long> lhs, Vector128<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long> AddVector128Ls(Vector128<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long> AddVector128Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<long> AddVector128Ls(in Vector128<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector128L()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128L(1L, 2L));

        Vector128<long> value2;
        GenericsNative.GetVector128LOut(1L, 2L, &value2);
        Assert.Equal(1L, value2.GetElement(0));
        Assert.Equal(2L, value2.GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128LOut(1L, 2L, out Vector128<long> value3));

        Vector128<long>* value4 = GenericsNative.GetVector128LPtr(1L, 2L);
        Assert.Equal(1L, value4->GetElement(0));
        Assert.Equal(2L, value4->GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128LRef(1L, 2L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128L(default, default));

        Vector128<long>[] values = new Vector128<long>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<long>* pValues = &values[0])
            {
                GenericsNative.AddVector128Ls(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Ls(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Ls(in values[0], values.Length));
    }
}
