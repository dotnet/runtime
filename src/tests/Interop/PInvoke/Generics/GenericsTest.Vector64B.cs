// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool> GetVector64B(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, Vector64<bool>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector64BOut(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07, out Vector64<bool> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool>* GetVector64BPtr(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector64BPtr")]
    public static extern ref readonly Vector64<bool> GetVector64BRef(bool e00, bool e01, bool e02, bool e03, bool e04, bool e05, bool e06, bool e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool> AddVector64B(Vector64<bool> lhs, Vector64<bool> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool> AddVector64Bs(Vector64<bool>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool> AddVector64Bs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector64<bool>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector64<bool> AddVector64Bs(in Vector64<bool> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector64B()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64B(true, false, true, false, true, false, true, false));

        Vector64<bool> value2;
        GenericsNative.GetVector64BOut(true, false, true, false, true, false, true, false, &value2);
        Vector64<byte> tValue2 = *(Vector64<byte>*)&value2;
        Assert.AreEqual(tValue2.GetElement(0), 1);
        Assert.AreEqual(tValue2.GetElement(1), 0);
        Assert.AreEqual(tValue2.GetElement(2), 1);
        Assert.AreEqual(tValue2.GetElement(3), 0);
        Assert.AreEqual(tValue2.GetElement(4), 1);
        Assert.AreEqual(tValue2.GetElement(5), 0);
        Assert.AreEqual(tValue2.GetElement(6), 1);
        Assert.AreEqual(tValue2.GetElement(7), 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64BOut(true, false, true, false, true, false, true, false, out Vector64<bool> value3));

        Vector64<bool>* value4 = GenericsNative.GetVector64BPtr(true, false, true, false, true, false, true, false);
        Vector64<byte>* tValue4 = (Vector64<byte>*)value4;
        Assert.AreEqual(tValue4->GetElement(0), 1);
        Assert.AreEqual(tValue4->GetElement(1), 0);
        Assert.AreEqual(tValue4->GetElement(2), 1);
        Assert.AreEqual(tValue4->GetElement(3), 0);
        Assert.AreEqual(tValue4->GetElement(4), 1);
        Assert.AreEqual(tValue4->GetElement(5), 0);
        Assert.AreEqual(tValue4->GetElement(6), 1);
        Assert.AreEqual(tValue4->GetElement(7), 0);

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector64BRef(true, false, true, false, true, false, true, false));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64B(default, default));

        Vector64<bool>[] values = new Vector64<bool>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector64<bool>* pValues = &values[0])
            {
                GenericsNative.AddVector64Bs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Bs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector64Bs(in values[0], values.Length));
    }
}
