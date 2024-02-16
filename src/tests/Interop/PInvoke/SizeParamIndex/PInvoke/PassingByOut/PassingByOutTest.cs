// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SizeParamIndex.PInvoke;
/// <summary>
///  Pass Array Size by out keyword using SizeParamIndex Attributes
/// </summary>
public class PassingByOutTest
{

    #region ByOut

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayByte_AsByOut_AsSizeParamIndex(
        out byte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out byte[] arrByte);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArraySbyte_AsByOut_AsSizeParamIndex(
        out sbyte arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out sbyte[] arrSbyte);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayShort_AsByOut_AsSizeParamIndex(
        out short arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out short[] arrShort);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayShortReturnNegative_AsByOut_AsSizeParamIndex(
        out short arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out short[] arrShort);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayUshort_AsByOut_AsSizeParamIndex(
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ushort[] arrUshort, out ushort arrSize);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayInt_AsByOut_AsSizeParamIndex(
        out Int32 arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out Int32[] arrInt32);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayUInt_AsByOut_AsSizeParamIndex(
        out UInt32 arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out UInt32[] arrUInt32);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayLong_AsByOut_AsSizeParamIndex(
        out long arrSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] out long[] arrLong);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayUlong_AsByOut_AsSizeParamIndex(
         [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out ulong[] arrUlong, out ulong arrSize, ulong unused);

    [DllImport("PInvokePassingByOutNative")]
    private static extern bool MarshalCStyleArrayString_AsByOut_AsSizeParamIndex(
    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.BStr)] out string[] arrInt32, out int arrSize);

    #endregion

    static void SizeParamTypeIsByte()
    {
        string strDescription = "Scenario(byte ==> uint8_t): Array_Size(N->M) = 1";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        byte byte_Array_Size;
        byte[] arrByte;
        Assert.True(MarshalCStyleArrayByte_AsByOut_AsSizeParamIndex(out byte_Array_Size, out arrByte));

        //Construct Expected array
        int expected_ByteArray_Size = 1;
        byte[] expectedArrByte = Helper.GetExpChangeArray<byte>(expected_ByteArray_Size);
        Assert.True(Helper.EqualArray<byte>(arrByte, (int)byte_Array_Size, expectedArrByte, (int)expectedArrByte.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsSByte()
    {
        string strDescription = "Scenario(sbyte ==> CHAR):Array_Size(N->M) = sbyte.Max";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        sbyte sbyte_Array_Size;
        sbyte[] arrSbyte;
        Assert.True(MarshalCStyleArraySbyte_AsByOut_AsSizeParamIndex(out sbyte_Array_Size, out arrSbyte));

        sbyte[] expectedArrSbyte = Helper.GetExpChangeArray<sbyte>(sbyte.MaxValue);
        Assert.True(Helper.EqualArray<sbyte>(arrSbyte, (int)sbyte_Array_Size, expectedArrSbyte, (int)expectedArrSbyte.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsShort1()
    {
        string strDescription = "Scenario(short ==> int16_t)1,Array_Size(M->N) = -1, Array_Size(N->M)=(ShortMax+1)/2";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        short shortArray_Size = (short)-1;
        short[] arrShort = Helper.InitArray<short>(10);
        Assert.True(MarshalCStyleArrayShort_AsByOut_AsSizeParamIndex(out shortArray_Size, out arrShort));

        //Construct Expected Array
        int expected_ShortArray_Size = 16384;//(SHRT_MAX+1)/2
        short[] expectedArrShort = Helper.GetExpChangeArray<short>(expected_ShortArray_Size);
        Assert.True(Helper.EqualArray<short>(arrShort, (int)shortArray_Size, expectedArrShort, (int)expectedArrShort.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsShort2()
    {
        string strDescription = "Scenario(short ==> int16_t)2, Array_Size = 10, Array_Size(N->M) = -1";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        short short_Array_Size = (short)10;
        short[] arrShort = Helper.InitArray<short>(short_Array_Size);
        Assert.Throws<OverflowException>(() => MarshalCStyleArrayShortReturnNegative_AsByOut_AsSizeParamIndex(out short_Array_Size, out arrShort));
        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsUShort()
    {
        string strDescription = "Scenario(ushort==>uint16_t): Array_Size(N->M) = ushort.MaxValue";
        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        ushort ushort_Array_Size;
        ushort[] arrUshort;
        Assert.True(MarshalCStyleArrayUshort_AsByOut_AsSizeParamIndex(out arrUshort, out ushort_Array_Size));

        //Expected Array
        ushort[] expectedArrUshort = Helper.GetExpChangeArray<ushort>(ushort.MaxValue);
        Assert.True(Helper.EqualArray<ushort>(arrUshort, (int)ushort_Array_Size, expectedArrUshort, (ushort)expectedArrUshort.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsInt32()
    {
        string strDescription = "Scenario(Int32 ==> int32_t): Array_Size(N->M) = 0 ";

        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        Int32 Int32_Array_Size;
        Int32[] arrInt32;
        Assert.True(MarshalCStyleArrayInt_AsByOut_AsSizeParamIndex(out Int32_Array_Size, out arrInt32));

        //Expected Array
        Int32[] expectedArrInt32 = Helper.GetExpChangeArray<Int32>(0);
        Assert.True(Helper.EqualArray<Int32>(arrInt32, Int32_Array_Size, expectedArrInt32, expectedArrInt32.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsUInt32()
    {
        string strDescription = "Scenario(UInt32 ==> uint32_t): Array_Size(N->M) = 20";

        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        int expected_UInt32ArraySize = 20;

        UInt32 UInt32_Array_Size = (UInt32)10;
        UInt32[] arrUInt32 = Helper.InitArray<UInt32>((Int32)UInt32_Array_Size);
        Assert.True(MarshalCStyleArrayUInt_AsByOut_AsSizeParamIndex(out UInt32_Array_Size, out arrUInt32));

        //Construct expected
        UInt32[] expectedArrUInt32 = Helper.GetExpChangeArray<UInt32>(expected_UInt32ArraySize);
        Assert.True(Helper.EqualArray<UInt32>(arrUInt32, (Int32)UInt32_Array_Size, expectedArrUInt32, (Int32)expectedArrUInt32.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsLong()
    {
        string strDescription = "Scenario(long ==> int64_t): Array_Size(N->M) = 20";

        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        int expected_LongArraySize = 20;

        long long_Array_Size = (long)10;
        long[] arrLong = Helper.InitArray<long>((Int32)long_Array_Size);
        Assert.True(MarshalCStyleArrayLong_AsByOut_AsSizeParamIndex(out long_Array_Size, out arrLong));

        long[] expectedArrLong = Helper.GetExpChangeArray<long>(expected_LongArraySize);
        Assert.True(Helper.EqualArray<long>(arrLong, (Int32)long_Array_Size, expectedArrLong, (Int32)expectedArrLong.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsULong()
    {
        string strDescription = "Scenario(ulong ==> uint64_t): Array_Size(N->M) = 1000";

        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        int expected_ULongArraySize = 1000;

        ulong ulong_Array_Size = (ulong)10;
        ulong[] arrUlong = Helper.InitArray<ulong>((Int32)ulong_Array_Size);
        Assert.True(MarshalCStyleArrayUlong_AsByOut_AsSizeParamIndex(out arrUlong, out ulong_Array_Size, ulong_Array_Size));

        ulong[] expectedArrUlong = Helper.GetExpChangeArray<ulong>(expected_ULongArraySize);
        Assert.True(Helper.EqualArray<ulong>(arrUlong, (Int32)ulong_Array_Size, expectedArrUlong, (Int32)expectedArrUlong.Length));

        Console.WriteLine(strDescription + " Ends!");
    }

    static void SizeParamTypeIsString()
    {
        string strDescription = "Scenario(String ==> BSTR): Array_Size(N->M) = 20";

        Console.WriteLine();
        Console.WriteLine(strDescription + " Starts!");

        int expected_StringArraySize = 20;
        int string_Array_Size = 10;
        String[] arrString = Helper.InitArray<String>(string_Array_Size);
        Assert.True(MarshalCStyleArrayString_AsByOut_AsSizeParamIndex(out arrString, out string_Array_Size));

        String[] expArrString = Helper.GetExpChangeArray<String>(expected_StringArraySize);
        Assert.True(Helper.EqualArray<String>(arrString, string_Array_Size, expArrString, expArrString.Length));
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
