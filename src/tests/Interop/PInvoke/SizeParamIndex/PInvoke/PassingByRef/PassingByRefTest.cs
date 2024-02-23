// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SizeParamIndex.PInvoke;

/// <summary>
///  Pass LPArray Size by ref keyword using SizeParamIndex Attributes
/// </summary>

public class PassingByRefTest
{

    #region ByRef

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayByte_AsByRef_AsSizeParamIndex(
        ref byte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref byte[] arrByte);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArraySbyte_AsByRef_AsSizeParamIndex(
        ref sbyte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref sbyte[] arrSbyte);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayShort_AsByRef_AsSizeParamIndex(
        ref short arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref short[] arrShort);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayShortReturnNegative_AsByRef_AsSizeParamIndex(
        ref short arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref short[] arrShort);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayUshort_AsByRef_AsSizeParamIndex(
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ref ushort[] arrUshort, ref ushort arrSize);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayInt_AsByRef_AsSizeParamIndex(
        ref Int32 arrSize, Int32 unused, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref Int32[] arrInt32);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayUInt_AsByRef_AsSizeParamIndex(
         [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ref UInt32[] arrUInt32, UInt32 unused, ref UInt32 arrSize);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayLong_AsByRef_AsSizeParamIndex(
        ref long arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref long[] arrLong);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayUlong_AsByRef_AsSizeParamIndex(
        ref ulong arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ref ulong[] arrUlong);

    [DllImport("PInvokePassingByRefNative")]
    private static extern bool MarshalCStyleArrayString_AsByRef_AsSizeParamIndex(
        ref int arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, ArraySubType = UnmanagedType.BStr)] ref string[] arrStr,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0, ArraySubType = UnmanagedType.LPStr)] ref string[] arrStr2);

    #endregion

    static void SizeParamTypeIsByte()
    {
        string strDescription = "Scenario(byte==>uint8_t):Array_Size(M->N)=1,Array_Size(N->M)= byte.MinValue";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        byte byte_Array_Size = 1;
        byte[] arrByte = Helper.InitArray<byte>(byte_Array_Size);
        Assert.True(MarshalCStyleArrayByte_AsByRef_AsSizeParamIndex(ref byte_Array_Size, ref arrByte));

        //Construct Expected array
        int expected_ByteArray_Size = Byte.MinValue;
        byte[] expectedArrByte = Helper.GetExpChangeArray<byte>(expected_ByteArray_Size);
        Assert.True(Helper.EqualArray<byte>(arrByte, (int)byte_Array_Size, expectedArrByte, (int)expectedArrByte.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsSByte()
    {
        string strDescription = "Scenario(sbyte==>CHAR): Array_Size(M->N) = 10, Array_Size(N->M) = sbyte.Max";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        sbyte sbyte_Array_Size = (sbyte)10;
        sbyte[] arrSbyte = Helper.InitArray<sbyte>(sbyte_Array_Size);

        Assert.True(MarshalCStyleArraySbyte_AsByRef_AsSizeParamIndex(ref sbyte_Array_Size, ref arrSbyte));

        //Construct Expected
        sbyte[] expectedArrSbyte = Helper.GetExpChangeArray<sbyte>(sbyte.MaxValue);
        Assert.True(Helper.EqualArray<sbyte>(arrSbyte, (int)sbyte_Array_Size, expectedArrSbyte, (int)sbyte.MaxValue));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsShort1()
    {
        string strDescription = "Scenario(short==>int16_t)1: Array_Size(M->N) = -1, Array_Size(N->M) = 20";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        short short_Array_Size = (short)-1;
        short[] arrShort = Helper.InitArray<short>(10);
        int expected_ByteArraySize = 20;

        Assert.True(MarshalCStyleArrayShort_AsByRef_AsSizeParamIndex(ref short_Array_Size, ref arrShort));

        //Construct Expected
        short[] expectedArrShort = Helper.GetExpChangeArray<short>(expected_ByteArraySize);
        Assert.True(Helper.EqualArray<short>(arrShort, (int)short_Array_Size, expectedArrShort, expectedArrShort.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsShort2()
    {
        string strDescription = "Scenario(short==>int16_t)2: Array_Size(M->N) = 10, Array_Size(N->M) = -1";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        short short_Array_Size = (short)10;
        short[] arrShort = Helper.InitArray<short>(10);
        Assert.Throws<OverflowException>(() => MarshalCStyleArrayShortReturnNegative_AsByRef_AsSizeParamIndex(ref short_Array_Size, ref arrShort));
        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsUShort()
    {
        string strDescription = "Scenario(ushort==>uint16_t): Array_Size(M->N) = 0, Array_Size(N->M) = ushort.MaxValue";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        ushort ushort_Array_Size = 20;
        ushort[] arrUshort = Helper.InitArray<ushort>(ushort_Array_Size);

        int expected_UshortArraySize = ushort.MaxValue;
        Assert.True(MarshalCStyleArrayUshort_AsByRef_AsSizeParamIndex(ref arrUshort, ref ushort_Array_Size));

        //Construct Expected
        ushort[] expectedArrShort = Helper.GetExpChangeArray<ushort>(expected_UshortArraySize);
        Assert.True(Helper.EqualArray<ushort>(arrUshort, (int)ushort_Array_Size, expectedArrShort, expectedArrShort.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsInt32()
    {
        string strDescription = "Scenario(Int32==>int32_t):Array_Size(M->N)=10, Array_Size(N->M)=1";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        Int32 Int32_Array_Size = (Int32)10;
        Int32[] arrInt32 = Helper.InitArray<Int32>(Int32_Array_Size);

        Assert.True(MarshalCStyleArrayInt_AsByRef_AsSizeParamIndex(ref Int32_Array_Size, Int32.MaxValue, ref arrInt32));

        //Construct Expected
        int expected_UshortArraySize = 1;
        Int32[] expectedArrInt32 = Helper.GetExpChangeArray<Int32>(expected_UshortArraySize);
        Assert.True(Helper.EqualArray<Int32>(arrInt32, Int32_Array_Size, expectedArrInt32, expectedArrInt32.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsUInt32()
    {
        string strDescription = "Scenario(UInt32==>uint32_t):Array_Size(M->N)=1234,Array_Size(N->M)=4321";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        UInt32 UInt32_Array_Size = (UInt32)1234;
        UInt32[] arrUInt32 = Helper.InitArray<UInt32>((Int32)UInt32_Array_Size);
        Assert.True(MarshalCStyleArrayUInt_AsByRef_AsSizeParamIndex(ref arrUInt32, 1234, ref UInt32_Array_Size));

        //Construct Expected
        int expected_UInt32ArraySize = 4321;
        UInt32[] expectedArrUInt32 = Helper.GetExpChangeArray<UInt32>(expected_UInt32ArraySize);
        Assert.True(Helper.EqualArray<UInt32>(arrUInt32, (Int32)UInt32_Array_Size, expectedArrUInt32, (Int32)expectedArrUInt32.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsLong()
    {
        string strDescription = "Scenario(long==>int64_t):Array_Size(M->N)=10,Array_Size(N->M)=20";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        long long_Array_Size = (long)10;
        long[] arrLong = Helper.InitArray<long>((Int32)long_Array_Size);
        Assert.True(MarshalCStyleArrayLong_AsByRef_AsSizeParamIndex(ref long_Array_Size, ref arrLong));

        //Construct Expected Array
        int expected_LongArraySize = 20;
        long[] expectedArrLong = Helper.GetExpChangeArray<long>(expected_LongArraySize);
        Assert.True(Helper.EqualArray<long>(arrLong, (Int32)long_Array_Size, expectedArrLong, (Int32)expectedArrLong.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsULong()
    {
        string strDescription = "Scenario(ulong==>uint64_t):Array_Size(M->N)=0, Array_Size(N->M)=0";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        ulong ulong_Array_Size = (ulong)0;
        ulong[] arrUlong = Helper.InitArray<ulong>((Int32)ulong_Array_Size);

        Assert.True(MarshalCStyleArrayUlong_AsByRef_AsSizeParamIndex(ref ulong_Array_Size, ref arrUlong));

        //Construct Expected
        int expected_ULongArraySize = 0;
        ulong[] expectedArrUlong = Helper.GetExpChangeArray<ulong>(expected_ULongArraySize);
        Assert.True(Helper.EqualArray<ulong>(arrUlong, (Int32)ulong_Array_Size, expectedArrUlong, (Int32)expectedArrUlong.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsString()
    {
        string strDescription = "Scenario(String==>BSTR):Array_Size(M->N)= 20, Array_Size(N->M)=10";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        int array_Size = 20;
        String[] arrString = Helper.InitArray<String>(array_Size);
        String[] arrString2 = Helper.InitArray<String>(array_Size);

        Assert.True(MarshalCStyleArrayString_AsByRef_AsSizeParamIndex(ref array_Size, ref arrString, ref arrString2));

        //Construct Expected
        int expected_StringArraySize = 10;
        String[] expArrString = Helper.GetExpChangeArray<String>(expected_StringArraySize);
        Assert.True(Helper.EqualArray<String>(arrString, array_Size, expArrString, expArrString.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try{
            SizeParamTypeIsByte();
            SizeParamTypeIsSByte();
            SizeParamTypeIsShort1();
            SizeParamTypeIsShort2();
            SizeParamTypeIsUShort();
            SizeParamTypeIsInt32();
            SizeParamTypeIsUInt32();
            SizeParamTypeIsLong();
            SizeParamTypeIsULong();
            if (OperatingSystem.IsWindows())
            {
                SizeParamTypeIsString();
            }
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
