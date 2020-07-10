// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool> GetVector256B(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31, Vector256<bool>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31, out Vector256<bool> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool>* GetVector256BPtr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256BPtr")]
    public static extern ref readonly Vector256<bool> GetVector256BRef(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, bool e08, bool e09, bool e10, bool e11, bool e12, bool e13, bool e14, bool e15, bool e16, bool e17, bool e18, bool e19, bool e20, bool e21, bool e22, bool e23, bool e24, bool e25, bool e26, bool e27, bool e28, bool e29, bool e30, bool e31);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool> AddVector256B(Vector256<bool> lhs, Vector256<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool> AddVector256Bs(Vector256<bool>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool> AddVector256Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<bool> AddVector256Bs(in Vector256<bool> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector256B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256B(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Vector256<bool> value2;
        GenericsNative.GetVector256BOut(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, &value2);
        Vector256<byte> tValue2 = *(Vector256<byte>*)&value2;
        Assert.AreEqual(tValue2.GetElement(0), 1);
        Assert.AreEqual(tValue2.GetElement(1), 0);
        Assert.AreEqual(tValue2.GetElement(2), 1);
        Assert.AreEqual(tValue2.GetElement(3), 0);
        Assert.AreEqual(tValue2.GetElement(4), 1);
        Assert.AreEqual(tValue2.GetElement(5), 0);
        Assert.AreEqual(tValue2.GetElement(6), 1);
        Assert.AreEqual(tValue2.GetElement(7), 0);
        Assert.AreEqual(tValue2.GetElement(8), 1);
        Assert.AreEqual(tValue2.GetElement(9), 0);
        Assert.AreEqual(tValue2.GetElement(10), 1);
        Assert.AreEqual(tValue2.GetElement(11), 0);
        Assert.AreEqual(tValue2.GetElement(12), 1);
        Assert.AreEqual(tValue2.GetElement(13), 0);
        Assert.AreEqual(tValue2.GetElement(14), 1);
        Assert.AreEqual(tValue2.GetElement(15), 0);
        Assert.AreEqual(tValue2.GetElement(16), 1);
        Assert.AreEqual(tValue2.GetElement(17), 0);
        Assert.AreEqual(tValue2.GetElement(18), 1);
        Assert.AreEqual(tValue2.GetElement(19), 0);
        Assert.AreEqual(tValue2.GetElement(20), 1);
        Assert.AreEqual(tValue2.GetElement(21), 0);
        Assert.AreEqual(tValue2.GetElement(22), 1);
        Assert.AreEqual(tValue2.GetElement(23), 0);
        Assert.AreEqual(tValue2.GetElement(24), 1);
        Assert.AreEqual(tValue2.GetElement(25), 0);
        Assert.AreEqual(tValue2.GetElement(26), 1);
        Assert.AreEqual(tValue2.GetElement(27), 0);
        Assert.AreEqual(tValue2.GetElement(28), 1);
        Assert.AreEqual(tValue2.GetElement(29), 0);
        Assert.AreEqual(tValue2.GetElement(30), 1);
        Assert.AreEqual(tValue2.GetElement(31), 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256BOut(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, out Vector256<bool> value3));

        Vector256<bool>* value4 = GenericsNative.GetVector256BPtr(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false);
        Vector256<byte>* tValue4 = (Vector256<byte>*)value4;
        Assert.AreEqual(tValue4->GetElement(0), 1);
        Assert.AreEqual(tValue4->GetElement(1), 0);
        Assert.AreEqual(tValue4->GetElement(2), 1);
        Assert.AreEqual(tValue4->GetElement(3), 0);
        Assert.AreEqual(tValue4->GetElement(4), 1);
        Assert.AreEqual(tValue4->GetElement(5), 0);
        Assert.AreEqual(tValue4->GetElement(6), 1);
        Assert.AreEqual(tValue4->GetElement(7), 0);
        Assert.AreEqual(tValue4->GetElement(8), 1);
        Assert.AreEqual(tValue4->GetElement(9), 0);
        Assert.AreEqual(tValue4->GetElement(10), 1);
        Assert.AreEqual(tValue4->GetElement(11), 0);
        Assert.AreEqual(tValue4->GetElement(12), 1);
        Assert.AreEqual(tValue4->GetElement(13), 0);
        Assert.AreEqual(tValue4->GetElement(14), 1);
        Assert.AreEqual(tValue4->GetElement(15), 0);
        Assert.AreEqual(tValue4->GetElement(16), 1);
        Assert.AreEqual(tValue4->GetElement(17), 0);
        Assert.AreEqual(tValue4->GetElement(18), 1);
        Assert.AreEqual(tValue4->GetElement(19), 0);
        Assert.AreEqual(tValue4->GetElement(20), 1);
        Assert.AreEqual(tValue4->GetElement(21), 0);
        Assert.AreEqual(tValue4->GetElement(22), 1);
        Assert.AreEqual(tValue4->GetElement(23), 0);
        Assert.AreEqual(tValue4->GetElement(24), 1);
        Assert.AreEqual(tValue4->GetElement(25), 0);
        Assert.AreEqual(tValue4->GetElement(26), 1);
        Assert.AreEqual(tValue4->GetElement(27), 0);
        Assert.AreEqual(tValue4->GetElement(28), 1);
        Assert.AreEqual(tValue4->GetElement(29), 0);
        Assert.AreEqual(tValue4->GetElement(30), 1);
        Assert.AreEqual(tValue4->GetElement(31), 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256BRef(true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256B(default, default));

        Vector256<bool>[] values = new Vector256<bool>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<bool>* pValues = &values[0])
            {
                GenericsNative.AddVector256Bs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Bs(in values[0], values.Length));
    }
}
