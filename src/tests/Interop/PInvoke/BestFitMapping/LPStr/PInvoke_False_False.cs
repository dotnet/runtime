// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public class PInvoke_False_False
{
    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(false, ThrowOnUnmappableChar = false)]
    public struct LPStrTestStruct
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(false, ThrowOnUnmappableChar = false)]
    public class LPStrTestClass
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

#pragma warning disable 618
    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_String([In][MarshalAs(UnmanagedType.LPStr)]String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_String([In][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_String([In, Out][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_StringBuilder([In, Out][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Struct_String([In][MarshalAs(UnmanagedType.Struct)]LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Struct_String([In][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Struct_String([In, Out][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Array_String([In][MarshalAs(UnmanagedType.LPArray)]String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Array_String([In][MarshalAs(UnmanagedType.LPArray)]ref String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Array_String([In, Out][MarshalAs(UnmanagedType.LPArray)]ref String[] Array);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Class_String([In, Out][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_Array_Struct([In, Out][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);
#pragma warning restore 618

    static void testLPStrBufferString()
    {
        Assert.True(LPStrBuffer_In_String(GetInvalidString()));

        Assert.True(LPStrBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        Assert.True(LPStrBuffer_InByRef_String(ref cTemp));

        cTemp = GetValidString();
        Assert.True(LPStrBuffer_InByRef_String(ref cTemp));

        cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_String(ref cTemp));
        Assert.NotEqual(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testLPStrBufferStringBuilder()
    {
        StringBuilder sb = GetInvalidStringBuilder();
        Assert.True(LPStrBuffer_In_StringBuilder(sb));

        Assert.True(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        Assert.True(LPStrBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        Assert.True(LPStrBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.NotEqual(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());
    }

    static LPStrTestStruct GetInvalidStruct()
    {
        LPStrTestStruct inValidStruct = new LPStrTestStruct();
        inValidStruct.str = GetInvalidString();

        return inValidStruct;
    }


    static LPStrTestStruct GetValidStruct()
    {
        LPStrTestStruct validStruct = new LPStrTestStruct();
        validStruct.str = GetValidString();

        return validStruct;
    }

    static void testLPStrBufferStruct()
    {
        LPStrTestStruct lpss = GetInvalidStruct();
        Assert.True(LPStrBuffer_In_Struct_String(lpss));

        Assert.True(LPStrBuffer_In_Struct_String(GetValidStruct()));

        LPStrTestStruct cTemp = GetInvalidStruct();
        Assert.True(LPStrBuffer_InByRef_Struct_String(ref cTemp));

        cTemp = GetValidStruct();
        Assert.True(LPStrBuffer_InByRef_Struct_String(ref cTemp));

        cTemp = GetInvalidStruct();
        LPStrTestStruct cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_Struct_String(ref cTemp));
        Assert.NotEqual(cTempClone.str, cTemp.str);

        cTemp = GetValidStruct();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_Struct_String(ref cTemp));
        Assert.Equal(cTempClone.str, cTemp.str);
    }

    static void testLPStrBufferClass()
    {
        LPStrTestClass lpss = new LPStrTestClass();
        lpss.str = GetInvalidString();
        Assert.True(LPStrBuffer_In_Class_String(lpss));

        lpss.str = GetValidString();
        Assert.True(LPStrBuffer_In_Class_String(lpss));

        LPStrTestClass cTemp = new LPStrTestClass();
        cTemp.str = GetInvalidString();
        Assert.True(LPStrBuffer_InByRef_Class_String(ref cTemp));

        cTemp.str = GetValidString();
        Assert.True(LPStrBuffer_InByRef_Class_String(ref cTemp));

        cTemp.str = GetInvalidString();
        LPStrTestClass cTempClone = new LPStrTestClass();
        cTempClone.str = cTemp.str;
        Assert.True(LPStrBuffer_InOutByRef_Class_String(ref cTemp));
        Assert.NotEqual(cTempClone.str, cTemp.str);

        cTemp.str = GetValidString();
        cTempClone.str = cTemp.str;
        Assert.True(LPStrBuffer_InOutByRef_Class_String(ref cTemp));
        Assert.Equal(cTempClone.str, cTemp.str);
    }

    static void testLPStrBufferArray()
    {
        String[] lpss = GetInvalidStringArray();
        Assert.True(LPStrBuffer_In_Array_String(lpss));

        Assert.True(LPStrBuffer_In_Array_String(GetValidStringArray()));

        String[] cTemp = GetInvalidStringArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref cTemp));

        cTemp = GetValidStringArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref cTemp));

        cTemp = GetInvalidStringArray();
        String[] cTempClone = new String[3];
        cTempClone[0] = cTemp[0];
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref cTemp));
        Assert.NotEqual(cTempClone[0], cTemp[0]);

        cTemp = GetValidStringArray();
        cTempClone[0] = cTemp[0];
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref cTemp));
        Assert.Equal(cTempClone[0], cTemp[0]);
    }

    static void testLPStrBufferArrayOfStructs()
    {
        LPStrTestStruct[] lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.True(LPStrBuffer_In_Array_Struct(lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.True(LPStrBuffer_In_Array_Struct(lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.True(LPStrBuffer_InByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.True(LPStrBuffer_InByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        LPStrTestStruct[] lpssClone = new LPStrTestStruct[2];
        lpssClone[0].str = lpss[0].str;
        lpssClone[1].str = lpss[1].str;
        Assert.True(LPStrBuffer_InOutByRef_Array_Struct(ref lpss));
        Assert.NotEqual(lpss[0].str, lpssClone[0].str);

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        lpssClone = new LPStrTestStruct[2];
        lpssClone[0].str = lpss[0].str;
        lpssClone[1].str = lpss[1].str;
        Assert.True(LPStrBuffer_InOutByRef_Array_Struct(ref lpss));
        Assert.Equal(lpss[0].str, lpssClone[0].str);
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping=false, ThrowOnUnmappableChar=false");
        testLPStrBufferString();
        testLPStrBufferStringBuilder();
        testLPStrBufferStruct();
        testLPStrBufferArray();
        testLPStrBufferClass();
        testLPStrBufferArrayOfStructs();
    }
}
