// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long> GetVector256L(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256LOut(long e00, long e01, long e02, long e03, Vector256<long>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256LOut(long e00, long e01, long e02, long e03, out Vector256<long> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long>* GetVector256LPtr(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256LPtr")]
    public static extern ref readonly Vector256<long> GetVector256LRef(long e00, long e01, long e02, long e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long> AddVector256L(Vector256<long> lhs, Vector256<long> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long> AddVector256Ls(Vector256<long>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long> AddVector256Ls([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<long>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<long> AddVector256Ls(in Vector256<long> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsXArch))]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector256L()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256L(1L, 2L, 3L, 4L));

        Vector256<long> value2;
        GenericsNative.GetVector256LOut(1L, 2L, 3L, 4L, &value2);
        Assert.Equal(1L, value2.GetElement(0));
        Assert.Equal(2L, value2.GetElement(1));
        Assert.Equal(3L, value2.GetElement(2));
        Assert.Equal(4L, value2.GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256LOut(1L, 2L, 3L, 4L, out Vector256<long> value3));

        Vector256<long>* value4 = GenericsNative.GetVector256LPtr(1L, 2L, 3L, 4L);
        Assert.Equal(1L, value4->GetElement(0));
        Assert.Equal(2L, value4->GetElement(1));
        Assert.Equal(3L, value4->GetElement(2));
        Assert.Equal(4L, value4->GetElement(3));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256LRef(1L, 2L, 3L, 4L));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256L(default, default));

        Vector256<long>[] values = new Vector256<long>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<long>* pValues = &values[0])
            {
                GenericsNative.AddVector256Ls(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Ls(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Ls(in values[0], values.Length));
    }
}
