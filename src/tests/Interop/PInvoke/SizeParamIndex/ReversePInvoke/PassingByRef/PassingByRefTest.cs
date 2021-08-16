// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

public class ReversePInvoke_MashalArrayByRef_AsManagedTest
{
    public static int arrSize = 10;

    public static int failures = 0;

    #region Func Sig

    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalByteArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelByteArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalSbyteArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelSbyteArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalShortArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelShortArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelShortArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUshortArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelUshortArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalInt32Array_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelInt32ArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUint32Array_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelUint32ArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalLongArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelLongArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUlongArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelUlongArrByRefAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByRefNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalStringArray_AsParam_AsByRef([MarshalAs(UnmanagedType.FunctionPtr)]DelStringArrByRefAsCdeclCaller caller);

    #endregion

    #region Delegate Method

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelByteArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref byte[] arrArg, ref byte arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelSbyteArrByRefAsCdeclCaller(ref sbyte arraySize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref sbyte[] arrArg);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelShortArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref short[] arrArg, ref short arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUshortArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref ushort[] arrArg, ref ushort arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelInt32ArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref Int32[] arrArg, ref Int32 arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUint32ArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref UInt32[] arrArg, ref UInt32 arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelLongArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref long[] arrArg, ref long arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUlongArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref ulong[] arrArg, ref ulong arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelStringArrByRefAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.BStr)] ref string[] arrArg, ref Int32 arraySize);
    
    #endregion

    #region Test Method

    //Type: byte ==> BYTE    Array Size: byte.MinValue ==> 20
    public static bool TestMethodForByteArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref byte[] arrArg, ref byte arraySize)
    {
        if (arraySize == byte.MinValue)
            return Helper.CheckAndChangeArray<byte>(ref arrArg, ref arraySize, (Int32)byte.MinValue, 20);
        return false;
    }

    //Type: sbyte ==> CHAR  Array Size: 1 ==> sbyte.MaxValue
    public static bool TestMethodForSbyteArray_AsReversePInvokeByRef_AsCdecl(ref sbyte arraySize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref sbyte[] arrArg)
    {
        if (arraySize == 1)
            return Helper.CheckAndChangeArray<sbyte>(ref arrArg, ref arraySize, 1, (Int32)sbyte.MaxValue);
        return false;
    }

    //Type: short ==> SHORT  Array Size: -1 ==> 20(Actual 10 ==> 20)
    public static bool TestMethodForShortArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref short[] arrArg, ref short arraySize)
    {
        if (arraySize == -1)
            return Helper.CheckAndChangeArray<short>(ref arrArg, ref arraySize, 10, 20);
        return false;
    }

    //Type: short ==> SHORT  Array Size: 10 ==> -1(Actual 10 ==> 20)
    public static bool TestMethodForShortArrayReturnNegativeSize_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref short[] arrArg, ref short arraySize)
    {
        if (arraySize == 10)
        {
            Helper.CheckAndChangeArray<short>(ref arrArg, ref arraySize, 10, 20);
            arraySize = -1;
            return true;
        }
        return false;
    }

    //Type: ushort ==> USHORT  Array Size: ushort.MaxValue ==> 20
    public static bool TestMethodForUshortArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref ushort[] arrArg, ref ushort arraySize)
    {
        if (arraySize == ushort.MaxValue)
            return Helper.CheckAndChangeArray<ushort>(ref arrArg, ref arraySize, (Int32)ushort.MaxValue, 20);
        return false;
    }

    //Type: Int32 ==> LONG    Array Size: 10 ==> 20
    public static bool TestMethodForInt32Array_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref Int32[] arrArg, ref Int32 arraySize)
    {
        if (arraySize == 10)
            return Helper.CheckAndChangeArray<Int32>(ref arrArg, ref arraySize, 10, 20);
        return false;
    }

    //Type: UInt32 ==> ULONG    Array Size: 10 ==> 20
    public static bool TestMethodForUint32Array_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref UInt32[] arrArg, ref UInt32 arraySize)
    {
        if (arraySize == 10)
            return Helper.CheckAndChangeArray<UInt32>(ref arrArg, ref arraySize, 10, 20);
        return false;
    }

    //Type: long ==> LONGLONG    Array Size: 10 ==> 20
    public static bool TestMethodForLongArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref long[] arrArg, ref long arraySize)
    {
        if (arraySize == 10)
            return Helper.CheckAndChangeArray<long>(ref arrArg, ref arraySize, 10, 20);
        return false;
    }

    //Type: ulong ==> ULONGLONG    Array Size: 10 ==> 20
    public static bool TestMethodForUlongArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref ulong[] arrArg, ref ulong arraySize)
    {
        if (arraySize == 10)
            return Helper.CheckAndChangeArray<ulong>(ref arrArg, ref arraySize, 10, 20);
        return false;
    }

    //Type: string ==> BSTR    Array Size: 10 ==> 20
    public static bool TestMethodForStringArray_AsReversePInvokeByRef_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.BStr)] ref string[] arrArg, ref Int32 arraySize)
    {
        string[] actualArr = Helper.InitArray<string>(10);
        if (!Helper.EqualArray<string>(arrArg, arraySize, actualArr, 10))
        {
            return false;
        }

        arraySize = 20;
        arrArg = Helper.GetExpChangeArray<string>(20);
        return true;
    }

    #endregion

    public static void RunTestByRef()
    {
        Console.WriteLine("ReversePInvoke C-Style Array marshaled by ref with SizeParamIndex attribute(by ref Array size).");

        //Common value type
        Console.WriteLine("\tScenario 1 : byte ==> BYTE, Array_Size = byte.MinValue, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalByteArray_AsParam_AsByRef(new DelByteArrByRefAsCdeclCaller(TestMethodForByteArray_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalByteArray_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 2 : sbyte ==> CHAR, Array_Size = 1, Return_Array_Size = sbyte.Max");
        Assert.IsTrue(DoCallBack_MarshalSbyteArray_AsParam_AsByRef(new DelSbyteArrByRefAsCdeclCaller(TestMethodForSbyteArray_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalSbyteArray_AsReversePInvokeByRef_AsCdecl Passed!");

        // We don't support exception interop in .NET off-Windows.
        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("\tScenario 3 : short ==> SHORT, Array_Size = -1, Return_Array_Size = 20");
            Assert.Throws<OverflowException>(() => DoCallBack_MarshalShortArray_AsParam_AsByRef(new DelShortArrByRefAsCdeclCaller(TestMethodForShortArray_AsReversePInvokeByRef_AsCdecl)));
            Console.WriteLine("\t\tMarshalShortArray_AsReversePInvokeByRef_AsCdecl Passed!");
        }


        Console.WriteLine("\tScenario 4 : short ==> SHORT, Array_Size = 10, Return_Array_Size = -1");
        Assert.IsTrue(DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByRef(new DelShortArrByRefAsCdeclCaller(TestMethodForShortArrayReturnNegativeSize_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalShortArray_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 5 : ushort ==> USHORT, Array_Size = ushort.MaxValue, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalUshortArray_AsParam_AsByRef(new DelUshortArrByRefAsCdeclCaller(TestMethodForUshortArray_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalUshortArray_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 6 : Int32 ==> LONG, Array_Size = 10, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalInt32Array_AsParam_AsByRef(new DelInt32ArrByRefAsCdeclCaller(TestMethodForInt32Array_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalInt32Array_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 7 : UInt32 ==> ULONG, Array_Size = 10, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalUint32Array_AsParam_AsByRef(new DelUint32ArrByRefAsCdeclCaller(TestMethodForUint32Array_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalUint32Array_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 8 : long ==> LONGLONG, Array_Size = 10, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalLongArray_AsParam_AsByRef(new DelLongArrByRefAsCdeclCaller(TestMethodForLongArray_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalLongArray_AsReversePInvokeByRef_AsCdecl Passed!");

        Console.WriteLine("\tScenario 9 : ulong ==> ULONGLONG, Array_Size = 10, Return_Array_Size = 20");
        Assert.IsTrue(DoCallBack_MarshalUlongArray_AsParam_AsByRef(new DelUlongArrByRefAsCdeclCaller(TestMethodForUlongArray_AsReversePInvokeByRef_AsCdecl)));
        Console.WriteLine("\t\tMarshalUlongArray_AsReversePInvokeByRef_AsCdecl Passed!");

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("\tScenario 10 : string ==> BSTR, Array_Size = 10, Return_Array_Size = 20");
            Assert.IsTrue(DoCallBack_MarshalStringArray_AsParam_AsByRef(new DelStringArrByRefAsCdeclCaller(TestMethodForStringArray_AsReversePInvokeByRef_AsCdecl)));
            Console.WriteLine("\t\tMarshalStringArray_AsReversePInvokeByRef_AsCdecl Passed!");
        }
    }

    public static int Main()
    {
        try{
            RunTestByRef();
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
