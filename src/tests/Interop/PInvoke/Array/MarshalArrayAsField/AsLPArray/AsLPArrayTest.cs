// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace MarshalArrayAsField.LPArray;
public class Test
{
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArraySeqStructByVal([In]S_INTArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArraySeqStructByVal([In]S_UINTArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArraySeqStructByVal([In]S_SHORTArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArraySeqStructByVal([In]S_WORDArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArraySeqStructByVal([In]S_LONG64Array_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArraySeqStructByVal([In]S_ULONG64Array_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArraySeqStructByVal([In]S_DOUBLEArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArraySeqStructByVal([In]S_FLOATArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArraySeqStructByVal([In]S_BYTEArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArraySeqStructByVal([In]S_CHARArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArraySeqStructByVal([In]S_LPSTRArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArraySeqStructByVal([In]S_LPCSTRArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArraySeqStructByVal([In]S_BSTRArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArraySeqStructByVal([In]S_StructArray_Seq s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArraySeqClassByVal([In]C_INTArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArraySeqClassByVal([In]C_UINTArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArraySeqClassByVal([In]C_SHORTArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArraySeqClassByVal([In]C_WORDArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArraySeqClassByVal([In]C_LONG64Array_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArraySeqClassByVal([In]C_ULONG64Array_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArraySeqClassByVal([In]C_DOUBLEArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArraySeqClassByVal([In]C_FLOATArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArraySeqClassByVal([In]C_BYTEArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArraySeqClassByVal([In]C_CHARArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArraySeqClassByVal([In]C_LPSTRArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArraySeqClassByVal([In]C_LPCSTRArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArraySeqClassByVal([In]C_BSTRArray_Seq c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArraySeqClassByVal([In]C_StructArray_Seq c, [In]int size);

    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArrayExpStructByVal([In]S_INTArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArrayExpStructByVal([In]S_UINTArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArrayExpStructByVal([In]S_SHORTArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArrayExpStructByVal([In]S_WORDArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArrayExpStructByVal([In]S_LONG64Array_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArrayExpStructByVal([In]S_ULONG64Array_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArrayExpStructByVal([In]S_DOUBLEArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArrayExpStructByVal([In]S_FLOATArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArrayExpStructByVal([In]S_BYTEArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArrayExpStructByVal([In]S_CHARArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArrayExpStructByVal([In]S_LPSTRArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArrayExpStructByVal([In]S_LPCSTRArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArrayExpStructByVal([In]S_BSTRArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArrayExpStructByVal([In]S_StructArray_Exp s, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeIntArrayExpClassByVal([In]C_INTArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeUIntArrayExpClassByVal([In]C_UINTArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeShortArrayExpClassByVal([In]C_SHORTArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeWordArrayExpClassByVal([In]C_WORDArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLong64ArrayExpClassByVal([In]C_LONG64Array_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeULong64ArrayExpClassByVal([In]C_ULONG64Array_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeDoubleArrayExpClassByVal([In]C_DOUBLEArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeFloatArrayExpClassByVal([In]C_FLOATArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeByteArrayExpClassByVal([In]C_BYTEArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeCharArrayExpClassByVal([In]C_CHARArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPSTRArrayExpClassByVal([In]C_LPSTRArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeLPCSTRArrayExpClassByVal([In]C_LPCSTRArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeBSTRArrayExpClassByVal([In]C_BSTRArray_Exp c, [In]int size);
    [DllImport("MarshalArrayByValArrayNative", CallingConvention = CallingConvention.Cdecl)]
    static extern bool TakeStructArrayExpClassByVal([In]C_StructArray_Exp c, [In]int size);

    #region Helper

    internal const int ARRAY_SIZE = 100;

    static T[] InitArray<T>(int size)
    {
        T[] array = new T[size];

        for (int i = 0; i < array.Length; i++)
            array[i] = (T)Convert.ChangeType(i, typeof(T));

        return array;
    }

    static TestStruct[] InitStructArray(int size)
    {
        TestStruct[] array = new TestStruct[size];

        for (int i = 0; i < array.Length; i++)
        {
            array[i].x = i;
            array[i].d = i;
            array[i].l = i;
            array[i].str = i.ToString();
        }

        return array;
    }

    static bool[] InitBoolArray(int size)
    {
        bool[] array = new bool[size];

        for (int i = 0; i < array.Length; i++)
        {
            if (i % 2 == 0)
                array[i] = true;
            else
                array[i] = false;
        }

        return array;
    }

    #endregion

    static void RunTest1(string report)
    {
        Console.WriteLine(report);
        S_INTArray_Seq s1 = new S_INTArray_Seq();
        s1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeIntArraySeqStructByVal(s1, ARRAY_SIZE));

        S_UINTArray_Seq s2 = new S_UINTArray_Seq();
        s2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeUIntArraySeqStructByVal(s2, ARRAY_SIZE));

        S_SHORTArray_Seq s3 = new S_SHORTArray_Seq();
        s3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeShortArraySeqStructByVal(s3, ARRAY_SIZE));

        S_WORDArray_Seq s4 = new S_WORDArray_Seq();
        s4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeWordArraySeqStructByVal(s4, ARRAY_SIZE));

        S_LONG64Array_Seq s5 = new S_LONG64Array_Seq();
        s5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLong64ArraySeqStructByVal(s5, ARRAY_SIZE));

        S_ULONG64Array_Seq s6 = new S_ULONG64Array_Seq();
        s6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeULong64ArraySeqStructByVal(s6, ARRAY_SIZE));

        S_DOUBLEArray_Seq s7 = new S_DOUBLEArray_Seq();
        s7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeDoubleArraySeqStructByVal(s7, ARRAY_SIZE));

        S_FLOATArray_Seq s8 = new S_FLOATArray_Seq();
        s8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeFloatArraySeqStructByVal(s8, ARRAY_SIZE));

        S_BYTEArray_Seq s9 = new S_BYTEArray_Seq();
        s9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeByteArraySeqStructByVal(s9, ARRAY_SIZE));

        S_CHARArray_Seq s10 = new S_CHARArray_Seq();
        s10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeCharArraySeqStructByVal(s10, ARRAY_SIZE));

        S_LPSTRArray_Seq s11 = new S_LPSTRArray_Seq();
        s11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPSTRArraySeqStructByVal(s11, ARRAY_SIZE));

        S_LPCSTRArray_Seq s12 = new S_LPCSTRArray_Seq();
        s12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPCSTRArraySeqStructByVal(s12, ARRAY_SIZE));

        if (OperatingSystem.IsWindows())
        {
            S_BSTRArray_Seq s13 = new S_BSTRArray_Seq();
            s13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.Throws<TypeLoadException>(() => TakeBSTRArraySeqStructByVal(s13, ARRAY_SIZE));
        }

        S_StructArray_Seq s14 = new S_StructArray_Seq();
        s14.arr = InitStructArray(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeStructArraySeqStructByVal(s14, ARRAY_SIZE));
    }

    static void RunTest2(string report)
    {
        Console.WriteLine(report);
        C_INTArray_Seq c1 = new C_INTArray_Seq();
        c1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeIntArraySeqClassByVal(c1, ARRAY_SIZE));

        C_UINTArray_Seq c2 = new C_UINTArray_Seq();
        c2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeUIntArraySeqClassByVal(c2, ARRAY_SIZE));

        C_SHORTArray_Seq c3 = new C_SHORTArray_Seq();
        c3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeShortArraySeqClassByVal(c3, ARRAY_SIZE));

        C_WORDArray_Seq c4 = new C_WORDArray_Seq();
        c4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeWordArraySeqClassByVal(c4, ARRAY_SIZE));

        C_LONG64Array_Seq c5 = new C_LONG64Array_Seq();
        c5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLong64ArraySeqClassByVal(c5, ARRAY_SIZE));

        C_ULONG64Array_Seq c6 = new C_ULONG64Array_Seq();
        c6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeULong64ArraySeqClassByVal(c6, ARRAY_SIZE));

        C_DOUBLEArray_Seq c7 = new C_DOUBLEArray_Seq();
        c7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeDoubleArraySeqClassByVal(c7, ARRAY_SIZE));

        C_FLOATArray_Seq c8 = new C_FLOATArray_Seq();
        c8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeFloatArraySeqClassByVal(c8, ARRAY_SIZE));

        C_BYTEArray_Seq c9 = new C_BYTEArray_Seq();
        c9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeByteArraySeqClassByVal(c9, ARRAY_SIZE));

        C_CHARArray_Seq c10 = new C_CHARArray_Seq();
        c10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeCharArraySeqClassByVal(c10, ARRAY_SIZE));

        C_LPSTRArray_Seq c11 = new C_LPSTRArray_Seq();
        c11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPSTRArraySeqClassByVal(c11, ARRAY_SIZE));

        C_LPCSTRArray_Seq c12 = new C_LPCSTRArray_Seq();
        c12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPCSTRArraySeqClassByVal(c12, ARRAY_SIZE));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Seq c13 = new C_BSTRArray_Seq();
            c13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.Throws<TypeLoadException>(() => TakeBSTRArraySeqClassByVal(c13, ARRAY_SIZE));
        }

        C_StructArray_Seq c14 = new C_StructArray_Seq();
        c14.arr = InitStructArray(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeStructArraySeqClassByVal(c14, ARRAY_SIZE));
    }

    static void RunTest3(string report)
    {
        Console.WriteLine(report);

        S_INTArray_Exp s1 = new S_INTArray_Exp();
        s1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeIntArrayExpStructByVal(s1, ARRAY_SIZE));

        S_UINTArray_Exp s2 = new S_UINTArray_Exp();
        s2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeUIntArrayExpStructByVal(s2, ARRAY_SIZE));

        S_SHORTArray_Exp s3 = new S_SHORTArray_Exp();
        s3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeShortArrayExpStructByVal(s3, ARRAY_SIZE));

        S_WORDArray_Exp s4 = new S_WORDArray_Exp();
        s4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeWordArrayExpStructByVal(s4, ARRAY_SIZE));

        S_LONG64Array_Exp s5 = new S_LONG64Array_Exp();
        s5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLong64ArrayExpStructByVal(s5, ARRAY_SIZE));

        S_ULONG64Array_Exp s6 = new S_ULONG64Array_Exp();
        s6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeULong64ArrayExpStructByVal(s6, ARRAY_SIZE));

        S_DOUBLEArray_Exp s7 = new S_DOUBLEArray_Exp();
        s7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeDoubleArrayExpStructByVal(s7, ARRAY_SIZE));

        S_FLOATArray_Exp s8 = new S_FLOATArray_Exp();
        s8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeFloatArrayExpStructByVal(s8, ARRAY_SIZE));

        S_BYTEArray_Exp s9 = new S_BYTEArray_Exp();
        s9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeByteArrayExpStructByVal(s9, ARRAY_SIZE));

        S_CHARArray_Exp s10 = new S_CHARArray_Exp();
        s10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeCharArrayExpStructByVal(s10, ARRAY_SIZE));

        S_LPSTRArray_Exp s11 = new S_LPSTRArray_Exp();
        s11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPSTRArrayExpStructByVal(s11, ARRAY_SIZE));

        S_LPCSTRArray_Exp s12 = new S_LPCSTRArray_Exp();
        s12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPCSTRArrayExpStructByVal(s12, ARRAY_SIZE));

        if (OperatingSystem.IsWindows())
        {
            S_BSTRArray_Exp s13 = new S_BSTRArray_Exp();
            s13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.Throws<TypeLoadException>(() => TakeBSTRArrayExpStructByVal(s13, ARRAY_SIZE));
        }

        S_StructArray_Exp s14 = new S_StructArray_Exp();
        s14.arr = InitStructArray(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeStructArrayExpStructByVal(s14, ARRAY_SIZE));
    }

    static void RunTest4(string report)
    {
        Console.WriteLine(report);

        C_INTArray_Exp c1 = new C_INTArray_Exp();
        c1.arr = InitArray<int>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeIntArrayExpClassByVal(c1, ARRAY_SIZE));

        C_UINTArray_Exp c2 = new C_UINTArray_Exp();
        c2.arr = InitArray<uint>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeUIntArrayExpClassByVal(c2, ARRAY_SIZE));

        C_SHORTArray_Exp c3 = new C_SHORTArray_Exp();
        c3.arr = InitArray<short>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeShortArrayExpClassByVal(c3, ARRAY_SIZE));

        C_WORDArray_Exp c4 = new C_WORDArray_Exp();
        c4.arr = InitArray<ushort>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeWordArrayExpClassByVal(c4, ARRAY_SIZE));

        C_LONG64Array_Exp c5 = new C_LONG64Array_Exp();
        c5.arr = InitArray<long>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLong64ArrayExpClassByVal(c5, ARRAY_SIZE));

        C_ULONG64Array_Exp c6 = new C_ULONG64Array_Exp();
        c6.arr = InitArray<ulong>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeULong64ArrayExpClassByVal(c6, ARRAY_SIZE));

        C_DOUBLEArray_Exp c7 = new C_DOUBLEArray_Exp();
        c7.arr = InitArray<double>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeDoubleArrayExpClassByVal(c7, ARRAY_SIZE));

        C_FLOATArray_Exp c8 = new C_FLOATArray_Exp();
        c8.arr = InitArray<float>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeFloatArrayExpClassByVal(c8, ARRAY_SIZE));

        C_BYTEArray_Exp c9 = new C_BYTEArray_Exp();
        c9.arr = InitArray<byte>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeByteArrayExpClassByVal(c9, ARRAY_SIZE));

        C_CHARArray_Exp c10 = new C_CHARArray_Exp();
        c10.arr = InitArray<char>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeCharArrayExpClassByVal(c10, ARRAY_SIZE));

        C_LPSTRArray_Exp c11 = new C_LPSTRArray_Exp();
        c11.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPSTRArrayExpClassByVal(c11, ARRAY_SIZE));

        C_LPCSTRArray_Exp c12 = new C_LPCSTRArray_Exp();
        c12.arr = InitArray<string>(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeLPCSTRArrayExpClassByVal(c12, ARRAY_SIZE));

        if (OperatingSystem.IsWindows())
        {
            C_BSTRArray_Exp c13 = new C_BSTRArray_Exp();
            c13.arr = InitArray<string>(ARRAY_SIZE);
            Assert.Throws<TypeLoadException>(() => TakeBSTRArrayExpClassByVal(c13, ARRAY_SIZE));
        }

        C_StructArray_Exp c14 = new C_StructArray_Exp();
        c14.arr = InitStructArray(ARRAY_SIZE);
        Assert.Throws<TypeLoadException>(() => TakeStructArrayExpClassByVal(c14, ARRAY_SIZE));
    }

    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            RunTest1("RunTest 1 : Marshal Array In Sequential Struct As LPArray. ");
            RunTest2("RunTest 2 : Marshal Array In Sequential Class As LPArray. ");
            if (OperatingSystem.IsWindows())
            {
                RunTest3("RunTest 3 : Marshal Array In Explicit Struct As LPArray. ");
            }
            RunTest4("RunTest 4 : Marshal Array In Explicit Class As LPArray. ");
            Console.WriteLine("\nTest PASS.");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nTest FAIL: {e}");
            return 101;
        }
    }
}
