// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace MarshalArrayAsParam.Default;

public class ArrayMarshal
{
    public struct TestStruct
    {
        public int x;
        public double d;
        public long l;
        public string str;
    }

    #region No attributes applied

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int(int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint(uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short(short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word(ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64(long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64(ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double(double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float(float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte(byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char(char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPCSTR(string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR(string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct(TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool(bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object(object[] actual, int cActual);
    #endregion

    #region InAttribute attribute applied

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Int")]
    private static extern bool CStyle_Array_Int_In([In]int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Uint")]
    private static extern bool CStyle_Array_Uint_In([In]uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Short")]
    private static extern bool CStyle_Array_Short_In([In]short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Word")]
    private static extern bool CStyle_Array_Word_In([In]ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Long64")]
    private static extern bool CStyle_Array_Long64_In([In]long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_ULong64")]
    private static extern bool CStyle_Array_ULong64_In([In]ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Double")]
    private static extern bool CStyle_Array_Double_In([In]double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Float")]
    private static extern bool CStyle_Array_Float_In([In]float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Byte")]
    private static extern bool CStyle_Array_Byte_In([In]byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Char")]
    private static extern bool CStyle_Array_Char_In([In]char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_LPCSTR")]
    private static extern bool CStyle_Array_LPCSTR_In([In]string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_LPSTR")]
    private static extern bool CStyle_Array_LPSTR_In([In]string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Struct")]
    private static extern bool CStyle_Array_Struct_In([In]TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Bool")]
    private static extern bool CStyle_Array_Bool_In([In]bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative", EntryPoint = "CStyle_Array_Object")]
    private static extern bool CStyle_Array_Object_In([In]object[] actual, int cActual);
    #endregion

    #region InAttribute and OutAttribute attributes applied

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut(
        [In, Out] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut_Null(
        [In, Out] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_InOut_ZeroLength(
        [In, Out] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object_InOut(
        [In, Out] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint_InOut(
        [In, Out] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short_InOut(
        [In, Out] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word_InOut(
        [In, Out] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64_InOut(
        [In, Out] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64_InOut(
        [In, Out] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double_InOut(
        [In, Out] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float_InOut(
        [In, Out] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte_InOut(
        [In, Out] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char_InOut(
        [In, Out] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR_InOut(
        [In, Out] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct_InOut(
        [In, Out] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool_InOut(
        [In, Out] bool[] actual, int cActual);
    #endregion

    #region OutAttribute attributes applied

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out(
        [Out] int[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out_Null(
        [Out] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Int_Out_ZeroLength(
        [Out] int[] actual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Object_Out(
        [Out] object[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Uint_Out(
        [Out] uint[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Short_Out(
        [Out] short[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Word_Out(
        [Out] ushort[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Long64_Out(
        [Out] long[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_ULong64_Out(
        [Out] ulong[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Double_Out(
        [Out] double[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Float_Out(
        [Out] float[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Byte_Out(
        [Out] byte[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Char_Out(
        [Out] char[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_LPSTR_Out(
        [Out] string[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Struct_Out(
        [Out] TestStruct[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern bool CStyle_Array_Bool_Out(
        [Out] bool[] actual, int cActual);

    [DllImport("MarshalArrayLPArrayNative")]
    private static extern int Get_Multidimensional_Array_Sum(int[,] array, int rows, int columns);
    #endregion

    #region Marshal ByVal

    private const int ARRAY_SIZE = 100;

    private const int ROWS = 3;

    private const int COLUMNS = 2;

    private static T[] InitArray<T>(int size)
    {
        T[] array = new T[size];

        for (int i = 0; i < array.Length; ++i)
            array[i] = (T)Convert.ChangeType(i, typeof(T));

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

    private static int[,] InitMultidimensionalBlittableArray(int rows, int columns)
    {
        int[,] array = new int[rows, columns];

        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                array[i, j] = i * j;
            }
        }
        return array;
    }

    private static void TestMarshalByVal_NoAttributes()
    {
        Console.WriteLine("ByVal marshaling CLR array as c-style-array no attributes");

        Assert.True(CStyle_Array_Int(InitArray<int>(ARRAY_SIZE), ARRAY_SIZE));
        Assert.True(CStyle_Array_Uint(InitArray<uint>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Uint") ;
        Assert.True(CStyle_Array_Short(InitArray<short>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Short");
        Assert.True(CStyle_Array_Word(InitArray<ushort>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Word");
        Assert.True(CStyle_Array_Long64(InitArray<long>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Long64");
        Assert.True(CStyle_Array_ULong64(InitArray<ulong>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_ULong64");
        Assert.True(CStyle_Array_Double(InitArray<double>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Double");
        Assert.True(CStyle_Array_Float(InitArray<float>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Float");
        Assert.True(CStyle_Array_Byte(InitArray<byte>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Byte");
        Assert.True(CStyle_Array_Char(InitArray<char>(ARRAY_SIZE), ARRAY_SIZE));

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        strArr[strArr.Length / 2] = null;
        Assert.True(CStyle_Array_LPCSTR(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_LPSTR(strArr, ARRAY_SIZE));
        Assert.True(CStyle_Array_Struct(InitStructArray(ARRAY_SIZE), ARRAY_SIZE));

        Assert.True(CStyle_Array_Bool(InitBoolArray(ARRAY_SIZE), ARRAY_SIZE));

        if (TestLibrary.PlatformDetection.IsBuiltInComEnabled)
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
        if (TestLibrary.PlatformDetection.IsBuiltInComEnabled)
        {
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            oArr[oArr.Length / 2] = null;
            Assert.True(CStyle_Array_Object_In(oArr, ARRAY_SIZE));
        }
    }

    #endregion

    #region Marshal InOut ByVal

    private static void TestMarshalInOut_ByVal()
    {
        Console.WriteLine("By value marshaling CLR array as c-style-array with InAttribute and OutAttribute applied");
        Console.WriteLine("CStyle_Array_Int_InOut");
        int[] iArr = InitArray<int>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Int_InOut(iArr, ARRAY_SIZE));
        Assert.True(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Int_InOut_Null");
        int[] iArrNull = null;
        Assert.True(CStyle_Array_Int_InOut_Null(iArrNull));
        Assert.Null(iArrNull);

        Console.WriteLine("CStyle_Array_Int_InOut_ZeroLength");
        int[] iArrLength0 = InitArray<int>(0);
        Assert.True(CStyle_Array_Int_InOut_ZeroLength(iArrLength0));
        Assert.Empty(iArrLength0);

        Console.WriteLine("CStyle_Array_Uint_InOut");
        uint[] uiArr = InitArray<uint>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Uint_InOut(uiArr, ARRAY_SIZE));
        Assert.True(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Short_InOut");
        short[] sArr = InitArray<short>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Short_InOut(sArr, ARRAY_SIZE));
        Assert.True(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Word_InOut");
        ushort[] usArr = InitArray<ushort>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Word_InOut(usArr, ARRAY_SIZE));
        Assert.True(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Long64_InOut");
        long[] lArr = InitArray<long>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Long64_InOut(lArr, ARRAY_SIZE));
        Assert.True(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_ULong64_InOut");
        ulong[] ulArr = InitArray<ulong>(ARRAY_SIZE);
        Assert.True(CStyle_Array_ULong64_InOut(ulArr, ARRAY_SIZE));
        Assert.True(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Double_InOut");
        double[] dArr = InitArray<double>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Double_InOut(dArr, ARRAY_SIZE));
        Assert.True(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Float_InOut");
        float[] fArr = InitArray<float>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Float_InOut(fArr, ARRAY_SIZE));
        Assert.True(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Byte_InOut");
        byte[] bArr = InitArray<byte>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Byte_InOut(bArr, ARRAY_SIZE));
        Assert.True(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Char_InOut");
        char[] cArr = InitArray<char>(ARRAY_SIZE);
        Assert.True(CStyle_Array_Char_InOut(cArr, ARRAY_SIZE));
        Assert.True(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_LPSTR_InOut");
        string[] strArr = InitArray<string>(ARRAY_SIZE);
        strArr[strArr.Length / 2] = null;
        Assert.True(CStyle_Array_LPSTR_InOut(strArr, ARRAY_SIZE));
        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.True(Equals<string>(strArr, expectedArr));

        Console.WriteLine("CStyle_Array_Struct_InOut");
        TestStruct[] tsArr = InitStructArray(ARRAY_SIZE);
        Assert.True(CStyle_Array_Struct_InOut(tsArr, ARRAY_SIZE));
        Assert.True(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Bool_InOut");
        bool[] boolArr = InitBoolArray(ARRAY_SIZE);
        Assert.True(CStyle_Array_Bool_InOut(boolArr, ARRAY_SIZE));
        Assert.True(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)));

        if (TestLibrary.PlatformDetection.IsBuiltInComEnabled)
        {
            Console.WriteLine("CStyle_Array_Object_InOut");
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            oArr[oArr.Length / 2] = null;
            Assert.True(CStyle_Array_Object_InOut(oArr, ARRAY_SIZE));

            object[] expectedOArr = GetExpectedOutArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            expectedOArr[expectedOArr.Length / 2 - 1] = null;
            Assert.True(Equals<object>(oArr, expectedOArr));
        }
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
                Console.WriteLine("Array marshaling error: Index: {0} :  Actual:{1}, Expected:{2},", i, arr1[i], arr2[i]);
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

    #region Marshal Out ByVal

    private static void TestMarshalOut_ByVal()
    {
        Console.WriteLine("By value marshaling CLR array as c-style-array with OutAttribute applied");

        Console.WriteLine("CStyle_Array_Int_Out");
        int[] iArr = new int[ARRAY_SIZE];
        Assert.True(CStyle_Array_Int_Out(iArr, ARRAY_SIZE));
        Assert.True(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Int_Out_Null");
        int[] iArrNull = null;
        Assert.True(CStyle_Array_Int_Out_Null(iArrNull));
        Assert.Null(iArrNull);

        Console.WriteLine("CStyle_Array_Int_Out_ZeroLength");
        int[] iArrLength0 = new int[0];
        Assert.True(CStyle_Array_Int_Out_ZeroLength(iArrLength0));
        Assert.Empty(iArrLength0);

        Console.WriteLine("CStyle_Array_Uint_Out");
        uint[] uiArr = new uint[ARRAY_SIZE];
        Assert.True(CStyle_Array_Uint_Out(uiArr, ARRAY_SIZE));
        Assert.True(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Short_Out");
        short[] sArr = new short[ARRAY_SIZE];
        Assert.True(CStyle_Array_Short_Out(sArr, ARRAY_SIZE));
        Assert.True(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Word_Out");
        ushort[] usArr = new ushort[ARRAY_SIZE];
        Assert.True(CStyle_Array_Word_Out(usArr, ARRAY_SIZE));
        Assert.True(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Long64_Out");
        long[] lArr = new long[ARRAY_SIZE];
        Assert.True(CStyle_Array_Long64_Out(lArr, ARRAY_SIZE));
        Assert.True(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_ULong64_Out");
        ulong[] ulArr = new ulong[ARRAY_SIZE];
        Assert.True(CStyle_Array_ULong64_Out(ulArr, ARRAY_SIZE));
        Assert.True(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Double_Out");
        double[] dArr = new double[ARRAY_SIZE];
        Assert.True(CStyle_Array_Double_Out(dArr, ARRAY_SIZE));
        Assert.True(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Float_Out");
        float[] fArr = new float[ARRAY_SIZE];
        Assert.True(CStyle_Array_Float_Out(fArr, ARRAY_SIZE));
        Assert.True(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Byte_Out");
        byte[] bArr = new byte[ARRAY_SIZE];
        Assert.True(CStyle_Array_Byte_Out(bArr, ARRAY_SIZE));
        Assert.True(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Char_Out");
        char[] cArr = new char[ARRAY_SIZE];
        Assert.True(CStyle_Array_Char_Out(cArr, ARRAY_SIZE));
        Assert.True(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_LPSTR_Out");
        string[] strArr = new string[ARRAY_SIZE];
        Assert.True(CStyle_Array_LPSTR_Out(strArr, ARRAY_SIZE));
        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.True(Equals<string>(strArr, expectedArr));
        Console.WriteLine("CStyle_Array_Struct_Out");
        TestStruct[] tsArr = new TestStruct[ARRAY_SIZE];
        Assert.True(CStyle_Array_Struct_Out(tsArr, ARRAY_SIZE));
        Assert.True(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)));

        Console.WriteLine("CStyle_Array_Bool_Out");
        bool[] boolArr = new bool[ARRAY_SIZE];
        Assert.True(CStyle_Array_Bool_Out(boolArr, ARRAY_SIZE));
        Assert.True(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)));

        if (TestLibrary.PlatformDetection.IsBuiltInComEnabled)
        {
            Console.WriteLine("CStyle_Array_Object_Out");
            object[] oArr = new object[ARRAY_SIZE];
            Assert.True(CStyle_Array_Object_Out(oArr, ARRAY_SIZE));

            object[] expectedOArr = GetExpectedOutArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            expectedOArr[expectedOArr.Length / 2 - 1] = null;
            Assert.True(Equals<object>(oArr, expectedOArr));
        }
    }

    #endregion

    private static void TestMultidimensional()
    {
        Console.WriteLine("================== [Get_Multidimensional_Array_Sum] ============");
        int[,] array = InitMultidimensionalBlittableArray(ROWS, COLUMNS);
        int sum = 0;
        foreach (int item in array)
        {
            sum += item;
        }

        Assert.Equal(sum, Get_Multidimensional_Array_Sum(array, ROWS, COLUMNS));
    }

    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/81674", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            TestMarshalByVal_NoAttributes();
            TestMarshalByVal_In();
            TestMarshalInOut_ByVal();
            TestMarshalOut_ByVal();
            TestMultidimensional();

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
