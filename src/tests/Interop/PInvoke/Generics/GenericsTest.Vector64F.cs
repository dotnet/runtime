// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float> GetVector64F(float e00, float e01);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64FOut(float e00, float e01, Vector64<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64FOut(float e00, float e01, out Vector64<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float>* GetVector64FPtr(float e00, float e01);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64FPtr")]
    public static extern ref readonly Vector64<float> GetVector64FRef(float e00, float e01);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float> AddVector64F(Vector64<float> lhs, Vector64<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float> AddVector64Fs(Vector64<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float> AddVector64Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<float> AddVector64Fs(in Vector64<float> pValues, int count);
}

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVector64F()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64F(1.0f, 2.0f));

        Vector64<float> value2;
        GenericsNative.GetVector64FOut(1.0f, 2.0f, &value2);
        Assert.Equal(1.0f, value2.GetElement(0));
        Assert.Equal(2.0f, value2.GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64FOut(1.0f, 2.0f, out Vector64<float> value3));

        Vector64<float>* value4 = GenericsNative.GetVector64FPtr(1.0f, 2.0f);
        Assert.Equal(1.0f, value4->GetElement(0));
        Assert.Equal(2.0f, value4->GetElement(1));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64FRef(1.0f, 2.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64F(default, default));

        Vector64<float>[] values = new Vector64<float>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<float>* pValues = &values[0])
            {
                GenericsNative.AddVector64Fs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Fs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Fs(in values[0], values.Length));
    }
}
