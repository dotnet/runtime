// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

[assembly: BestFitMapping(false, ThrowOnUnmappableChar = false)]

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

public class BFM_LPStrMarshaler
{
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

    static String GetValidString()
    {
        return "This is the initial test string.";
    }

    static String GetInvalidString()
    {
        StringBuilder sbl = new StringBuilder();
        sbl.Append((char)0x2216);
        sbl.Append((char)0x2044);
        sbl.Append((char)0x2215);
        sbl.Append((char)0x0589);
        sbl.Append((char)0x2236);
        sbl.Append('乀');
        return sbl.ToString();
    }

    static StringBuilder GetValidStringBuilder()
    {
        StringBuilder sb = new StringBuilder("test string.");
        return sb;
    }

    static StringBuilder GetInvalidStringBuilder()
    {
        StringBuilder sbl = new StringBuilder();
        sbl.Append((char)0x2216);
        sbl.Append((char)0x2044);
        sbl.Append((char)0x2215);
        sbl.Append((char)0x0589);
        sbl.Append((char)0x2236);
        sbl.Append('乀');
        return sbl;
    }

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

    static String[] GetValidArray()
    {
        String[] s = new String[3];

        s[0] = GetValidString();
        s[1] = GetValidString();
        s[2] = GetValidString();

        return s;
    }

    static String[] GetInvalidArray()
    {
        String[] s = new String[3];

        s[0] = GetInvalidString();
        s[1] = GetInvalidString();
        s[2] = GetInvalidString();

        return s;
    }

    static void testLPStrBufferArray()
    {
        String[] s = GetInvalidArray();
        Assert.True(LPStrBuffer_In_Array_String(s));

        s = GetValidArray();
        Assert.True(LPStrBuffer_In_Array_String(s));

        s = GetInvalidArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref s));

        s = GetValidArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref s));

        s = GetInvalidArray();
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref s));

        s = GetValidArray();
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

    static void runTest()
    {
        testLPStrBufferString();
        testLPStrBufferStringBuilder();
        testLPStrBufferStruct();
        testLPStrBufferArray();
        testLPStrBufferClass();
        testLPStrBufferArrayOfStructs();
    }

    public static int Main()
    {
        if (System.Globalization.CultureInfo.CurrentCulture.Name != "en-US")
        {
            Console.WriteLine("Non-US English platforms are not supported.\nPassing without running tests");

            Console.WriteLine("--- Success");
            return 100;
        }

        try
        {
            runTest();
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }
    }
}
