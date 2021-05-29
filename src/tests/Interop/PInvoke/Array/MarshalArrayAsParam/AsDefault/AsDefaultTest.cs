// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

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

        Assert.IsTrue(CStyle_Array_Int(InitArray<int>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Int");
        Assert.IsTrue(CStyle_Array_Uint(InitArray<uint>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Uint") ;
        Assert.IsTrue(CStyle_Array_Short(InitArray<short>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Short");
        Assert.IsTrue(CStyle_Array_Word(InitArray<ushort>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Word");
        Assert.IsTrue(CStyle_Array_Long64(InitArray<long>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Long64");
        Assert.IsTrue(CStyle_Array_ULong64(InitArray<ulong>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_ULong64");
        Assert.IsTrue(CStyle_Array_Double(InitArray<double>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Double");
        Assert.IsTrue(CStyle_Array_Float(InitArray<float>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Float");
        Assert.IsTrue(CStyle_Array_Byte(InitArray<byte>(ARRAY_SIZE), ARRAY_SIZE),"CStyle_Array_Byte");
        Assert.IsTrue(CStyle_Array_Char(InitArray<char>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Char");

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        strArr[strArr.Length / 2] = null;
        Assert.IsTrue(CStyle_Array_LPCSTR(strArr, ARRAY_SIZE), "CStyle_Array_LPCSTR");
        Assert.IsTrue(CStyle_Array_LPSTR(strArr, ARRAY_SIZE), "CStyle_Array_LPSTR");
        Assert.IsTrue(CStyle_Array_Struct(InitStructArray(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Struct");

        Assert.IsTrue(CStyle_Array_Bool(InitBoolArray(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Bool");

        if (OperatingSystem.IsWindows())
        {
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            oArr[oArr.Length / 2] = null;
            Assert.IsTrue(CStyle_Array_Object(oArr, ARRAY_SIZE), "CStyle_Array_Object"); 
        }
    }

    private static void TestMarshalByVal_In()
    {
        Console.WriteLine("ByVal marshaling  CLR array as c-style-array with InAttribute applied");

        Assert.IsTrue(CStyle_Array_Int_In(InitArray<int>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Int_In");
        Assert.IsTrue(CStyle_Array_Uint_In(InitArray<uint>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Uint_In");
        Assert.IsTrue(CStyle_Array_Short_In(InitArray<short>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Short_In");
        Assert.IsTrue(CStyle_Array_Word_In(InitArray<ushort>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Word_In");
        Assert.IsTrue(CStyle_Array_Long64_In(InitArray<long>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Long64_In");
        Assert.IsTrue(CStyle_Array_ULong64_In(InitArray<ulong>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_ULong64_In");
        Assert.IsTrue(CStyle_Array_Double_In(InitArray<double>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Double_In");
        Assert.IsTrue(CStyle_Array_Float_In(InitArray<float>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Float_In");
        Assert.IsTrue(CStyle_Array_Byte_In(InitArray<byte>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Byte_In");
        Assert.IsTrue(CStyle_Array_Char_In(InitArray<char>(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Char_In");

        string[] strArr = InitArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        strArr[strArr.Length / 2] = null;

        Assert.IsTrue(CStyle_Array_LPCSTR_In(strArr, ARRAY_SIZE), "CStyle_Array_LPCSTR_In");
        Assert.IsTrue(CStyle_Array_LPSTR_In(strArr, ARRAY_SIZE), "CStyle_Array_LPSTR_In");
        Assert.IsTrue(CStyle_Array_Struct_In(InitStructArray(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Struct_In");
        Assert.IsTrue(CStyle_Array_Bool_In(InitBoolArray(ARRAY_SIZE), ARRAY_SIZE), "CStyle_Array_Bool_In");
        if (OperatingSystem.IsWindows())
        {
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            oArr[oArr.Length / 2] = null;
            Assert.IsTrue(CStyle_Array_Object_In(oArr, ARRAY_SIZE), "CStyle_Array_Object_In"); 
        }
    }

    #endregion

    #region Marshal InOut ByVal

    private static void TestMarshalInOut_ByVal()
    {
        Console.WriteLine("By value marshaling CLR array as c-style-array with InAttribute and OutAttribute applied");
        Console.WriteLine("CStyle_Array_Int_InOut");
        int[] iArr = InitArray<int>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Int_InOut(iArr, ARRAY_SIZE), "CStyle_Array_Int_InOut");
        Assert.IsTrue(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)), "CStyle_Array_Int_InOut:Equals<int>");

        Console.WriteLine("CStyle_Array_Int_InOut_Null");
        int[] iArrNull = null;
        Assert.IsTrue(CStyle_Array_Int_InOut_Null(iArrNull), "CStyle_Array_Int_InOut_Null");
        Assert.IsNull(iArrNull, "CStyle_Array_Int_InOut_Null:Equals<null>");

        Console.WriteLine("CStyle_Array_Int_InOut_ZeroLength");
        int[] iArrLength0 = InitArray<int>(0);
        Assert.IsTrue(CStyle_Array_Int_InOut_ZeroLength(iArrLength0), "CStyle_Array_Int_InOut_ZeroLength");
        Assert.AreEqual(0, iArrLength0.Length, "CStyle_Array_Int_InOut_ZeroLength:Length<!0>");

        Console.WriteLine("CStyle_Array_Uint_InOut");
        uint[] uiArr = InitArray<uint>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Uint_InOut(uiArr, ARRAY_SIZE), "CStyle_Array_Uint_InOut");
        Assert.IsTrue(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)), "CStyle_Array_Uint_InOut:Equals<uint>");

        Console.WriteLine("CStyle_Array_Short_InOut");
        short[] sArr = InitArray<short>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Short_InOut(sArr, ARRAY_SIZE), "CStyle_Array_Short_InOut");
        Assert.IsTrue(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)), "CStyle_Array_Short_InOut:Equals<short>");

        Console.WriteLine("CStyle_Array_Word_InOut");
        ushort[] usArr = InitArray<ushort>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Word_InOut(usArr, ARRAY_SIZE), "CStyle_Array_Word_InOut");
        Assert.IsTrue(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)), "CStyle_Array_Word_InOut:Equals<ushort>");

        Console.WriteLine("CStyle_Array_Long64_InOut");
        long[] lArr = InitArray<long>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Long64_InOut(lArr, ARRAY_SIZE), "CStyle_Array_Long64_InOut");
        Assert.IsTrue(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)), "CStyle_Array_Long64_InOut:Equals<long>");

        Console.WriteLine("CStyle_Array_ULong64_InOut");
        ulong[] ulArr = InitArray<ulong>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_ULong64_InOut(ulArr, ARRAY_SIZE), "CStyle_Array_ULong64_InOut");
        Assert.IsTrue(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)), "CStyle_Array_ULong64_InOut:Equals<ulong>");

        Console.WriteLine("CStyle_Array_Double_InOut");
        double[] dArr = InitArray<double>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Double_InOut(dArr, ARRAY_SIZE), "CStyle_Array_Double_InOut");
        Assert.IsTrue(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)), "CStyle_Array_Double_InOut:Equals<double>");

        Console.WriteLine("CStyle_Array_Float_InOut");
        float[] fArr = InitArray<float>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Float_InOut(fArr, ARRAY_SIZE), "CStyle_Array_Float_InOut");
        Assert.IsTrue(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)), "CStyle_Array_Float_InOut:Equals<float>");

        Console.WriteLine("CStyle_Array_Byte_InOut");
        byte[] bArr = InitArray<byte>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Byte_InOut(bArr, ARRAY_SIZE), "CStyle_Array_Byte_InOut");
        Assert.IsTrue(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)), "CStyle_Array_Byte_InOut:Equals<byte>");

        Console.WriteLine("CStyle_Array_Char_InOut");
        char[] cArr = InitArray<char>(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Char_InOut(cArr, ARRAY_SIZE), "CStyle_Array_Char_InOut");
        Assert.IsTrue(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)), "CStyle_Array_Char_InOut:Equals<char>");

        Console.WriteLine("CStyle_Array_LPSTR_InOut");
        string[] strArr = InitArray<string>(ARRAY_SIZE);
        strArr[strArr.Length / 2] = null;
        Assert.IsTrue(CStyle_Array_LPSTR_InOut(strArr, ARRAY_SIZE), "CStyle_Array_LPSTR_InOut");
        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.IsTrue(Equals<string>(strArr, expectedArr), "CStyle_Array_LPSTR_InOut:Equals<string>");

        Console.WriteLine("CStyle_Array_Struct_InOut");
        TestStruct[] tsArr = InitStructArray(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Struct_InOut(tsArr, ARRAY_SIZE), "CStyle_Array_Struct_InOut");
        Assert.IsTrue(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)), "CStyle_Array_Struct_InOut:Equals<TestStruct>");

        Console.WriteLine("CStyle_Array_Bool_InOut");
        bool[] boolArr = InitBoolArray(ARRAY_SIZE);
        Assert.IsTrue(CStyle_Array_Bool_InOut(boolArr, ARRAY_SIZE), "CStyle_Array_Bool_InOut");
        Assert.IsTrue(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)), "CStyle_Array_Bool_InOut:Equals<bool>");

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("CStyle_Array_Object_InOut");
            object[] oArr = InitArray<object>(ARRAY_SIZE);
            oArr[oArr.Length / 2] = null;
            Assert.IsTrue(CStyle_Array_Object_InOut(oArr, ARRAY_SIZE), "CStyle_Array_Object_InOut");

            object[] expectedOArr = GetExpectedOutArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            expectedOArr[expectedOArr.Length / 2 - 1] = null;
            Assert.IsTrue(Equals<object>(oArr, expectedOArr), "CStyle_Array_Object_InOut:Equals<object>"); 
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
        Assert.IsTrue(CStyle_Array_Int_Out(iArr, ARRAY_SIZE), "CStyle_Array_Int_Out");
        Assert.IsTrue(Equals<int>(iArr, GetExpectedOutArray<int>(ARRAY_SIZE)), "CStyle_Array_Int_Out:Equals<int>");

        Console.WriteLine("CStyle_Array_Int_Out_Null");
        int[] iArrNull = null;
        Assert.IsTrue(CStyle_Array_Int_Out_Null(iArrNull), "CStyle_Array_Int_Out_Null");
        Assert.IsNull(iArrNull, "CStyle_Array_Int_Out_Null:Equals<null>");

        Console.WriteLine("CStyle_Array_Int_Out_ZeroLength");
        int[] iArrLength0 = new int[0];
        Assert.IsTrue(CStyle_Array_Int_Out_ZeroLength(iArrLength0), "CStyle_Array_Int_Out_ZeroLength");
        Assert.AreEqual(0, iArrLength0.Length, "CStyle_Array_Int_Out_ZeroLength:Length<!0>");

        Console.WriteLine("CStyle_Array_Uint_Out");
        uint[] uiArr = new uint[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Uint_Out(uiArr, ARRAY_SIZE), "CStyle_Array_Uint_Out");
        Assert.IsTrue(Equals<uint>(uiArr, GetExpectedOutArray<uint>(ARRAY_SIZE)), "CStyle_Array_Uint_Out:Equals<uint>");

        Console.WriteLine("CStyle_Array_Short_Out");
        short[] sArr = new short[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Short_Out(sArr, ARRAY_SIZE), "CStyle_Array_Short_Out");
        Assert.IsTrue(Equals<short>(sArr, GetExpectedOutArray<short>(ARRAY_SIZE)), "CStyle_Array_Short_Out:Equals<short>");

        Console.WriteLine("CStyle_Array_Word_Out");
        ushort[] usArr = new ushort[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Word_Out(usArr, ARRAY_SIZE), "CStyle_Array_Word_Out");
        Assert.IsTrue(Equals<ushort>(usArr, GetExpectedOutArray<ushort>(ARRAY_SIZE)), "CStyle_Array_Word_Out:Equals<ushort>");

        Console.WriteLine("CStyle_Array_Long64_Out");
        long[] lArr = new long[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Long64_Out(lArr, ARRAY_SIZE), "CStyle_Array_Long64_Out");
        Assert.IsTrue(Equals<long>(lArr, GetExpectedOutArray<long>(ARRAY_SIZE)), "CStyle_Array_Long64_Out:Equals<long>");

        Console.WriteLine("CStyle_Array_ULong64_Out");
        ulong[] ulArr = new ulong[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_ULong64_Out(ulArr, ARRAY_SIZE), "CStyle_Array_ULong64_Out");
        Assert.IsTrue(Equals<ulong>(ulArr, GetExpectedOutArray<ulong>(ARRAY_SIZE)), "CStyle_Array_ULong64_Out:Equals<ulong>");

        Console.WriteLine("CStyle_Array_Double_Out");
        double[] dArr = new double[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Double_Out(dArr, ARRAY_SIZE), "CStyle_Array_Double_Out");
        Assert.IsTrue(Equals<double>(dArr, GetExpectedOutArray<double>(ARRAY_SIZE)), "CStyle_Array_Double_Out:Equals<double>");

        Console.WriteLine("CStyle_Array_Float_Out");
        float[] fArr = new float[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Float_Out(fArr, ARRAY_SIZE), "CStyle_Array_Float_Out");
        Assert.IsTrue(Equals<float>(fArr, GetExpectedOutArray<float>(ARRAY_SIZE)), "CStyle_Array_Float_Out:Equals<float>");

        Console.WriteLine("CStyle_Array_Byte_Out");
        byte[] bArr = new byte[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Byte_Out(bArr, ARRAY_SIZE), "CStyle_Array_Byte_Out");
        Assert.IsTrue(Equals<byte>(bArr, GetExpectedOutArray<byte>(ARRAY_SIZE)), "CStyle_Array_Byte_Out:Equals<byte>");

        Console.WriteLine("CStyle_Array_Char_Out");
        char[] cArr = new char[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Char_Out(cArr, ARRAY_SIZE), "CStyle_Array_Char_Out");
        Assert.IsTrue(Equals<char>(cArr, GetExpectedOutArray<char>(ARRAY_SIZE)), "CStyle_Array_Char_Out:Equals<char>");

        Console.WriteLine("CStyle_Array_LPSTR_Out");
        string[] strArr = new string[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_LPSTR_Out(strArr, ARRAY_SIZE), "CStyle_Array_LPSTR_Out");
        string[] expectedArr = GetExpectedOutArray<string>(ARRAY_SIZE);
        // Test nesting null value scenario
        expectedArr[expectedArr.Length / 2 - 1] = null;
        Assert.IsTrue(Equals<string>(strArr, expectedArr), "CStyle_Array_LPSTR_Out:Equals<string>");
        Console.WriteLine("CStyle_Array_Struct_Out");
        TestStruct[] tsArr = new TestStruct[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Struct_Out(tsArr, ARRAY_SIZE), "CStyle_Array_Struct_Out");
        Assert.IsTrue(Equals<TestStruct>(tsArr, GetExpectedOutStructArray(ARRAY_SIZE)), "Equals<TestStruct>");

        Console.WriteLine("CStyle_Array_Bool_Out");
        bool[] boolArr = new bool[ARRAY_SIZE];
        Assert.IsTrue(CStyle_Array_Bool_Out(boolArr, ARRAY_SIZE), "CStyle_Array_Bool_Out");
        Assert.IsTrue(Equals<bool>(boolArr, GetExpectedOutBoolArray(ARRAY_SIZE)), "CStyle_Array_Bool_Out:Equals<bool>");

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("CStyle_Array_Object_Out");
            object[] oArr = new object[ARRAY_SIZE];
            Assert.IsTrue(CStyle_Array_Object_Out(oArr, ARRAY_SIZE), "CStyle_Array_Object_Out");

            object[] expectedOArr = GetExpectedOutArray<object>(ARRAY_SIZE);
            // Test nesting null value scenario
            expectedOArr[expectedOArr.Length / 2 - 1] = null;
            Assert.IsTrue(Equals<object>(oArr, expectedOArr), "CStyle_Array_Object_Out:Equals<object>"); 
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

        Assert.AreEqual(sum, Get_Multidimensional_Array_Sum(array, ROWS, COLUMNS));
    }

    public static int Main()
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
