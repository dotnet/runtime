// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SizeParamIndex.ReversePInvoke;
public class PassingByOutTest
{
    public static int arrSize = 10;

    public static int failures = 0;

    #region Func Sig

    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalByteArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelByteArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalSbyteArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelSbyteArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalShortArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelShortArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelShortArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUshortArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelUshortArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalInt32Array_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelInt32ArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUint32Array_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelUint32ArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalLongArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelLongArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalUlongArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelUlongArrByOutAsCdeclCaller caller);
    [DllImport("ReversePInvokePassingByOutNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool DoCallBack_MarshalStringArray_AsParam_AsByOut([MarshalAs(UnmanagedType.FunctionPtr)]DelStringArrByOutAsCdeclCaller caller);

    #endregion

    #region Delegate Method

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelByteArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte[] arrArg, out byte arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelSbyteArrByOutAsCdeclCaller(out sbyte arraySize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out sbyte[] arrArg);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelShortArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out short[] arrArg, out short arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUshortArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ushort[] arrArg, out ushort arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelInt32ArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Int32[] arrArg, out Int32 arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUint32ArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out UInt32[] arrArg, out UInt32 arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelLongArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out long[] arrArg, out long arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelUlongArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ulong[] arrArg, out ulong arraySize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelStringArrByOutAsCdeclCaller([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.BStr)] out string[] arrArg, out Int32 arraySize);

    #endregion

    #region Test Method

    //Type: byte ==> uint8_t    Array Size: byte.MinValue ==> 20
    public static bool TestMethodForByteArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte[] arrArg, out byte arraySize)
    {
        arrArg = Helper.GetExpChangeArray<byte>(20);
        arraySize = 20;
        return true;
    }

    //Type: sbyte ==> CHAR  Array Size: 1 ==> sbyte.MaxValue
    public static bool TestMethodForSbyteArray_AsReversePInvokeByOut_AsCdecl(out sbyte arraySize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out sbyte[] arrArg)
    {
        arrArg = Helper.GetExpChangeArray<sbyte>(sbyte.MaxValue);
        arraySize = sbyte.MaxValue;
        return true;
    }

    //Type: short ==> int16_t  Array Size: -1 ==> 20(Actual 10 ==> 20)
    public static bool TestMethodForShortArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out short[] arrArg, out short arraySize)
    {
        arrArg = Helper.GetExpChangeArray<short>(20);
        arraySize = 20;
        return true;
    }

    //Type: short ==> int16_t  Array Size: 10 ==> -1(Actual 10 ==> 20)
    public static bool TestMethodForShortArrayReturnNegativeSize_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out short[] arrArg, out short arraySize)
    {
        arrArg = Helper.GetExpChangeArray<short>(20);
        arraySize = -1;
        return true;
    }

    //Type: ushort ==> uint16_t  Array Size: ushort.MaxValue ==> 20
    public static bool TestMethodForUshortArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ushort[] arrArg, out ushort arraySize)
    {
        arrArg = Helper.GetExpChangeArray<ushort>(20);
        arraySize = 20;
        return true;
    }

    //Type: Int32 ==> int32_t    Array Size: 10 ==> 20
    public static bool TestMethodForInt32Array_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out Int32[] arrArg, out Int32 arraySize)
    {
        arrArg = Helper.GetExpChangeArray<Int32>(20);
        arraySize = 20;
        return true;
    }

    //Type: UInt32 ==> uint32_t    Array Size: 10 ==> 20
    public static bool TestMethodForUint32Array_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out UInt32[] arrArg, out UInt32 arraySize)
    {
        arrArg = Helper.GetExpChangeArray<UInt32>(20);
        arraySize = 20;
        return true;
    }

    //Type: long ==> int64_t    Array Size: 10 ==> 20
    public static bool TestMethodForLongArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out long[] arrArg, out long arraySize)
    {
        arrArg = Helper.GetExpChangeArray<long>(20);
        arraySize = 20;
        return true;
    }

    //Type: ulong ==> uint64_t    Array Size: 10 ==> 20
    public static bool TestMethodForUlongArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ulong[] arrArg, out ulong arraySize)
    {
        arrArg = Helper.GetExpChangeArray<ulong>(20);
        arraySize = 20;
        return true;
    }

    //Type: string ==> BSTR    Array Size: 10 ==> 20
    public static bool TestMethodForStringArray_AsReversePInvokeByOut_AsCdecl([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.BStr)] out string[] arrArg, out Int32 arraySize)
    {
        arrArg = Helper.GetExpChangeArray<string>(20);
        arraySize = 20;
        return true;
    }

    #endregion

    public static void RunTestByOut()
    {
        Console.WriteLine("ReversePInvoke C-Style Array marshaled by out with SizeParamIndex attribute(by out Array size).");

        //Common value type
        Console.WriteLine("\tScenario 1 : byte ==> uint8_t, Array_Size = byte.MinValue, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalByteArray_AsParam_AsByOut(new DelByteArrByOutAsCdeclCaller(TestMethodForByteArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalByteArray_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 2 : sbyte ==> CHAR, Array_Size = 1, Return_Array_Size = sbyte.Max");
        Assert.True(DoCallBack_MarshalSbyteArray_AsParam_AsByOut(new DelSbyteArrByOutAsCdeclCaller(TestMethodForSbyteArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalSbyteArray_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 3 : short ==> int16_t, Array_Size = -1, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalShortArray_AsParam_AsByOut(new DelShortArrByOutAsCdeclCaller(TestMethodForShortArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalShortArray_AsReversePInvokeByOut_AsCdecl Failed!");

        Console.WriteLine("\tScenario 4 : short ==> int16_t, Array_Size = 10, Return_Array_Size = -1");
        Assert.True(DoCallBack_MarshalShortArrayReturnNegativeSize_AsParam_AsByOut(new DelShortArrByOutAsCdeclCaller(TestMethodForShortArrayReturnNegativeSize_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalShortArray_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 5 : ushort ==> uint16_t, Array_Size = ushort.MaxValue, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalUshortArray_AsParam_AsByOut(new DelUshortArrByOutAsCdeclCaller(TestMethodForUshortArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalUshortArray_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 6 : Int32 ==> int32_t, Array_Size = 10, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalInt32Array_AsParam_AsByOut(new DelInt32ArrByOutAsCdeclCaller(TestMethodForInt32Array_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalInt32Array_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 7 : UInt32 ==> uint32_t, Array_Size = 10, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalUint32Array_AsParam_AsByOut(new DelUint32ArrByOutAsCdeclCaller(TestMethodForUint32Array_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalUint32Array_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 8 : long ==> int64_t, Array_Size = 10, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalLongArray_AsParam_AsByOut(new DelLongArrByOutAsCdeclCaller(TestMethodForLongArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalLongArray_AsReversePInvokeByOut_AsCdecl Passed!");

        Console.WriteLine("\tScenario 9 : ulong ==> uint64_t, Array_Size = 10, Return_Array_Size = 20");
        Assert.True(DoCallBack_MarshalUlongArray_AsParam_AsByOut(new DelUlongArrByOutAsCdeclCaller(TestMethodForUlongArray_AsReversePInvokeByOut_AsCdecl)));
        Console.WriteLine("\t\tMarshalUlongArray_AsReversePInvokeByOut_AsCdecl Passed!");

        if (OperatingSystem.IsWindows())
        {
            Console.WriteLine("\tScenario 10 : string ==> BSTR, Array_Size = 10, Return_Array_Size = 20");
            Assert.True(DoCallBack_MarshalStringArray_AsParam_AsByOut(new DelStringArrByOutAsCdeclCaller(TestMethodForStringArray_AsReversePInvokeByOut_AsCdecl)));
            Console.WriteLine("\t\tMarshalStringArray_AsReversePInvokeByOut_AsCdecl Passed!");
        }
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34196", TestRuntimes.Mono)]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/167", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try{
            RunTestByOut();
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
