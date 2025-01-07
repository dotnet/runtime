// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long> GetVector64L(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64LOut(long e00, Vector64<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64LOut(long e00, out Vector64<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long>* GetVector64LPtr(long e00);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64LPtr")]
    public static extern ref readonly Vector64<long> GetVector64LRef(long e00);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long> AddVector64L(Vector64<long> lhs, Vector64<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long> AddVector64Ls(Vector64<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long> AddVector64Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<long> AddVector64Ls(in Vector64<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector64L()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64L(1L));

        Vector64<long> value2;
        GenericsNative.GetVector64LOut(1L, &value2);
        Assert.Equal(1L, value2.GetElement(0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64LOut(1L, out Vector64<long> value3));

        Vector64<long>* value4 = GenericsNative.GetVector64LPtr(1L);
        Assert.Equal(1L, value4->GetElement(0));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64LRef(1L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64L(default, default));

        Vector64<long>[] values = new Vector64<long>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<long>* pValues = &values[0])
            {
                GenericsNative.AddVector64Ls(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Ls(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Ls(in values[0], values.Length));
    }
}
