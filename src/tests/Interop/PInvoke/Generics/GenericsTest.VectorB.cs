// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

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

public unsafe partial class GenericsTest
{
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/177", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void TestVectorB()
    {
        if (Vector<byte>.Count == 32)
        {
            TestVectorB256();
        }
        else
        {
            Assert.Equal(16, Vector<byte>.Count);
            TestVectorB128();
        }
    }

    public static void TestVectorB128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB128(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector<bool> value2;
        GenericsNative.GetVectorB128Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector<byte> tValue2 = *(Vector<byte>*)&value2;
        Assert.Equal(1, tValue2[0]);
        Assert.Equal(0, tValue2[1]);
        Assert.Equal(1, tValue2[2]);
        Assert.Equal(0, tValue2[3]);
        Assert.Equal(1, tValue2[4]);
        Assert.Equal(0, tValue2[5]);
        Assert.Equal(1, tValue2[6]);
        Assert.Equal(0, tValue2[7]);
        Assert.Equal(1, tValue2[8]);
        Assert.Equal(0, tValue2[9]);
        Assert.Equal(1, tValue2[10]);
        Assert.Equal(0, tValue2[11]);
        Assert.Equal(1, tValue2[12]);
        Assert.Equal(0, tValue2[13]);
        Assert.Equal(1, tValue2[14]);
        Assert.Equal(0, tValue2[15]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB128Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector<bool> value3));

        Vector<bool>* value4 = GenericsNative.GetVectorB128Ptr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector<byte>* tValue4 = (Vector<byte>*)value4;
        Assert.Equal(1, (*tValue4)[0]);
        Assert.Equal(0, (*tValue4)[1]);
        Assert.Equal(1, (*tValue4)[2]);
        Assert.Equal(0, (*tValue4)[3]);
        Assert.Equal(1, (*tValue4)[4]);
        Assert.Equal(0, (*tValue4)[5]);
        Assert.Equal(1, (*tValue4)[6]);
        Assert.Equal(0, (*tValue4)[7]);
        Assert.Equal(1, (*tValue4)[8]);
        Assert.Equal(0, (*tValue4)[9]);
        Assert.Equal(1, (*tValue4)[10]);
        Assert.Equal(0, (*tValue4)[11]);
        Assert.Equal(1, (*tValue4)[12]);
        Assert.Equal(0, (*tValue4)[13]);
        Assert.Equal(1, (*tValue4)[14]);
        Assert.Equal(0, (*tValue4)[15]);

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

    public static void TestVectorB256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB256(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector<bool> value2;
        GenericsNative.GetVectorB256Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector<byte> tValue2 = *(Vector<byte>*)&value2;
        Assert.Equal(1, tValue2[0]);
        Assert.Equal(0, tValue2[1]);
        Assert.Equal(1, tValue2[2]);
        Assert.Equal(0, tValue2[3]);
        Assert.Equal(1, tValue2[4]);
        Assert.Equal(0, tValue2[5]);
        Assert.Equal(1, tValue2[6]);
        Assert.Equal(0, tValue2[7]);
        Assert.Equal(1, tValue2[8]);
        Assert.Equal(0, tValue2[9]);
        Assert.Equal(1, tValue2[10]);
        Assert.Equal(0, tValue2[11]);
        Assert.Equal(1, tValue2[12]);
        Assert.Equal(0, tValue2[13]);
        Assert.Equal(1, tValue2[14]);
        Assert.Equal(0, tValue2[15]);
        Assert.Equal(1, tValue2[16]);
        Assert.Equal(0, tValue2[17]);
        Assert.Equal(1, tValue2[18]);
        Assert.Equal(0, tValue2[19]);
        Assert.Equal(1, tValue2[20]);
        Assert.Equal(0, tValue2[21]);
        Assert.Equal(1, tValue2[22]);
        Assert.Equal(0, tValue2[23]);
        Assert.Equal(1, tValue2[24]);
        Assert.Equal(0, tValue2[25]);
        Assert.Equal(1, tValue2[26]);
        Assert.Equal(0, tValue2[27]);
        Assert.Equal(1, tValue2[28]);
        Assert.Equal(0, tValue2[29]);
        Assert.Equal(1, tValue2[30]);
        Assert.Equal(0, tValue2[31]);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorB256Out(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector<bool> value3));

        Vector<bool>* value4 = GenericsNative.GetVectorB256Ptr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector<byte>* tValue4 = (Vector<byte>*)value4;
        Assert.Equal(1, (*tValue4)[0]);
        Assert.Equal(0, (*tValue4)[1]);
        Assert.Equal(1, (*tValue4)[2]);
        Assert.Equal(0, (*tValue4)[3]);
        Assert.Equal(1, (*tValue4)[4]);
        Assert.Equal(0, (*tValue4)[5]);
        Assert.Equal(1, (*tValue4)[6]);
        Assert.Equal(0, (*tValue4)[7]);
        Assert.Equal(1, (*tValue4)[8]);
        Assert.Equal(0, (*tValue4)[9]);
        Assert.Equal(1, (*tValue4)[10]);
        Assert.Equal(0, (*tValue4)[11]);
        Assert.Equal(1, (*tValue4)[12]);
        Assert.Equal(0, (*tValue4)[13]);
        Assert.Equal(1, (*tValue4)[14]);
        Assert.Equal(0, (*tValue4)[15]);
        Assert.Equal(1, (*tValue4)[16]);
        Assert.Equal(0, (*tValue4)[17]);
        Assert.Equal(1, (*tValue4)[18]);
        Assert.Equal(0, (*tValue4)[19]);
        Assert.Equal(1, (*tValue4)[20]);
        Assert.Equal(0, (*tValue4)[21]);
        Assert.Equal(1, (*tValue4)[22]);
        Assert.Equal(0, (*tValue4)[23]);
        Assert.Equal(1, (*tValue4)[24]);
        Assert.Equal(0, (*tValue4)[25]);
        Assert.Equal(1, (*tValue4)[26]);
        Assert.Equal(0, (*tValue4)[27]);
        Assert.Equal(1, (*tValue4)[28]);
        Assert.Equal(0, (*tValue4)[29]);
        Assert.Equal(1, (*tValue4)[30]);
        Assert.Equal(0, (*tValue4)[31]);

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
