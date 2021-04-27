// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char> GetVector256C([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15, Vector256<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVector256COut([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15, out Vector256<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char>* GetVector256CPtr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVector256CPtr")]
    public static extern ref readonly Vector256<char> GetVector256CRef([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char> AddVector256C(Vector256<char> lhs, Vector256<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char> AddVector256Cs(Vector256<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char> AddVector256Cs([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector256<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector256<char> AddVector256Cs(in Vector256<char> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVector256C()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256C('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'));

        Vector256<char> value2;
        GenericsNative.GetVector256COut('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', &value2);
        Vector256<short> tValue2 = *(Vector256<short>*)&value2;
        Assert.AreEqual(tValue2.GetElement(0), (short)'0');
        Assert.AreEqual(tValue2.GetElement(1), (short)'1');
        Assert.AreEqual(tValue2.GetElement(2), (short)'2');
        Assert.AreEqual(tValue2.GetElement(3), (short)'3');
        Assert.AreEqual(tValue2.GetElement(4), (short)'4');
        Assert.AreEqual(tValue2.GetElement(5), (short)'5');
        Assert.AreEqual(tValue2.GetElement(6), (short)'6');
        Assert.AreEqual(tValue2.GetElement(7), (short)'7');
        Assert.AreEqual(tValue2.GetElement(8), (short)'8');
        Assert.AreEqual(tValue2.GetElement(9), (short)'9');
        Assert.AreEqual(tValue2.GetElement(10), (short)'A');
        Assert.AreEqual(tValue2.GetElement(11), (short)'B');
        Assert.AreEqual(tValue2.GetElement(12), (short)'C');
        Assert.AreEqual(tValue2.GetElement(13), (short)'D');
        Assert.AreEqual(tValue2.GetElement(14), (short)'E');
        Assert.AreEqual(tValue2.GetElement(15), (short)'F');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256COut('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', out Vector256<char> value3));

        Vector256<char>* value4 = GenericsNative.GetVector256CPtr('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F');
        Vector256<short>* tValue4 = (Vector256<short>*)value4;
        Assert.AreEqual(tValue4->GetElement(0), (short)'0');
        Assert.AreEqual(tValue4->GetElement(1), (short)'1');
        Assert.AreEqual(tValue4->GetElement(2), (short)'2');
        Assert.AreEqual(tValue4->GetElement(3), (short)'3');
        Assert.AreEqual(tValue4->GetElement(4), (short)'4');
        Assert.AreEqual(tValue4->GetElement(5), (short)'5');
        Assert.AreEqual(tValue4->GetElement(6), (short)'6');
        Assert.AreEqual(tValue4->GetElement(7), (short)'7');
        Assert.AreEqual(tValue4->GetElement(8), (short)'8');
        Assert.AreEqual(tValue4->GetElement(9), (short)'9');
        Assert.AreEqual(tValue4->GetElement(10), (short)'A');
        Assert.AreEqual(tValue4->GetElement(11), (short)'B');
        Assert.AreEqual(tValue4->GetElement(12), (short)'C');
        Assert.AreEqual(tValue4->GetElement(13), (short)'D');
        Assert.AreEqual(tValue4->GetElement(14), (short)'E');
        Assert.AreEqual(tValue4->GetElement(15), (short)'F');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVector256CRef('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256C(default, default));

        Vector256<char>[] values = new Vector256<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector256<char>* pValues = &values[0])
            {
                GenericsNative.AddVector256Cs(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Cs(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVector256Cs(in values[0], values.Length));
    }
}
