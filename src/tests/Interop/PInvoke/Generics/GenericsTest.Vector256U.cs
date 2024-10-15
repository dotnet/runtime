// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint> GetVector256U(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256UOut(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07, Vector256<uint>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256UOut(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07, out Vector256<uint> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint>* GetVector256UPtr(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256UPtr")]
    public static extern ref readonly Vector256<uint> GetVector256URef(uint e00, uint e01, uint e02, uint e03, uint e04, uint e05, uint e06, uint e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint> AddVector256U(Vector256<uint> lhs, Vector256<uint> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint> AddVector256Us(Vector256<uint>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint> AddVector256Us([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<uint>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<uint> AddVector256Us(in Vector256<uint> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsXArch))]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector256U()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256U(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u));

        Vector256<uint> value2;
        GenericsNative.GetVector256UOut(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, &value2);
        Assert.Equal(1u, value2.GetElement(0));
        Assert.Equal(2u, value2.GetElement(1));
        Assert.Equal(3u, value2.GetElement(2));
        Assert.Equal(4u, value2.GetElement(3));
        Assert.Equal(5u, value2.GetElement(4));
        Assert.Equal(6u, value2.GetElement(5));
        Assert.Equal(7u, value2.GetElement(6));
        Assert.Equal(8u, value2.GetElement(7));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256UOut(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u, out Vector256<uint> value3));

        Vector256<uint>* value4 = GenericsNative.GetVector256UPtr(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u);
        Assert.Equal(1u, value4->GetElement(0));
        Assert.Equal(2u, value4->GetElement(1));
        Assert.Equal(3u, value4->GetElement(2));
        Assert.Equal(4u, value4->GetElement(3));
        Assert.Equal(5u, value4->GetElement(4));
        Assert.Equal(6u, value4->GetElement(5));
        Assert.Equal(7u, value4->GetElement(6));
        Assert.Equal(8u, value4->GetElement(7));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256URef(1u, 2u, 3u, 4u, 5u, 6u, 7u, 8u));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256U(default, default));

        Vector256<uint>[] values = new Vector256<uint>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<uint>* pValues = &values[0])
            {
                GenericsNative.AddVector256Us(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Us(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Us(in values[0], values.Length));
    }
}
