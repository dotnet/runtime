// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> GetVector128C([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, Vector128<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector128COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, out Vector128<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char>* GetVector128CPtr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector128CPtr")]
    public static extern ref readonly Vector128<char> GetVector128CRef([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128C(Vector128<char> lhs, Vector128<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs(Vector128<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector128<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector128<char> AddVector128Cs(in Vector128<char> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector128C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128C('0', '1', '2', '3', '4', '5', '6', '7'));

        Vector128<char> value2;
        GenericsNative.GetVector128COut('0', '1', '2', '3', '4', '5', '6', '7', &value2);
        Vector128<short> tValue2 = *(Vector128<short>*)&value2;
        Assert.AreEqual(tValue2.GetElement(0), (short)'0');
        Assert.AreEqual(tValue2.GetElement(1), (short)'1');
        Assert.AreEqual(tValue2.GetElement(2), (short)'2');
        Assert.AreEqual(tValue2.GetElement(3), (short)'3');
        Assert.AreEqual(tValue2.GetElement(4), (short)'4');
        Assert.AreEqual(tValue2.GetElement(5), (short)'5');
        Assert.AreEqual(tValue2.GetElement(6), (short)'6');
        Assert.AreEqual(tValue2.GetElement(7), (short)'7');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128COut('0', '1', '2', '3', '4', '5', '6', '7', out Vector128<char> value3));

        Vector128<char>* value4 = GenericsNative.GetVector128CPtr('0', '1', '2', '3', '4', '5', '6', '7');
        Vector128<short>* tValue4 = (Vector128<short>*)value4;
        Assert.AreEqual(tValue4->GetElement(0), (short)'0');
        Assert.AreEqual(tValue4->GetElement(1), (short)'1');
        Assert.AreEqual(tValue4->GetElement(2), (short)'2');
        Assert.AreEqual(tValue4->GetElement(3), (short)'3');
        Assert.AreEqual(tValue4->GetElement(4), (short)'4');
        Assert.AreEqual(tValue4->GetElement(5), (short)'5');
        Assert.AreEqual(tValue4->GetElement(6), (short)'6');
        Assert.AreEqual(tValue4->GetElement(7), (short)'7');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector128CRef('0', '1', '2', '3', '4', '5', '6', '7'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128C(default, default));

        Vector128<char>[] values = new Vector128<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector128<char>* pValues = &values[0])
            {
                GenericsNative.AddVector128Cs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector128Cs(in values[0], values.Length));
    }
}
