// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float> GetVector128F(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128FOut(float e00, float e01, float e02, float e03, Vector128<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128FOut(float e00, float e01, float e02, float e03, out Vector128<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float>* GetVector128FPtr(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128FPtr")]
    public static extern ref readonly Vector128<float> GetVector128FRef(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float> AddVector128F(Vector128<float> lhs, Vector128<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float> AddVector128Fs(Vector128<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float> AddVector128Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<float> AddVector128Fs(in Vector128<float> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector128F()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128F(1.0f, 2.0f, 3.0f, 4.0f));

        Vector128<float> value2;
        GenericsNative.GetVector128FOut(1.0f, 2.0f, 3.0f, 4.0f, &value2);
        Assert.AreEqual(value2.GetElement(0), 1.0f);
        Assert.AreEqual(value2.GetElement(1), 2.0f);
        Assert.AreEqual(value2.GetElement(2), 3.0f);
        Assert.AreEqual(value2.GetElement(3), 4.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128FOut(1.0f, 2.0f, 3.0f, 4.0f, out Vector128<float> value3));

        Vector128<float>* value4 = GenericsNative.GetVector128FPtr(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.AreEqual(value4->GetElement(0), 1.0f);
        Assert.AreEqual(value4->GetElement(1), 2.0f);
        Assert.AreEqual(value4->GetElement(2), 3.0f);
        Assert.AreEqual(value4->GetElement(3), 4.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128FRef(1.0f, 2.0f, 3.0f, 4.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128F(default, default));

        Vector128<float>[] values = new Vector128<float>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<float>* pValues = &values[0])
            {
                GenericsNative.AddVector128Fs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Fs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Fs(in values[0], values.Length));
    }
}
