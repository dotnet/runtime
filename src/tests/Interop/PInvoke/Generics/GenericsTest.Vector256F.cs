// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float> GetVector256F(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256FOut(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, Vector256<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256FOut(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, out Vector256<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float>* GetVector256FPtr(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256FPtr")]
    public static extern ref readonly Vector256<float> GetVector256FRef(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float> AddVector256F(Vector256<float> lhs, Vector256<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float> AddVector256Fs(Vector256<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float> AddVector256Fs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<float> AddVector256Fs(in Vector256<float> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector256F()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256F(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f));

        Vector256<float> value2;
        GenericsNative.GetVector256FOut(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, &value2);
        Assert.AreEqual(value2.GetElement(0), 1.0f);
        Assert.AreEqual(value2.GetElement(1), 2.0f);
        Assert.AreEqual(value2.GetElement(2), 3.0f);
        Assert.AreEqual(value2.GetElement(3), 4.0f);
        Assert.AreEqual(value2.GetElement(4), 5.0f);
        Assert.AreEqual(value2.GetElement(5), 6.0f);
        Assert.AreEqual(value2.GetElement(6), 7.0f);
        Assert.AreEqual(value2.GetElement(7), 8.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256FOut(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, out Vector256<float> value3));

        Vector256<float>* value4 = GenericsNative.GetVector256FPtr(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);
        Assert.AreEqual(value4->GetElement(0), 1.0f);
        Assert.AreEqual(value4->GetElement(1), 2.0f);
        Assert.AreEqual(value4->GetElement(2), 3.0f);
        Assert.AreEqual(value4->GetElement(3), 4.0f);
        Assert.AreEqual(value4->GetElement(4), 5.0f);
        Assert.AreEqual(value4->GetElement(5), 6.0f);
        Assert.AreEqual(value4->GetElement(6), 7.0f);
        Assert.AreEqual(value4->GetElement(7), 8.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256FRef(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256F(default, default));

        Vector256<float>[] values = new Vector256<float>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<float>* pValues = &values[0])
            {
                GenericsNative.AddVector256Fs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Fs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Fs(in values[0], values.Length));
    }
}
