// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public class PInvoke_True_False
{
    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(true, ThrowOnUnmappableChar = false)]
    public struct LPStrTestStruct
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(true, ThrowOnUnmappableChar = false)]
    public class LPStrTestClass
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

#pragma warning disable 618
    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_String([In][MarshalAs(UnmanagedType.LPStr)]String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_String([In][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_String([In, Out][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_StringBuilder([In, Out][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Struct_String([In][MarshalAs(UnmanagedType.Struct)]LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Struct_String([In][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Struct_String([In, Out][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Array_String([In][MarshalAs(UnmanagedType.LPArray)]String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Array_String([In][MarshalAs(UnmanagedType.LPArray)]ref String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Array_String([In, Out][MarshalAs(UnmanagedType.LPArray)]ref String[] Array);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Class_String([In, Out][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_Array_Struct([In, Out][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);
#pragma warning restore 618

    static void testLPStrBufferString()
    {
        Assert.True(LPStrBuffer_In_String(GetInvalidString()));

        Assert.True(LPStrBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.True(LPStrBuffer_InByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_String(ref cTemp));
        Assert.NotEqual(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testLPStrBufferStringBuilder()
    {
        Assert.True(LPStrBuffer_In_StringBuilder(GetInvalidStringBuilder()));

        Assert.True(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(LPStrBuffer_InByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(LPStrBuffer_InByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
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
        Assert.True(LPStrBuffer_In_Struct_String(GetInvalidStruct()));

        Assert.True(LPStrBuffer_In_Struct_String(GetValidStruct()));

        LPStrTestStruct lpss = GetInvalidStruct();
        Assert.True(LPStrBuffer_InByRef_Struct_String(ref lpss));

        lpss = GetValidStruct();
        Assert.True(LPStrBuffer_InByRef_Struct_String(ref lpss));

        lpss = GetInvalidStruct();
        Assert.True(LPStrBuffer_InOutByRef_Struct_String(ref lpss));

        lpss = GetValidStruct();
        Assert.True(LPStrBuffer_InOutByRef_Struct_String(ref lpss));
    }

    static void testLPStrBufferArray()
    {
        String[] s = GetInvalidStringArray();
        Assert.True(LPStrBuffer_In_Array_String(s));

        s = GetValidStringArray();
        Assert.True(LPStrBuffer_In_Array_String(s));

        s = GetInvalidStringArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref s));

        s = GetValidStringArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref s));

        s = GetInvalidStringArray();
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref s));

        s = GetValidStringArray();
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref s));
    }

    static void testLPStrBufferClass()
    {
        LPStrTestClass sClass = new LPStrTestClass();
        sClass.str = GetInvalidString();
        Assert.True(LPStrBuffer_In_Class_String(sClass));

        sClass.str = GetValidString();
        Assert.True(LPStrBuffer_In_Class_String(sClass));

        sClass.str = GetInvalidString();
        Assert.True(LPStrBuffer_InByRef_Class_String(ref sClass));

        sClass.str = GetValidString();
        Assert.True(LPStrBuffer_InByRef_Class_String(ref sClass));

        sClass.str = GetInvalidString();
        Assert.True(LPStrBuffer_InOutByRef_Class_String(ref sClass));

        sClass.str = GetValidString();
        Assert.True(LPStrBuffer_InOutByRef_Class_String(ref sClass));
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
        Assert.True(LPStrBuffer_InOutByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.True(LPStrBuffer_InOutByRef_Array_Struct(ref lpss));
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping=true, ThrowOnUnmappableChar=false");
        testLPStrBufferString();
        testLPStrBufferStringBuilder();
        testLPStrBufferStruct();
        testLPStrBufferArray();
        testLPStrBufferClass();
        testLPStrBufferArrayOfStructs();
    }
}
