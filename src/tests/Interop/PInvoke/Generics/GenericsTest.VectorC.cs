// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using TestLibrary;

unsafe partial class GenericsNative
{
    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> GetVectorC128([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> GetVectorC256([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorC128Out([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, Vector<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorC256Out([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15, Vector<char>* value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorC128Out([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, out Vector<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern void GetVectorC256Out([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15, out Vector<char> value);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char>* GetVectorC128Ptr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char>* GetVectorC256Ptr([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorC128Ptr")]
    public static extern ref readonly Vector<char> GetVectorC128Ref([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07);

    [DllImport(nameof(GenericsNative), EntryPoint = "GetVectorC256Ptr")]
    public static extern ref readonly Vector<char> GetVectorC256Ref([MarshalAs(UnmanagedType.U2)]char e00, [MarshalAs(UnmanagedType.U2)]char e01, [MarshalAs(UnmanagedType.U2)]char e02, [MarshalAs(UnmanagedType.U2)]char e03, [MarshalAs(UnmanagedType.U2)]char e04, [MarshalAs(UnmanagedType.U2)]char e05, [MarshalAs(UnmanagedType.U2)]char e06, [MarshalAs(UnmanagedType.U2)]char e07, [MarshalAs(UnmanagedType.U2)]char e08, [MarshalAs(UnmanagedType.U2)]char e09, [MarshalAs(UnmanagedType.U2)]char e10, [MarshalAs(UnmanagedType.U2)]char e11, [MarshalAs(UnmanagedType.U2)]char e12, [MarshalAs(UnmanagedType.U2)]char e13, [MarshalAs(UnmanagedType.U2)]char e14, [MarshalAs(UnmanagedType.U2)]char e15);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC128(Vector<char> lhs, Vector<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC256(Vector<char> lhs, Vector<char> rhs);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC128s(Vector<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC256s(Vector<char>* pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC128s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC256s([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Vector<char>[] pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC128s(in Vector<char> pValues, int count);

    [DllImport(nameof(GenericsNative))]
    public static extern Vector<char> AddVectorC256s(in Vector<char> pValues, int count);
}

unsafe partial class GenericsTest
{
    private static void TestVectorC()
    {
        if (Vector<short>.Count == 16)
        {
            TestVectorC256();
        }
        else
        {
            Assert.AreEqual(Vector<short>.Count, 8);
            TestVectorC128();
        }
    }

    private static void TestVectorC128()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC128('0', '1', '2', '3', '4', '5', '6', '7'));

        Vector<char> value2;
        GenericsNative.GetVectorC128Out('0', '1', '2', '3', '4', '5', '6', '7', &value2);
        Vector<short> tValue2 = *(Vector<short>*)&value2;
        Assert.AreEqual(tValue2[0], (short)'0');
        Assert.AreEqual(tValue2[1], (short)'1');
        Assert.AreEqual(tValue2[2], (short)'2');
        Assert.AreEqual(tValue2[3], (short)'3');
        Assert.AreEqual(tValue2[4], (short)'4');
        Assert.AreEqual(tValue2[5], (short)'5');
        Assert.AreEqual(tValue2[6], (short)'6');
        Assert.AreEqual(tValue2[7], (short)'7');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC128Out('0', '1', '2', '3', '4', '5', '6', '7', out Vector<char> value3));

        Vector<char>* value4 = GenericsNative.GetVectorC128Ptr('0', '1', '2', '3', '4', '5', '6', '7');
        Vector<short>* tValue4 = (Vector<short>*)value4;
        Assert.AreEqual((*tValue4)[0], (short)'0');
        Assert.AreEqual((*tValue4)[1], (short)'1');
        Assert.AreEqual((*tValue4)[2], (short)'2');
        Assert.AreEqual((*tValue4)[3], (short)'3');
        Assert.AreEqual((*tValue4)[4], (short)'4');
        Assert.AreEqual((*tValue4)[5], (short)'5');
        Assert.AreEqual((*tValue4)[6], (short)'6');
        Assert.AreEqual((*tValue4)[7], (short)'7');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC128Ref('0', '1', '2', '3', '4', '5', '6', '7'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC128(default, default));

        Vector<char>[] values = new Vector<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<char>* pValues = &values[0])
            {
                GenericsNative.AddVectorC128s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC128s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC128s(in values[0], values.Length));
    }

    private static void TestVectorC256()
    {
        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC256('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'));

        Vector<char> value2;
        GenericsNative.GetVectorC256Out('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', &value2);
        Vector<short> tValue2 = *(Vector<short>*)&value2;
        Assert.AreEqual(tValue2[0], (short)'0');
        Assert.AreEqual(tValue2[1], (short)'1');
        Assert.AreEqual(tValue2[2], (short)'2');
        Assert.AreEqual(tValue2[3], (short)'3');
        Assert.AreEqual(tValue2[4], (short)'4');
        Assert.AreEqual(tValue2[5], (short)'5');
        Assert.AreEqual(tValue2[6], (short)'6');
        Assert.AreEqual(tValue2[7], (short)'7');
        Assert.AreEqual(tValue2[8], (short)'8');
        Assert.AreEqual(tValue2[9], (short)'9');
        Assert.AreEqual(tValue2[10], (short)'A');
        Assert.AreEqual(tValue2[11], (short)'B');
        Assert.AreEqual(tValue2[12], (short)'C');
        Assert.AreEqual(tValue2[13], (short)'D');
        Assert.AreEqual(tValue2[14], (short)'E');
        Assert.AreEqual(tValue2[15], (short)'F');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC256Out('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', out Vector<char> value3));

        Vector<char>* value4 = GenericsNative.GetVectorC256Ptr('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F');
        Vector<short>* tValue4 = (Vector<short>*)value4;
        Assert.AreEqual((*tValue4)[0], (short)'0');
        Assert.AreEqual((*tValue4)[1], (short)'1');
        Assert.AreEqual((*tValue4)[2], (short)'2');
        Assert.AreEqual((*tValue4)[3], (short)'3');
        Assert.AreEqual((*tValue4)[4], (short)'4');
        Assert.AreEqual((*tValue4)[5], (short)'5');
        Assert.AreEqual((*tValue4)[6], (short)'6');
        Assert.AreEqual((*tValue4)[7], (short)'7');
        Assert.AreEqual((*tValue4)[8], (short)'8');
        Assert.AreEqual((*tValue4)[9], (short)'9');
        Assert.AreEqual((*tValue4)[10], (short)'A');
        Assert.AreEqual((*tValue4)[11], (short)'B');
        Assert.AreEqual((*tValue4)[12], (short)'C');
        Assert.AreEqual((*tValue4)[13], (short)'D');
        Assert.AreEqual((*tValue4)[14], (short)'E');
        Assert.AreEqual((*tValue4)[15], (short)'F');

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.GetVectorC256Ref('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC256(default, default));

        Vector<char>[] values = new Vector<char>[] {
            default,
            value2,
            default,
            *value4,
            default,
        };

        Assert.Throws<MarshalDirectiveException>(() => {
            fixed (Vector<char>* pValues = &values[0])
            {
                GenericsNative.AddVectorC256s(pValues, values.Length);
            }
        });

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC256s(values, values.Length));

        Assert.Throws<MarshalDirectiveException>(() => GenericsNative.AddVectorC256s(in values[0], values.Length));
    }
}
