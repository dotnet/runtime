// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> GetVectorF128(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> GetVectorF256(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorF128Out(float e00, float e01, float e02, float e03, Vector<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorF256Out(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, Vector<float>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorF128Out(float e00, float e01, float e02, float e03, out Vector<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorF256Out(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07, out Vector<float> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float>* GetVectorF128Ptr(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float>* GetVectorF256Ptr(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorF128Ptr")]
    public static extern ref readonly Vector<float> GetVectorF128Ref(float e00, float e01, float e02, float e03);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorF256Ptr")]
    public static extern ref readonly Vector<float> GetVectorF256Ref(float e00, float e01, float e02, float e03, float e04, float e05, float e06, float e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF128(Vector<float> lhs, Vector<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF256(Vector<float> lhs, Vector<float> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF128s(Vector<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF256s(Vector<float>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<float>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF128s(in Vector<float> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<float> AddVectorF256s(in Vector<float> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVectorF()
    {
        if (Vector<float>.Count == 8)
        {
            TestVectorF256();
        }
        else
        {
            Assert.AreEqual(Vector<float>.Count, 4);
            TestVectorF128();
        }
    }

    private static void TestVectorF128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF128(1.0f, 2.0f, 3.0f, 4.0f));

        Vector<float> value2;
        GenericsNative.GetVectorF128Out(1.0f, 2.0f, 3.0f, 4.0f, &value2);
        Assert.AreEqual(value2[0], 1.0f);
        Assert.AreEqual(value2[1], 2.0f);
        Assert.AreEqual(value2[2], 3.0f);
        Assert.AreEqual(value2[3], 4.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF128Out(1.0f, 2.0f, 3.0f, 4.0f, out Vector<float> value3));

        Vector<float>* value4 = GenericsNative.GetVectorF128Ptr(1.0f, 2.0f, 3.0f, 4.0f);
        Assert.AreEqual((*value4)[0], 1.0f);
        Assert.AreEqual((*value4)[1], 2.0f);
        Assert.AreEqual((*value4)[2], 3.0f);
        Assert.AreEqual((*value4)[3], 4.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF128Ref(1.0f, 2.0f, 3.0f, 4.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF128(default, default));

        Vector<float>[] values = new Vector<float>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<float>* pValues = &values[0])
            {
                GenericsNative.AddVectorF128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF128s(in values[0], values.Length));
    }

    private static void TestVectorF256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF256(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f));

        Vector<float> value2;
        GenericsNative.GetVectorF256Out(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, &value2);
        Assert.AreEqual(value2[0], 1.0f);
        Assert.AreEqual(value2[1], 2.0f);
        Assert.AreEqual(value2[2], 3.0f);
        Assert.AreEqual(value2[3], 4.0f);
        Assert.AreEqual(value2[4], 5.0f);
        Assert.AreEqual(value2[5], 6.0f);
        Assert.AreEqual(value2[6], 7.0f);
        Assert.AreEqual(value2[7], 8.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF256Out(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, out Vector<float> value3));

        Vector<float>* value4 = GenericsNative.GetVectorF256Ptr(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);
        Assert.AreEqual((*value4)[0], 1.0f);
        Assert.AreEqual((*value4)[1], 2.0f);
        Assert.AreEqual((*value4)[2], 3.0f);
        Assert.AreEqual((*value4)[3], 4.0f);
        Assert.AreEqual((*value4)[4], 5.0f);
        Assert.AreEqual((*value4)[5], 6.0f);
        Assert.AreEqual((*value4)[6], 7.0f);
        Assert.AreEqual((*value4)[7], 8.0f);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorF256Ref(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF256(default, default));

        Vector<float>[] values = new Vector<float>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<float>* pValues = &values[0])
            {
                GenericsNative.AddVectorF256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorF256s(in values[0], values.Length));
    }
}
