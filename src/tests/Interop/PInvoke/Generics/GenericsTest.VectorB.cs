// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> GetVectorB128([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> GetVectorB256([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, [MarshalAs(UnmanagedType.U1)]bool e16, [MarshalAs(UnmanagedType.U1)]bool e17, [MarshalAs(UnmanagedType.U1)]bool e18, [MarshalAs(UnmanagedType.U1)]bool e19, [MarshalAs(UnmanagedType.U1)]bool e20, [MarshalAs(UnmanagedType.U1)]bool e21, [MarshalAs(UnmanagedType.U1)]bool e22, [MarshalAs(UnmanagedType.U1)]bool e23, [MarshalAs(UnmanagedType.U1)]bool e24, [MarshalAs(UnmanagedType.U1)]bool e25, [MarshalAs(UnmanagedType.U1)]bool e26, [MarshalAs(UnmanagedType.U1)]bool e27, [MarshalAs(UnmanagedType.U1)]bool e28, [MarshalAs(UnmanagedType.U1)]bool e29, [MarshalAs(UnmanagedType.U1)]bool e30, [MarshalAs(UnmanagedType.U1)]bool e31);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorB128Out([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, Vector<bool>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorB256Out([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, [MarshalAs(UnmanagedType.U1)]bool e16, [MarshalAs(UnmanagedType.U1)]bool e17, [MarshalAs(UnmanagedType.U1)]bool e18, [MarshalAs(UnmanagedType.U1)]bool e19, [MarshalAs(UnmanagedType.U1)]bool e20, [MarshalAs(UnmanagedType.U1)]bool e21, [MarshalAs(UnmanagedType.U1)]bool e22, [MarshalAs(UnmanagedType.U1)]bool e23, [MarshalAs(UnmanagedType.U1)]bool e24, [MarshalAs(UnmanagedType.U1)]bool e25, [MarshalAs(UnmanagedType.U1)]bool e26, [MarshalAs(UnmanagedType.U1)]bool e27, [MarshalAs(UnmanagedType.U1)]bool e28, [MarshalAs(UnmanagedType.U1)]bool e29, [MarshalAs(UnmanagedType.U1)]bool e30, [MarshalAs(UnmanagedType.U1)]bool e31, Vector<bool>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorB128Out([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, out Vector<bool> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorB256Out([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, [MarshalAs(UnmanagedType.U1)]bool e16, [MarshalAs(UnmanagedType.U1)]bool e17, [MarshalAs(UnmanagedType.U1)]bool e18, [MarshalAs(UnmanagedType.U1)]bool e19, [MarshalAs(UnmanagedType.U1)]bool e20, [MarshalAs(UnmanagedType.U1)]bool e21, [MarshalAs(UnmanagedType.U1)]bool e22, [MarshalAs(UnmanagedType.U1)]bool e23, [MarshalAs(UnmanagedType.U1)]bool e24, [MarshalAs(UnmanagedType.U1)]bool e25, [MarshalAs(UnmanagedType.U1)]bool e26, [MarshalAs(UnmanagedType.U1)]bool e27, [MarshalAs(UnmanagedType.U1)]bool e28, [MarshalAs(UnmanagedType.U1)]bool e29, [MarshalAs(UnmanagedType.U1)]bool e30, [MarshalAs(UnmanagedType.U1)]bool e31, out Vector<bool> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool>* GetVectorB128Ptr([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool>* GetVectorB256Ptr([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, [MarshalAs(UnmanagedType.U1)]bool e16, [MarshalAs(UnmanagedType.U1)]bool e17, [MarshalAs(UnmanagedType.U1)]bool e18, [MarshalAs(UnmanagedType.U1)]bool e19, [MarshalAs(UnmanagedType.U1)]bool e20, [MarshalAs(UnmanagedType.U1)]bool e21, [MarshalAs(UnmanagedType.U1)]bool e22, [MarshalAs(UnmanagedType.U1)]bool e23, [MarshalAs(UnmanagedType.U1)]bool e24, [MarshalAs(UnmanagedType.U1)]bool e25, [MarshalAs(UnmanagedType.U1)]bool e26, [MarshalAs(UnmanagedType.U1)]bool e27, [MarshalAs(UnmanagedType.U1)]bool e28, [MarshalAs(UnmanagedType.U1)]bool e29, [MarshalAs(UnmanagedType.U1)]bool e30, [MarshalAs(UnmanagedType.U1)]bool e31);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorB128Ptr")]
    public static extern ref readonly Vector<bool> GetVectorB128Ref([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorB256Ptr")]
    public static extern ref readonly Vector<bool> GetVectorB256Ref([MarshalAs(UnmanagedType.U1)]bool e00, [MarshalAs(UnmanagedType.U1)]bool e01, [MarshalAs(UnmanagedType.U1)]bool e02, [MarshalAs(UnmanagedType.U1)]bool e03, [MarshalAs(UnmanagedType.U1)]bool e04, [MarshalAs(UnmanagedType.U1)]bool e05, [MarshalAs(UnmanagedType.U1)]bool e06, [MarshalAs(UnmanagedType.U1)]bool e07, [MarshalAs(UnmanagedType.U1)]bool e08, [MarshalAs(UnmanagedType.U1)]bool e09, [MarshalAs(UnmanagedType.U1)]bool e10, [MarshalAs(UnmanagedType.U1)]bool e11, [MarshalAs(UnmanagedType.U1)]bool e12, [MarshalAs(UnmanagedType.U1)]bool e13, [MarshalAs(UnmanagedType.U1)]bool e14, [MarshalAs(UnmanagedType.U1)]bool e15, [MarshalAs(UnmanagedType.U1)]bool e16, [MarshalAs(UnmanagedType.U1)]bool e17, [MarshalAs(UnmanagedType.U1)]bool e18, [MarshalAs(UnmanagedType.U1)]bool e19, [MarshalAs(UnmanagedType.U1)]bool e20, [MarshalAs(UnmanagedType.U1)]bool e21, [MarshalAs(UnmanagedType.U1)]bool e22, [MarshalAs(UnmanagedType.U1)]bool e23, [MarshalAs(UnmanagedType.U1)]bool e24, [MarshalAs(UnmanagedType.U1)]bool e25, [MarshalAs(UnmanagedType.U1)]bool e26, [MarshalAs(UnmanagedType.U1)]bool e27, [MarshalAs(UnmanagedType.U1)]bool e28, [MarshalAs(UnmanagedType.U1)]bool e29, [MarshalAs(UnmanagedType.U1)]bool e30, [MarshalAs(UnmanagedType.U1)]bool e31);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB128(Vector<bool> lhs, Vector<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB256(Vector<bool> lhs, Vector<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB128s(Vector<bool>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB256s(Vector<bool>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB128s(in Vector<bool> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<bool> AddVectorB256s(in Vector<bool> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVectorB()
    {
        if (Vector<byte>.Count == 32)
        {
            TestVectorB256();
        }
        else
        {
            Assert.AreEqual(Vector<byte>.Count, 16);
            TestVectorB128();
        }
    }

    private static void TestVectorB128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB128(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector<bool> value2;
        GenericsNative.GetVectorB128Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector<byte> tValue2 = *(Vector<byte>*)&value2;
        Assert.AreEqual(tValue2[0], 1);
        Assert.AreEqual(tValue2[1], 0);
        Assert.AreEqual(tValue2[2], 1);
        Assert.AreEqual(tValue2[3], 0);
        Assert.AreEqual(tValue2[4], 1);
        Assert.AreEqual(tValue2[5], 0);
        Assert.AreEqual(tValue2[6], 1);
        Assert.AreEqual(tValue2[7], 0);
        Assert.AreEqual(tValue2[8], 1);
        Assert.AreEqual(tValue2[9], 0);
        Assert.AreEqual(tValue2[10], 1);
        Assert.AreEqual(tValue2[11], 0);
        Assert.AreEqual(tValue2[12], 1);
        Assert.AreEqual(tValue2[13], 0);
        Assert.AreEqual(tValue2[14], 1);
        Assert.AreEqual(tValue2[15], 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB128Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector<bool> value3));

        Vector<bool>* value4 = GenericsNative.GetVectorB128Ptr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector<byte>* tValue4 = (Vector<byte>*)value4;
        Assert.AreEqual((*tValue4)[0], 1);
        Assert.AreEqual((*tValue4)[1], 0);
        Assert.AreEqual((*tValue4)[2], 1);
        Assert.AreEqual((*tValue4)[3], 0);
        Assert.AreEqual((*tValue4)[4], 1);
        Assert.AreEqual((*tValue4)[5], 0);
        Assert.AreEqual((*tValue4)[6], 1);
        Assert.AreEqual((*tValue4)[7], 0);
        Assert.AreEqual((*tValue4)[8], 1);
        Assert.AreEqual((*tValue4)[9], 0);
        Assert.AreEqual((*tValue4)[10], 1);
        Assert.AreEqual((*tValue4)[11], 0);
        Assert.AreEqual((*tValue4)[12], 1);
        Assert.AreEqual((*tValue4)[13], 0);
        Assert.AreEqual((*tValue4)[14], 1);
        Assert.AreEqual((*tValue4)[15], 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB128Ref(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB128(default, default));

        Vector<bool>[] values = new Vector<bool>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<bool>* pValues = &values[0])
            {
                GenericsNative.AddVectorB128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB128s(in values[0], values.Length));
    }

    private static void TestVectorB256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB256(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector<bool> value2;
        GenericsNative.GetVectorB256Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector<byte> tValue2 = *(Vector<byte>*)&value2;
        Assert.AreEqual(tValue2[0], 1);
        Assert.AreEqual(tValue2[1], 0);
        Assert.AreEqual(tValue2[2], 1);
        Assert.AreEqual(tValue2[3], 0);
        Assert.AreEqual(tValue2[4], 1);
        Assert.AreEqual(tValue2[5], 0);
        Assert.AreEqual(tValue2[6], 1);
        Assert.AreEqual(tValue2[7], 0);
        Assert.AreEqual(tValue2[8], 1);
        Assert.AreEqual(tValue2[9], 0);
        Assert.AreEqual(tValue2[10], 1);
        Assert.AreEqual(tValue2[11], 0);
        Assert.AreEqual(tValue2[12], 1);
        Assert.AreEqual(tValue2[13], 0);
        Assert.AreEqual(tValue2[14], 1);
        Assert.AreEqual(tValue2[15], 0);
        Assert.AreEqual(tValue2[16], 1);
        Assert.AreEqual(tValue2[17], 0);
        Assert.AreEqual(tValue2[18], 1);
        Assert.AreEqual(tValue2[19], 0);
        Assert.AreEqual(tValue2[20], 1);
        Assert.AreEqual(tValue2[21], 0);
        Assert.AreEqual(tValue2[22], 1);
        Assert.AreEqual(tValue2[23], 0);
        Assert.AreEqual(tValue2[24], 1);
        Assert.AreEqual(tValue2[25], 0);
        Assert.AreEqual(tValue2[26], 1);
        Assert.AreEqual(tValue2[27], 0);
        Assert.AreEqual(tValue2[28], 1);
        Assert.AreEqual(tValue2[29], 0);
        Assert.AreEqual(tValue2[30], 1);
        Assert.AreEqual(tValue2[31], 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB256Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector<bool> value3));

        Vector<bool>* value4 = GenericsNative.GetVectorB256Ptr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector<byte>* tValue4 = (Vector<byte>*)value4;
        Assert.AreEqual((*tValue4)[0], 1);
        Assert.AreEqual((*tValue4)[1], 0);
        Assert.AreEqual((*tValue4)[2], 1);
        Assert.AreEqual((*tValue4)[3], 0);
        Assert.AreEqual((*tValue4)[4], 1);
        Assert.AreEqual((*tValue4)[5], 0);
        Assert.AreEqual((*tValue4)[6], 1);
        Assert.AreEqual((*tValue4)[7], 0);
        Assert.AreEqual((*tValue4)[8], 1);
        Assert.AreEqual((*tValue4)[9], 0);
        Assert.AreEqual((*tValue4)[10], 1);
        Assert.AreEqual((*tValue4)[11], 0);
        Assert.AreEqual((*tValue4)[12], 1);
        Assert.AreEqual((*tValue4)[13], 0);
        Assert.AreEqual((*tValue4)[14], 1);
        Assert.AreEqual((*tValue4)[15], 0);
        Assert.AreEqual((*tValue4)[16], 1);
        Assert.AreEqual((*tValue4)[17], 0);
        Assert.AreEqual((*tValue4)[18], 1);
        Assert.AreEqual((*tValue4)[19], 0);
        Assert.AreEqual((*tValue4)[20], 1);
        Assert.AreEqual((*tValue4)[21], 0);
        Assert.AreEqual((*tValue4)[22], 1);
        Assert.AreEqual((*tValue4)[23], 0);
        Assert.AreEqual((*tValue4)[24], 1);
        Assert.AreEqual((*tValue4)[25], 0);
        Assert.AreEqual((*tValue4)[26], 1);
        Assert.AreEqual((*tValue4)[27], 0);
        Assert.AreEqual((*tValue4)[28], 1);
        Assert.AreEqual((*tValue4)[29], 0);
        Assert.AreEqual((*tValue4)[30], 1);
        Assert.AreEqual((*tValue4)[31], 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB256Ref(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB256(default, default));

        Vector<bool>[] values = new Vector<bool>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<bool>* pValues = &values[0])
            {
                GenericsNative.AddVectorB256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorB256s(in values[0], values.Length));
    }
}
