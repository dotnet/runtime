// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

namespace MarshalArrayAsParam.LPArray;

public class ArrayMarshal
{
    private static int NumArrOfStructElements1 = 10;

    public struct TestStruct
    {
        public int x;
        public double d;
        public long l;
        public string str;
    }
    public struct S2
    {
        public int i32;
        public uint ui32;
        public short s1;
        public ushort us1;
        public Byte b;
        public SByte sb;
        public Int16 i16;
        public UInt16 ui16;
        public Int64 i64;
        public UInt64 ui64;
        public Single sgl;
        public Double d;
    }

    #region ByVal PInvoke method with no attributes applied
    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int([MarshalAs(UnmanagedType.LPArray)] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object([MarshalAs(UnmanagedType.LPArray)] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint([MarshalAs(UnmanagedType.LPArray)] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short([MarshalAs(UnmanagedType.LPArray)] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word([MarshalAs(UnmanagedType.LPArray)] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64([MarshalAs(UnmanagedType.LPArray)] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64([MarshalAs(UnmanagedType.LPArray)] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double([MarshalAs(UnmanagedType.LPArray)] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float([MarshalAs(UnmanagedType.LPArray)] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte([MarshalAs(UnmanagedType.LPArray)] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char([MarshalAs(UnmanagedType.LPArray)] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPCSTR([MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR([MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct([MarshalAs(UnmanagedType.LPArray)] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool([MarshalAs(UnmanagedType.LPArray)] bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool MarshalArrayOfStructAsLPArrayByVal([MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)] S2[] arrS2, int cActual, [In, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)]S2[] pExpect);

    #endregion

    #region ByVal PInvoke method with InAttribute applied
    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Int")]
    private static extern bool CStyle_Array_Int_In([In, MarshalAs(UnmanagedType.LPArray)] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Object")]
    private static extern bool CStyle_Array_Object_In(
        [In, MarshalAs(UnmanagedType.LPArray)] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Uint")]
    private static extern bool CStyle_Array_Uint_In([In, MarshalAs(UnmanagedType.LPArray)] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Short")]
    private static extern bool CStyle_Array_Short_In([In, MarshalAs(UnmanagedType.LPArray)] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Word")]
    private static extern bool CStyle_Array_Word_In([In, MarshalAs(UnmanagedType.LPArray)] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Long64")]
    private static extern bool CStyle_Array_Long64_In([In, MarshalAs(UnmanagedType.LPArray)] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_ULong64")]
    private static extern bool CStyle_Array_ULong64_In([In, MarshalAs(UnmanagedType.LPArray)] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Double")]
    private static extern bool CStyle_Array_Double_In([In, MarshalAs(UnmanagedType.LPArray)] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Float")]
    private static extern bool CStyle_Array_Float_In([In, MarshalAs(UnmanagedType.LPArray)] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Byte")]
    private static extern bool CStyle_Array_Byte_In([In, MarshalAs(UnmanagedType.LPArray)] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Char")]
    private static extern bool CStyle_Array_Char_In([In, MarshalAs(UnmanagedType.LPArray)] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_LPCSTR")]
    private static extern bool CStyle_Array_LPCSTR_In([In, MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_LPSTR")]
    private static extern bool CStyle_Array_LPSTR_In([In, MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Struct")]
    private static extern bool CStyle_Array_Struct_In([In, MarshalAs(UnmanagedType.LPArray)] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Bool")]
    private static extern bool CStyle_Array_Bool_In([In, MarshalAs(UnmanagedType.LPArray)] bool[] actual, int cActual);


    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "MarshalArrayOfStructAsLPArrayByVal")]
    private static extern bool MarshalArrayOfStructAsLPArrayByValIn([In, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)] S2[] arrS2, int cActual, [In, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)]S2[] pExpect);
    #endregion

    #region ByVal PInvoke method with InAttribute and OutAttribute applied
    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut_Null([In, Out, MarshalAs(UnmanagedType.LPArray)] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut_ZeroLength([In, Out, MarshalAs(UnmanagedType.LPArray)] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool_InOut([In, Out, MarshalAs(UnmanagedType.LPArray)] bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool MarshalArrayOfStructAsLPArrayByValInOut([In, Out, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)] S2[] arrS2, int cActual, [In, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)]S2[] pExpect);

    #endregion

    #region ByVal PInvoke method with OutAttribute applied
    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out([Out, MarshalAs(UnmanagedType.LPArray)] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out_Null([Out, MarshalAs(UnmanagedType.LPArray)] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out_ZeroLength([Out, MarshalAs(UnmanagedType.LPArray)] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object_Out([Out, MarshalAs(UnmanagedType.LPArray)] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint_Out([Out, MarshalAs(UnmanagedType.LPArray)] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short_Out([Out, MarshalAs(UnmanagedType.LPArray)] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word_Out([Out, MarshalAs(UnmanagedType.LPArray)] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64_Out([Out, MarshalAs(UnmanagedType.LPArray)] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64_Out([Out, MarshalAs(UnmanagedType.LPArray)] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double_Out([Out, MarshalAs(UnmanagedType.LPArray)] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float_Out([Out, MarshalAs(UnmanagedType.LPArray)] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte_Out([Out, MarshalAs(UnmanagedType.LPArray)] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char_Out([Out, MarshalAs(UnmanagedType.LPArray)] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR_Out([Out, MarshalAs(UnmanagedType.LPArray)] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct_Out([Out, MarshalAs(UnmanagedType.LPArray)] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool_Out([Out, MarshalAs(UnmanagedType.LPArray)] bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool MarshalArrayOfStructAsLPArrayByValOut([Out, MarshalAs(UnmanagedType.LPArray, SizeConst = ARRAY_SIZE)] S2[] arrS2, int cActual);

    #endregion

    #region Marshal ByVal
    private const int ARRAY_SIZE = 100;

    private static T[] InitArray<T>(int size)
    {
        T[] array = new T[size];

        for (int i = 0; i < array.Length; ++i)
            array[i] = (T)Convert.ChangeType(i, typeof(T));

        return array;
    }

    private static T[] CopyArray<T>(T[] srcarr, int size)
    {
        T[] array = new T[size];

        Array.Copy(srcarr, array, ARRAY_SIZE);
        return array;
    }

    private static bool[] InitBoolArray(int size)
    {
        bool[] array = new bool[size];

        for (int i = 0; i < array.Length; ++i)
        {
            if (i % 2 == 0)
                array[i] = true;
            else
                array[i] = false;
        }

        return array;
    }

    private static TestStruct[] InitStructArray(int size)
    {
        TestStruct[] array = new TestStruct[size];

        for (int i = 0; i < array.Length; ++i)
        {
            array[i].x = i;
            array[i].d = i;
            array[i].l = i;
            array[i].str = i.ToString();
        }

        return array;
    }
    private static void TestMarshalByVal_NoAttributes()
    {
        Console.WriteLine("ByVal marshaling CLR array as c-style-array no attributes");

        Assert.True(CStyle_Array_Int(InitArray<int>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Uint(InitArray<uint>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Short(InitArray<short>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Word(InitArray<ushort>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Long64(InitArray<long>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_ULong64(InitArray<ulong>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Double(InitArray<double>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Float(InitArray<float>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Byte(InitArray<byte>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Char(InitArray<char>(ARRAY_SIZE), ARRAY_SIZE));

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        strArr[strArr.Length / 2] = null;
        Assert.True(CStyle_Array_LPCSTR(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_LPSTR(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_Struct(InitStructArray(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Bool(InitBoolArray(ARRAY_SIZE), ARRAY_SIZE));

        if (PlatformDetection.IsBuiltInComEnabled)
        {
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            oArr[oArr.Length / 2] = null;
            Assert.True(CStyle_Array_Object(oArr, ARRAY_SIZE));
        }

    }

    private static void TestMarshalByVal_In()
    {
        Console.WriteLine("ByVal marshaling  CLR array as c-style-array with InAttribute applied");

        Assert.True(CStyle_Array_Int_In(InitArray<int>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Uint_In(InitArray<uint>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Short_In(InitArray<short>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Word_In(InitArray<ushort>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Long64_In(InitArray<long>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_ULong64_In(InitArray<ulong>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Double_In(InitArray<double>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Float_In(InitArray<float>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Byte_In(InitArray<byte>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Char_In(InitArray<char>(ARRAY_SIZE), ARRAY_SIZE));

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        strArr[strArr.Length / 2] = null;
        Assert.True(CStyle_Array_LPCSTR_In(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_LPSTR_In(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_Struct_In(InitStructArray(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Bool_In(InitBoolArray(ARRAY_SIZE), ARRAY_SIZE));

        if (PlatformDetection.IsBuiltInComEnabled)
        {
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            oArr[oArr.Length / 2] = null;
            Assert.True(CStyle_Array_Object_In(oArr, ARRAY_SIZE));
        }
    }
    #endregion

    #region Marshal InOut ByVal
    private static void TestMarshalByVal_InOut()
    {
        Console.WriteLine("By value marshaling CLR array as c-style-array with InAttribute and OutAttribute applied");
        int[] iArr = InitArray<int>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Int_InOut(iArr, ARRAY_SIZE));
        Assert.True(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)));

        int[] iArrNull = null;
        Assert.True(CStyle_Array_Int_InOut_Null(iArrNull));
        Assert.Null(iArrNull);

        int[] iArrLength0 = InitArray<int>(0);
        Assert.True(CStyle_Array_Int_InOut_ZeroLength(iArrLength0));
        Assert.Empty(iArrLength0);

        uint[] uiArr = InitArray<uint>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Uint_InOut(uiArr, ARRAY_SIZE));
        Assert.True(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)));

        short[] sArr = InitArray<short>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Short_InOut(sArr, ARRAY_SIZE));
        Assert.True(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)));

        ushort[] usArr = InitArray<ushort>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Word_InOut(usArr, ARRAY_SIZE));
        Assert.True(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)));

        long[] lArr = InitArray<long>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Long64_InOut(lArr, ARRAY_SIZE));
        Assert.True(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)));

        ulong[] ulArr = InitArray<ulong>(ARRAY_SIZE);
        Assert.True(CStyle_Array_ULong64_InOut(ulArr, ARRAY_SIZE));
        Assert.True(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)));

        double[] dArr = InitArray<double>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Double_InOut(dArr, ARRAY_SIZE));
        Assert.True(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)));

        float[] fArr = InitArray<float>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Float_InOut(fArr, ARRAY_SIZE));
        Assert.True(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)));

        byte[] bArr = InitArray<byte>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Byte_InOut(bArr, ARRAY_SIZE));
        Assert.True(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)));

        char[] cArr = InitArray<char>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Char_InOut(cArr, ARRAY_SIZE));
        Assert.True(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)));

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        strArr[strArr.Length / 2] = null;
        Assert.True(CStyle_Array_LPSTR_InOut(strArr, ARRAY_SIZE));

        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.True(Equals<string>(strArr, expectedArr));

        TestStruct[] tsArr = InitStructArray(ARRAY_SIZE);
        Assert.True(CStyle_Array_Struct_InOut(tsArr, ARRAY_SIZE));
        Assert.True(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)));

        bool[] boolArr = InitBoolArray(ARRAY_SIZE);
        Assert.True(CStyle_Array_Bool_InOut(boolArr, ARRAY_SIZE));
        Assert.True(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)));
    }

    private static bool Equals<T>(T[] arr1, T[] arr2)
    {
        if (arr1 == null && arr2 == null)
            return true;
        else if (arr1 == null && arr2 != null)
            return false;
        else if (arr1 != null && arr2 == null)
            return false;
        else if (arr1.Length != arr2.Length)
            return false;

        for (int i = 0; i < arr2.Length; ++i)
        {
            if (!Object.Equals(arr1[i], arr2[i]))
            {
                Console.WriteLine("Array marshaling error, Index: {0} , Expected:{1}, Actual:{2}", i, arr2[i], arr1[i]);
                return false;
            }
        }
        return true;
    }

    private static T[] GetExpectedOutArray<T>(int size)
    {
        T[] array = new T[size];
        for (int i = array.Length - 1; i >= 0; --i)
            array[i] = (T)Convert.ChangeType(array.Length - 1 - i, typeof(T));
        return array;
    }

    private static bool[] GetExpectedOutBoolArray(int size)
    {
        bool[] array = new bool[size];
        for (int i = 0; i < array.Length; ++i)
        {
            if (i % 2 != 0)
                array[i] = true;
            else
                array[i] = false;
        }
        return array;
    }

    private static TestStruct[] GetExpectedOutStructArray(int size)
    {
        TestStruct[] array = new TestStruct[size];
        for (int i = array.Length - 1; i >= 0; --i)
        {
            int v = array.Length - 1 - i;
            array[i].x = v;
            array[i].d = v;
            array[i].l = v;
            array[i].str = v.ToString();
        }
        return array;
    }
    #endregion

    #region Marshal InOut ByVal
    private static void TestMarshalByVal_Out()
    {
        Console.WriteLine("By value marshaling CLR array as c-style-array with OutAttribute applied");

        int[] iArr = new int[ARRAY_SIZE];
        Assert.True(CStyle_Array_Int_Out(iArr, ARRAY_SIZE));
        Assert.True(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)));

        int[] iArrNull = null;
        Assert.True(CStyle_Array_Int_Out_Null(iArrNull));
        Assert.Null(iArrNull);

        int[] iArrLength0 = new int[0];
        Assert.True(CStyle_Array_Int_Out_ZeroLength(iArrLength0));
        Assert.Empty(iArrLength0);

        uint[] uiArr = new uint[ARRAY_SIZE];
        Assert.True(CStyle_Array_Uint_Out(uiArr, ARRAY_SIZE));
        Assert.True(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)));

        short[] sArr = new short[ARRAY_SIZE];
        Assert.True(CStyle_Array_Short_Out(sArr, ARRAY_SIZE));
        Assert.True(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)));

        ushort[] usArr = new ushort[ARRAY_SIZE];
        Assert.True(CStyle_Array_Word_Out(usArr, ARRAY_SIZE));
        Assert.True(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)));

        long[] lArr = new long[ARRAY_SIZE];
        Assert.True(CStyle_Array_Long64_Out(lArr, ARRAY_SIZE));
        Assert.True(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)));

        ulong[] ulArr = new ulong[ARRAY_SIZE];
        Assert.True(CStyle_Array_ULong64_Out(ulArr, ARRAY_SIZE));
        Assert.True(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)));

        double[] dArr = new double[ARRAY_SIZE];
        Assert.True(CStyle_Array_Double_Out(dArr, ARRAY_SIZE));
        Assert.True(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)));

        float[] fArr = new float[ARRAY_SIZE];
        Assert.True(CStyle_Array_Float_Out(fArr, ARRAY_SIZE));
        Assert.True(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)));

        byte[] bArr = new byte[ARRAY_SIZE];
        Assert.True(CStyle_Array_Byte_Out(bArr, ARRAY_SIZE));
        Assert.True(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)));

        char[] cArr = new char[ARRAY_SIZE];
        Assert.True(CStyle_Array_Char_Out(cArr, ARRAY_SIZE));
        Assert.True(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)));

        string[] strArr = new string[ARRAY_SIZE];
        Assert.True(CStyle_Array_LPSTR_Out(strArr, ARRAY_SIZE));

        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.True(Equals<string>(strArr, expectedArr));

        TestStruct[] tsArr = new TestStruct[ARRAY_SIZE];
        Assert.True(CStyle_Array_Struct_Out(tsArr, ARRAY_SIZE));
        Assert.True(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)));

        bool[] boolArr = new bool[ARRAY_SIZE];
        Assert.True(CStyle_Array_Bool_Out(boolArr, ARRAY_SIZE));
        Assert.True(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)));

        if (PlatformDetection.IsBuiltInComEnabled)
        {
            object[] oArr = new object[ARRAY_SIZE];
            Assert.True(CStyle_Array_Object_Out(oArr, ARRAY_SIZE));
            object[] expectedOArr = GetExpectedOutArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            expectedOArr[expectedOArr.Length / 2 - 1] = null;
            Assert.True(Equals<object>(oArr, expectedOArr));
        }

    }
    #endregion

    #region methods for S2 struct array
    public static S2[] NewS2arr(int NumArrElements, int i32, uint ui32, short s1, ushort us1,
                   Byte b, SByte sb, Int16 i16, UInt16 ui16, Int64 i64, UInt64 ui64, Single sgl, Double d)
    {
        S2[] arrS2 = new S2[NumArrElements];
        for (int i = 0; i < NumArrElements; i++)
        {
            arrS2[i].i32 = i32;
            arrS2[i].ui32 = ui32;
            arrS2[i].s1 = s1;
            arrS2[i].us1 = us1;
            arrS2[i].b = b;
            arrS2[i].sb = sb;
            arrS2[i].i16 = i16;
            arrS2[i].ui16 = ui16;
            arrS2[i].i64 = i64;
            arrS2[i].ui64 = ui64;
            arrS2[i].sgl = sgl;
            arrS2[i].d = d;
        }
        return arrS2;
    }
    public static void PrintS2arr(string name, S2[] arrS2)
    {
        for (int i = 0; i < arrS2.Length; i++)
        {
            Console.WriteLine("{0}[{1}].i32 = {2}", name, i, arrS2[i].i32);
            Console.WriteLine("{0}[{1}].ui32 = {2}", name, i, arrS2[i].ui32);
            Console.WriteLine("{0}[{1}].s1 = {2}", name, i, arrS2[i].s1);
            Console.WriteLine("{0}[{1}].us1 = {2}", name, i, arrS2[i].us1);
            Console.WriteLine("{0}[{1}].b = {2}", name, i, arrS2[i].b);
            Console.WriteLine("{0}[{1}].sb = {2}", name, i, arrS2[i].sb);
            Console.WriteLine("{0}[{1}].i16 = {2}", name, i, arrS2[i].i16);
            Console.WriteLine("{0}[{1}].ui16 = {2}", name, i, arrS2[i].ui16);
            Console.WriteLine("{0}[{1}].i64 = {2}", name, i, arrS2[i].i64);
            Console.WriteLine("{0}[{1}].ui64 = {2}", name, i, arrS2[i].ui64);
            Console.WriteLine("{0}[{1}].sgl = {2}", name, i, arrS2[i].sgl);
            Console.WriteLine("{0}[{1}].d = {2}", name, i, arrS2[i].d);
        }
    }
    public static bool IsCorrect(S2[] actual, S2[] expected)
    {
        if (actual.Length != expected.Length)
        {
            return false;
        }
        for (int i = 0; i < actual.Length; i++)
        {
            if (actual[i].i32 != expected[i].i32 || actual[i].ui32 != expected[i].ui32 || actual[i].s1 != expected[i].s1 || actual[i].us1 != expected[i].us1 ||
                actual[i].b != expected[i].b || actual[i].sb != expected[i].sb || actual[i].i16 != expected[i].i16 ||
                actual[i].ui16 != expected[i].ui16 || actual[i].i64 != expected[i].i64 || actual[i].ui64 != expected[i].ui64 ||
                actual[i].sgl != expected[i].sgl || actual[i].d != expected[i].d)
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try{
            TestMarshalByVal_NoAttributes();
            TestMarshalByVal_In();
            TestMarshalByVal_InOut();
            TestMarshalByVal_Out();

            Console.WriteLine("\nTest PASS.");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"\nTEST FAIL: {e}");
            return 101;
        }
    }
}

public unsafe class ArrayPinningTests
{
    private const string NativeLibraryName = "MarshalArrayLPArrayNative";

    private enum SByteEnum : sbyte { }
    private enum ByteEnum : byte { }
    private enum Int16Enum : short { }
    private enum UInt16Enum : ushort { }
    private enum Int32Enum : int { }
    private enum UInt32Enum : uint { }
    private enum Int64Enum : long { }
    private enum UInt64Enum : ulong { }

    public enum CharacterMarshalling
    {
        Unicode,
        AnsiAsInt16,
        AnsiAsUInt16,
        Ansi,
        UnicodeAsInt8,
        UnicodeAsUInt8
    }

    public static bool IsSupported => !PlatformDetection.PlatformDoesNotSupportNativeTestAssets;

    public static IEnumerable<object[]> ArrayCases()
    {
        foreach (int length in new[] { -1, 0, 1, 5, 21 })
        {
            yield return new object[] { length, false };
            yield return new object[] { length, true };
        }
    }

    public static IEnumerable<object[]> CharacterCases()
    {
        foreach (CharacterMarshalling kind in Enum.GetValues<CharacterMarshalling>())
        {
            foreach (object[] testCase in ArrayCases())
            {
                yield return new object[] { kind, testCase[0], testCase[1] };
            }
        }
    }

    [ConditionalTheory(typeof(ArrayPinningTests), nameof(IsSupported))]
    [MemberData(nameof(ArrayCases))]
    public static void EnumArraysArePinned(int length, bool useDelegate)
    {
        nint target = useDelegate ? GetArrayElementReverser() : 0;
        VerifyArrayPinning(CreateEnumArray<SByteEnum>(length), sizeof(sbyte), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<SByteEnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<ByteEnum>(length), sizeof(byte), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<ByteEnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<Int16Enum>(length), sizeof(short), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<Int16EnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<UInt16Enum>(length), sizeof(ushort), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<UInt16EnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<Int32Enum>(length), sizeof(int), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<Int32EnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<UInt32Enum>(length), sizeof(uint), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<UInt32EnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<Int64Enum>(length), sizeof(long), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<Int64EnumReverser>(target).Invoke : ReverseArrayElements);
        VerifyArrayPinning(CreateEnumArray<UInt64Enum>(length), sizeof(ulong), true,
            useDelegate ? Marshal.GetDelegateForFunctionPointer<UInt64EnumReverser>(target).Invoke : ReverseArrayElements);
    }

    [ConditionalTheory(typeof(ArrayPinningTests), nameof(IsSupported))]
    [MemberData(nameof(CharacterCases))]
    [SkipOnMono("Mono character-array marshalling uses different pinning semantics.")]
    public static void CharacterArraysUseSelectedRepresentation(CharacterMarshalling kind, int length, bool useDelegate)
    {
        bool pinned = kind is CharacterMarshalling.Unicode or CharacterMarshalling.AnsiAsInt16 or CharacterMarshalling.AnsiAsUInt16;
        char[] values = length < 0 ? null : new char[length];
        if (values is not null)
        {
            ReadOnlySpan<char> pattern = pinned ? ['A', '\u03A9', '\uD83D', '\uDE00', '\0'] : ['A', 'B', 'C', 'D', 'E'];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = pattern[i % pattern.Length];
            }
        }

        nint target = useDelegate ? GetArrayElementReverser() : 0;
        Func<char[], int, int, nint> reverse = kind switch
        {
            CharacterMarshalling.Unicode => useDelegate ? Marshal.GetDelegateForFunctionPointer<UnicodeCharReverser>(target).Invoke : ReverseUnicodeChars,
            CharacterMarshalling.AnsiAsInt16 => useDelegate ? Marshal.GetDelegateForFunctionPointer<AnsiCharAsInt16Reverser>(target).Invoke : ReverseAnsiCharsAsInt16,
            CharacterMarshalling.AnsiAsUInt16 => useDelegate ? Marshal.GetDelegateForFunctionPointer<AnsiCharAsUInt16Reverser>(target).Invoke : ReverseAnsiCharsAsUInt16,
            CharacterMarshalling.Ansi => useDelegate ? Marshal.GetDelegateForFunctionPointer<AnsiCharReverser>(target).Invoke : ReverseAnsiChars,
            CharacterMarshalling.UnicodeAsInt8 => useDelegate ? Marshal.GetDelegateForFunctionPointer<UnicodeCharAsInt8Reverser>(target).Invoke : ReverseUnicodeCharsAsInt8,
            CharacterMarshalling.UnicodeAsUInt8 => useDelegate ? Marshal.GetDelegateForFunctionPointer<UnicodeCharAsUInt8Reverser>(target).Invoke : ReverseUnicodeCharsAsUInt8,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        VerifyArrayPinning(values, pinned ? sizeof(char) : sizeof(byte), pinned, reverse);
    }

    [ConditionalTheory(typeof(ArrayPinningTests), nameof(IsSupported))]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public static void BooleanArraysAreNotPinned(bool oneByte, bool useDelegate)
    {
        bool[] values = [true, false, true, false, false];
        nint target = useDelegate ? GetArrayElementReverser() : 0;
        Func<bool[], int, int, nint> reverse = oneByte
            ? (useDelegate ? Marshal.GetDelegateForFunctionPointer<ByteBoolReverser>(target).Invoke : ReverseByteBooleans)
            : (useDelegate ? Marshal.GetDelegateForFunctionPointer<BoolReverser>(target).Invoke : ReverseBooleans);
        VerifyArrayPinning(values, oneByte ? sizeof(byte) : sizeof(int), false, reverse);
    }

    private static T[] CreateEnumArray<T>(int length) where T : unmanaged, Enum
    {
        if (length < 0)
        {
            return null;
        }

        T[] values = new T[length];
        Span<byte> bytes = MemoryMarshal.AsBytes(values.AsSpan());
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(0x81 + i * 17));
        }

        return values;
    }

    private static void VerifyArrayPinning<T>(T[] values, int nativeElementSize, bool pinned, Func<T[], int, int, nint> reverse) where T : unmanaged
    {
        if (values is null)
        {
            Assert.Equal(nint.Zero, reverse(null, 0, nativeElementSize));
            return;
        }

        T[] expected = (T[])values.Clone();
        if (pinned)
        {
            Array.Reverse(expected);
        }

        fixed (T* address = &MemoryMarshal.GetArrayDataReference(values))
        {
            nint actual = reverse(values, values.Length, nativeElementSize);
            if (pinned)
            {
                Assert.Equal((nint)address, actual);
            }
            else
            {
                Assert.NotEqual((nint)address, actual);
            }
        }

        Assert.Equal(expected, values);
    }

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetArrayElementReverser();

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(SByteEnum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(ByteEnum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(Int16Enum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(UInt16Enum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(Int32Enum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(UInt32Enum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(Int64Enum[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseArrayElements(UInt64Enum[] values, int count, int elementSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint SByteEnumReverser(SByteEnum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint ByteEnumReverser(ByteEnum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint Int16EnumReverser(Int16Enum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint UInt16EnumReverser(UInt16Enum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint Int32EnumReverser(Int32Enum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint UInt32EnumReverser(UInt32Enum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint Int64EnumReverser(Int64Enum[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint UInt64EnumReverser(UInt64Enum[] values, int count, int elementSize);

    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern nint ReverseUnicodeChars(char[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern nint ReverseAnsiCharsAsInt16([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I2)] char[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern nint ReverseAnsiCharsAsUInt16([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern nint ReverseAnsiChars(char[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern nint ReverseUnicodeCharsAsInt8([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] char[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern nint ReverseUnicodeCharsAsUInt8([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] char[] values, int count, int elementSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate nint UnicodeCharReverser(char[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate nint AnsiCharAsInt16Reverser([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I2)] char[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate nint AnsiCharAsUInt16Reverser([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate nint AnsiCharReverser(char[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate nint UnicodeCharAsInt8Reverser([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] char[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private delegate nint UnicodeCharAsUInt8Reverser([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] char[] values, int count, int elementSize);

    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseBooleans(bool[] values, int count, int elementSize);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReverseArrayElements), CallingConvention = CallingConvention.Cdecl)]
    private static extern nint ReverseByteBooleans([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] bool[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint BoolReverser(bool[] values, int count, int elementSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint ByteBoolReverser([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] bool[] values, int count, int elementSize);
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/124219", typeof(PlatformDetection), nameof(PlatformDetection.IsWasm))]
public unsafe class PointerArrayTests
{
    private const string NativeLibraryName = "MarshalArrayLPArrayNative";
    private const int ArrayLength = 5;

    public enum ElementKind
    {
        Byte,
        Int32,
        Int64,
        Single,
        Boolean,
        Char,
        UnicodeChar,
        Void,
        Structure,
        Pointer,
        IntPtr,
        UIntPtr,
        UnmanagedFunction,
        ManagedFunction
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct Pointee
    {
    }

    public static bool IsSupported => !PlatformDetection.PlatformDoesNotSupportNativeTestAssets;

    public static IEnumerable<object[]> ArrayCases()
    {
        foreach (ElementKind kind in Enum.GetValues<ElementKind>())
        {
            foreach (int length in new[] { -1, 0, 1, 4, ArrayLength, 21 })
            {
                yield return new object[] { kind, length };
            }
        }
    }

    public static IEnumerable<object[]> DelegateCases()
    {
        foreach (object[] testCase in ArrayCases())
        {
            if ((ElementKind)testCase[0] is ElementKind.Byte or ElementKind.IntPtr or ElementKind.UIntPtr or ElementKind.UnmanagedFunction)
            {
                yield return testCase;
            }
        }
    }

    public static IEnumerable<object[]> ElementKinds()
    {
        foreach (ElementKind kind in Enum.GetValues<ElementKind>())
        {
            yield return new object[] { kind };
        }
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [MemberData(nameof(ArrayCases))]
    public static void PinArray(ElementKind kind, int length)
    {
        Array values = CreateArray(kind, length);
        nuint[] expected = CreateValues(kind, length);
        Initialize(values, expected);

        ref byte data = ref Unsafe.NullRef<byte>();
        if (values is not null)
        {
            data = ref MemoryMarshal.GetArrayDataReference(values);
        }

        fixed (byte* address = &data)
        {
            Assert.Equal((nint)address, GetAddress(kind, values, null, 0));
        }
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [MemberData(nameof(DelegateCases))]
    public static void PinArrayThroughDelegate(ElementKind kind, int length)
    {
        Array values = CreateArray(kind, length);
        Initialize(values, CreateValues(kind, length));
        nint target = (nint)(delegate* unmanaged[Cdecl]<nuint*, nint>)&GetAddressManaged;

        ref byte data = ref Unsafe.NullRef<byte>();
        if (values is not null)
        {
            data = ref MemoryMarshal.GetArrayDataReference(values);
        }

        fixed (byte* address = &data)
        {
            nint actual = kind switch
            {
                ElementKind.Byte => Marshal.GetDelegateForFunctionPointer<ByteArrayDelegate>(target)((byte*[])values),
                ElementKind.IntPtr => Marshal.GetDelegateForFunctionPointer<IntPtrArrayDelegate>(target)((nint[])values),
                ElementKind.UIntPtr => Marshal.GetDelegateForFunctionPointer<UIntPtrArrayDelegate>(target)((nuint[])values),
                ElementKind.UnmanagedFunction => Marshal.GetDelegateForFunctionPointer<FunctionArrayDelegate>(target)((delegate* unmanaged[Cdecl]<nint, nint>[])values),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            Assert.Equal((nint)address, actual);
        }
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [MemberData(nameof(ElementKinds))]
    public static void PinArrayAcrossCollection(ElementKind kind)
    {
        Array values = CreateArray(kind, ArrayLength);
        nuint[] expected = CreateValues(kind, values.Length);
        Initialize(values, expected);
        using (GCHandle<Array> handle = new(values))
        {
            Assert.NotEqual(nint.Zero, GetAddress(kind, values, &GetAddressAfterCollection, GCHandle<Array>.ToIntPtr(handle)));
            Assert.True(Contents(values).SequenceEqual(expected));
        }
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [MemberData(nameof(ArrayCases))]
    [SkipOnMono("Mono passes byref blittable arrays directly instead of copying them.")]
    public static void CopyArrayByRef(ElementKind kind, int length)
    {
        Array values = CreateArray(kind, length);
        nuint[] expected = CreateValues(kind, length);
        Initialize(values, expected);

        ref byte data = ref Unsafe.NullRef<byte>();
        if (values is not null)
        {
            data = ref MemoryMarshal.GetArrayDataReference(values);
        }

        fixed (byte* address = &data)
        fixed (nuint* expectedAddress = expected)
        {
            Assert.Equal(1, ReverseByRef(kind, ref values, Math.Max(0, length), length == -1 ? null : expectedAddress, (nuint*)address));
        }

        AssertResult(kind, length, values, expected, reversed: true);
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [InlineData(false)]
    [InlineData(true)]
    [SkipOnMono("Mono passes byref blittable arrays directly instead of copying them.")]
    public static void CopyInOnlyArrayByRef(bool functionPointers)
    {
        ElementKind kind = functionPointers ? ElementKind.UnmanagedFunction : ElementKind.Byte;
        Array values = CreateArray(kind, ArrayLength);
        Array original = values;
        nuint[] expected = CreateValues(kind, values.Length);
        Initialize(values, expected);

        fixed (byte* address = &MemoryMarshal.GetArrayDataReference(values))
        fixed (nuint* expectedAddress = expected)
        {
            if (functionPointers)
            {
                delegate* unmanaged[Cdecl]<nint, nint>[] typed = (delegate* unmanaged[Cdecl]<nint, nint>[])values;
                Assert.Equal(1, ReversePointerArrayIn(ref typed, typed.Length, expectedAddress, (nuint*)address));
                values = typed;
            }
            else
            {
                byte*[] typed = (byte*[])values;
                Assert.Equal(1, ReversePointerArrayIn(ref typed, typed.Length, expectedAddress, (nuint*)address));
                values = typed;
            }
        }

        Assert.Same(original, values);
        AssertResult(kind, values.Length, values, expected, reversed: false);
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [MemberData(nameof(DelegateCases))]
    [SkipOnMono("Pointer-array copy-back, including function-pointer layout, has not been validated on Mono.")]
    public static void CopyOutArray(ElementKind kind, int length)
    {
        nuint[] expected = CreateValues(kind, length);
        Array values;
        fixed (nuint* expectedAddress = expected)
        {
            nuint* source = length == -1 ? null : expectedAddress;
            int count = Math.Max(0, length);
            switch (kind)
            {
                case ElementKind.Byte:
                    Assert.Equal(1, CreatePointerArray(out byte*[] bytes, count, source));
                    values = bytes;
                    break;
                case ElementKind.IntPtr:
                    Assert.Equal(1, CreatePointerArray(out nint[] integers, count, source));
                    values = integers;
                    break;
                case ElementKind.UIntPtr:
                    Assert.Equal(1, CreatePointerArray(out nuint[] unsignedIntegers, count, source));
                    values = unsignedIntegers;
                    break;
                case ElementKind.UnmanagedFunction:
                    Assert.Equal(1, CreatePointerArray(out delegate* unmanaged[Cdecl]<nint, nint>[] functions, count, source));
                    values = functions;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        AssertResult(kind, length, values, expected, reversed: false);
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public static void PinArrayDirectionAttributes(int direction)
    {
        byte*[] values = new byte*[ArrayLength];
        nuint[] expected = CreateValues(ElementKind.Byte, values.Length);
        Initialize(values, expected);

        fixed (byte** address = values)
        fixed (nuint* expectedAddress = expected)
        {
            int result = direction switch
            {
                0 => ReversePointerArray(values, values.Length, expectedAddress, (nuint*)address, 1),
                1 => ReversePointerArrayIn(values, values.Length, expectedAddress, (nuint*)address, 1),
                2 => ReversePointerArrayOut(values, values.Length, expectedAddress, (nuint*)address, 1),
                3 => ReversePointerArrayInOut(values, values.Length, expectedAddress, (nuint*)address, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
            Assert.Equal(1, result);
        }

        AssertResult(ElementKind.Byte, values.Length, values, expected, reversed: true);
    }

    [ConditionalTheory(typeof(PointerArrayTests), nameof(IsSupported))]
    [InlineData(false, -1)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 4)]
    [InlineData(false, ArrayLength)]
    [InlineData(false, 21)]
    [InlineData(true, -1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 4)]
    [InlineData(true, ArrayLength)]
    [InlineData(true, 21)]
    [SkipOnMono("Reverse pointer-array marshalling, including function-pointer layout, has not been validated on Mono.")]
    public static void CopyArrayInReversePInvoke(bool functionPointers, int length)
    {
        ElementKind kind = functionPointers ? ElementKind.UnmanagedFunction : ElementKind.Byte;
        nuint[] expected = CreateValues(kind, length);
        nuint[] values = (nuint[])expected.Clone();
        fixed (nuint* address = values)
        fixed (nuint* expectedAddress = expected)
        {
            nuint* nativeValues = length == -1 ? null : address;
            nuint* nativeExpected = length == -1 ? null : expectedAddress;
            int count = Math.Max(0, length);
            int result = functionPointers
                ? CallPointerArrayCallback(ReverseFunctionArray, nativeValues, count, nativeExpected)
                : CallPointerArrayCallback(ReverseByteArray, nativeValues, count, nativeExpected);
            Assert.Equal(1, result);
        }
    }

    private static Array CreateArray(ElementKind kind, int length)
    {
        if (length == -1)
        {
            return null;
        }

        return kind switch
        {
            ElementKind.Byte => new byte*[length],
            ElementKind.Int32 => new int*[length],
            ElementKind.Int64 => new long*[length],
            ElementKind.Single => new float*[length],
            ElementKind.Boolean => new bool*[length],
            ElementKind.Char or ElementKind.UnicodeChar => new char*[length],
            ElementKind.Void => new void*[length],
            ElementKind.Structure => new Pointee*[length],
            ElementKind.Pointer => new byte**[length],
            ElementKind.IntPtr => new nint[length],
            ElementKind.UIntPtr => new nuint[length],
            ElementKind.UnmanagedFunction => new delegate* unmanaged[Cdecl]<nint, nint>[length],
            ElementKind.ManagedFunction => new delegate*<nint, nint>[length],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static nuint[] CreateValues(ElementKind kind, int length)
    {
        nuint[] values = new nuint[Math.Max(1, length)];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (i % 3) == 2 ? 0 : kind switch
            {
                ElementKind.UnmanagedFunction => (i & 1) == 0
                    ? (nuint)(delegate* unmanaged[Cdecl]<nint, nint>)&UnmanagedIdentity
                    : (nuint)(delegate* unmanaged[Cdecl]<nint, nint>)&UnmanagedNegate,
                ElementKind.ManagedFunction => (i & 1) == 0
                    ? (nuint)(delegate*<nint, nint>)&ManagedIdentity
                    : (nuint)(delegate*<nint, nint>)&ManagedNegate,
                _ => (IntPtr.Size == 8 ? unchecked((nuint)0x1234567810203040UL) : 0x10203040u) + (nuint)(i * 0x01010101)
            };
        }

        if (values.Length >= ArrayLength)
        {
            values[ArrayLength - 1] = 21;
        }

        return values;
    }

    private static Span<nuint> Contents(Array values)
        => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, nuint>(ref MemoryMarshal.GetArrayDataReference(values)), values.Length);

    private static void Initialize(Array values, nuint[] expected)
    {
        if (values is not null)
        {
            expected.AsSpan(0, values.Length).CopyTo(Contents(values));
        }
    }

    private static void AssertResult(ElementKind kind, int length, Array values, nuint[] expected, bool reversed)
    {
        if (length == -1)
        {
            Assert.Null(values);
            return;
        }

        Assert.NotNull(values);
        Assert.Equal(CreateArray(kind, 0).GetType(), values.GetType());
        Assert.Equal(length, values.Length);
        Span<nuint> contents = Contents(values);
        for (int i = 0; i < length; i++)
        {
            Assert.Equal(expected[reversed ? length - i - 1 : i], contents[i]);
        }
    }

    private static nint GetAddress(ElementKind kind, Array values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context)
        => kind switch
        {
            ElementKind.Byte => GetPointerArrayAddress((byte*[])values, callback, context),
            ElementKind.Int32 => GetPointerArrayAddress((int*[])values, callback, context),
            ElementKind.Int64 => GetPointerArrayAddress((long*[])values, callback, context),
            ElementKind.Single => GetPointerArrayAddress((float*[])values, callback, context),
            ElementKind.Boolean => GetPointerArrayAddress((bool*[])values, callback, context),
            ElementKind.Char => GetPointerArrayAddress((char*[])values, callback, context),
            ElementKind.UnicodeChar => GetPointerArrayAddressUnicode((char*[])values, callback, context),
            ElementKind.Void => GetPointerArrayAddress((void*[])values, callback, context),
            ElementKind.Structure => GetPointerArrayAddress((Pointee*[])values, callback, context),
            ElementKind.Pointer => GetPointerArrayAddress((byte**[])values, callback, context),
            ElementKind.IntPtr => GetPointerArrayAddress((nint[])values, callback, context),
            ElementKind.UIntPtr => GetPointerArrayAddress((nuint[])values, callback, context),
            ElementKind.UnmanagedFunction => GetPointerArrayAddress((delegate* unmanaged[Cdecl]<nint, nint>[])values, callback, context),
            ElementKind.ManagedFunction => GetPointerArrayAddress((delegate*<nint, nint>[])values, callback, context),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private delegate int ByRefArrayCall<TArray>(ref TArray values, int count, nuint* expected, nuint* original);

    private static int CallByRef<TArray>(ByRefArrayCall<TArray> call, ref Array values, int count, nuint* expected, nuint* original)
        where TArray : class
    {
        TArray typed = (TArray)(object)values;
        int result = call(ref typed, count, expected, original);
        values = (Array)(object)typed;
        return result;
    }

    private static int ReverseByRef(ElementKind kind, ref Array values, int count, nuint* expected, nuint* original)
        => kind switch
        {
            ElementKind.Byte => CallByRef<byte*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Int32 => CallByRef<int*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Int64 => CallByRef<long*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Single => CallByRef<float*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Boolean => CallByRef<bool*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Char => CallByRef<char*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.UnicodeChar => CallByRef<char*[]>(ReversePointerArrayByRefUnicode, ref values, count, expected, original),
            ElementKind.Void => CallByRef<void*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Structure => CallByRef<Pointee*[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.Pointer => CallByRef<byte**[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.IntPtr => CallByRef<nint[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.UIntPtr => CallByRef<nuint[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.UnmanagedFunction => CallByRef<delegate* unmanaged[Cdecl]<nint, nint>[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            ElementKind.ManagedFunction => CallByRef<delegate*<nint, nint>[]>(ReversePointerArrayByRef, ref values, count, expected, original),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static nint GetAddressManaged(nuint* values) => (nint)values;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static nuint* GetAddressAfterCollection(nuint* values, nint context)
    {
        Array array = GCHandle<Array>.FromIntPtr(context).Target;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        fixed (byte* address = &MemoryMarshal.GetArrayDataReference(array))
        {
            return (nuint*)address;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static nint UnmanagedIdentity(nint value) => value;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static nint UnmanagedNegate(nint value) => -value;

    private static nint ManagedIdentity(nint value) => value;
    private static nint ManagedNegate(nint value) => -value;

    private static int ReverseByteArray(byte*[] values, int count, nuint* expected)
        => ReverseCallback(values, typeof(byte*[]), count, expected);

    private static int ReverseFunctionArray(delegate* unmanaged[Cdecl]<nint, nint>[] values, int count, nuint* expected)
        => ReverseCallback(values, typeof(delegate* unmanaged[Cdecl]<nint, nint>[]), count, expected);

    private static int ReverseCallback(Array values, Type expectedType, int count, nuint* expected)
    {
        if (values is null)
        {
            return expected is null ? 1 : 0;
        }

        if (values.GetType() != expectedType || values.Length != count || !Contents(values).SequenceEqual(new ReadOnlySpan<nuint>(expected, count)))
        {
            return 0;
        }

        Contents(values).Reverse();
        return 1;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint ByteArrayDelegate(byte*[] values);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint IntPtrArrayDelegate(nint[] values);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint UIntPtrArrayDelegate(nuint[] values);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint FunctionArrayDelegate(delegate* unmanaged[Cdecl]<nint, nint>[] values);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ReverseByteArrayDelegate([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte*[] values, int count, nuint* expected);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ReverseFunctionArrayDelegate([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] delegate* unmanaged[Cdecl]<nint, nint>[] values, int count, nuint* expected);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(byte*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(int*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(long*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(float*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(bool*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(char*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, EntryPoint = nameof(GetPointerArrayAddress), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern nint GetPointerArrayAddressUnicode(char*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(void*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(Pointee*[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(byte**[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(nint[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(nuint[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(delegate* unmanaged[Cdecl]<nint, nint>[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint GetPointerArrayAddress(delegate*<nint, nint>[] values, delegate* unmanaged[Cdecl]<nuint*, nint, nuint*> callback, nint context);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref byte*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref int*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref long*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref float*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref bool*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref char*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArrayByRef), CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    private static extern int ReversePointerArrayByRefUnicode([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref char*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref void*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref Pointee*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref byte**[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref nint[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref nuint[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref delegate* unmanaged[Cdecl]<nint, nint>[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayByRef([In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref delegate*<nint, nint>[] values, int count, nuint* expected, nuint* original);

    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArrayByRef), CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayIn([In] ref byte*[] values, int count, nuint* expected, nuint* original);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArrayByRef), CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayIn([In] ref delegate* unmanaged[Cdecl]<nint, nint>[] values, int count, nuint* expected, nuint* original);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CreatePointerArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte*[] values, int count, nuint* expected);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CreatePointerArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out nint[] values, int count, nuint* expected);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CreatePointerArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out nuint[] values, int count, nuint* expected);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CreatePointerArray([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out delegate* unmanaged[Cdecl]<nint, nint>[] values, int count, nuint* expected);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArray(byte*[] values, int count, nuint* expected, nuint* original, int pinned);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArray), CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayIn([In] byte*[] values, int count, nuint* expected, nuint* original, int pinned);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArray), CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayOut([Out] byte*[] values, int count, nuint* expected, nuint* original, int pinned);
    [DllImport(NativeLibraryName, EntryPoint = nameof(ReversePointerArray), CallingConvention = CallingConvention.Cdecl)]
    private static extern int ReversePointerArrayInOut([In, Out] byte*[] values, int count, nuint* expected, nuint* original, int pinned);

    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CallPointerArrayCallback(ReverseByteArrayDelegate callback, nuint* values, int count, nuint* expected);
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int CallPointerArrayCallback(ReverseFunctionArrayDelegate callback, nuint* values, int count, nuint* expected);
}
