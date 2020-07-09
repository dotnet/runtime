// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

[assembly: BestFitMapping(true, ThrowOnUnmappableChar = true)]

[StructLayout(LayoutKind.Sequential)]
//[BestFitMapping(true, ThrowOnUnmappableChar=true)]
public struct LPStrTestStruct
{
    [MarshalAs(UnmanagedType.LPStr)]
    public String str;
}

[StructLayout(LayoutKind.Sequential)]
//[BestFitMapping(true, ThrowOnUnmappableChar=true)]
public class LPStrTestClass
{
    [MarshalAs(UnmanagedType.LPStr)]
    public String str;
}

public class BFM_LPStrMarshaler
{
#pragma warning disable 618
    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_String([In][MarshalAs(UnmanagedType.LPStr)]String s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_String([In][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InOutByRef_String([In, Out][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InOutByRef_StringBuilder([In, Out][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_Struct_String([In][MarshalAs(UnmanagedType.Struct)]LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_Struct_String([In][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InOutByRef_Struct_String([In, Out][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_Array_String([In][MarshalAs(UnmanagedType.LPArray)]String[] strArray);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_Array_String([In][MarshalAs(UnmanagedType.LPArray)]ref String[] strArray);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InOutByRef_Array_String([In, Out][MarshalAs(UnmanagedType.LPArray)]ref String[] Array);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InOutByRef_Class_String([In, Out][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_In_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative")]
    public static extern bool LPStrBuffer_InByRef_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative")]
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
        //sbl.Append ('乀');

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
        //sbl.Append ('乀');
        return sbl;
    }

    static void testLPStrBufferString()
    {
        Assert.IsTrue(LPStrBuffer_In_String(GetInvalidString()), "[Error] Location tlpsbs1");
        Assert.IsTrue(LPStrBuffer_In_String(GetValidString()), "[Error] Location tlpsbs2");

        String cTemp = GetInvalidString();
        String cTempClone = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tlpsbs3");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tlpsbs4");

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tlpsbs5");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tlpsbs6");

    }

    static void testLPStrBufferStringBuilder()
    {
        Assert.IsTrue(LPStrBuffer_In_StringBuilder(GetInvalidStringBuilder()), "[Error] Location tlpsbsb1");
        Assert.IsTrue(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()), "[Error] Location tlpsbsb2");

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb3");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb4");

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb5");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb6");
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
        Assert.IsTrue(LPStrBuffer_In_Struct_String(GetInvalidStruct()), "[Error] Location tlpsbst1");
        Assert.IsTrue(LPStrBuffer_In_Struct_String(GetValidStruct()), "[Error] Location tlpsbst2");

        LPStrTestStruct lpss = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Struct_String(ref lpss), "[Error] Location tlpsbst3");

        lpss = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Struct_String(ref lpss), "[Error] Location tlpsbst4");

        lpss = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Struct_String(ref lpss), "[Error] Location tlpsbst5");

        lpss = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Struct_String(ref lpss), "[Error] Location tlpsbst6");
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
        Assert.IsTrue(LPStrBuffer_In_Array_String(s), "[Error] Location tlpsba1");

        s = GetValidArray();
        Assert.IsTrue(LPStrBuffer_In_Array_String(s), "[Error] Location tlpsba2");

        s = GetInvalidArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref s), "[Error] Location tlpsba3");

        s = GetValidArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref s), "[Error] Location tlpsba4");

        s = GetInvalidArray();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_String(ref s), "[Error] Location tlpsba5");

        s = GetValidArray();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_String(ref s), "[Error] Location tlpsba6");
    }

    static void testLPStrBufferClass()
    {
        LPStrTestClass sClass = new LPStrTestClass();
        sClass.str = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_In_Class_String(sClass), "[Error] Location tlpsbc1");

        sClass.str = GetValidString();
        Assert.IsTrue(LPStrBuffer_In_Class_String(sClass), "[Error] Location tlpsbc2");

        sClass.str = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_InByRef_Class_String(ref sClass), "[Error] Location tlpsbc3");

        sClass.str = GetValidString();
        Assert.IsTrue(LPStrBuffer_InByRef_Class_String(ref sClass), "[Error] Location tlpsbc4");

        sClass.str = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Class_String(ref sClass), "[Error] Location tlpsbc5");

        sClass.str = GetValidString();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Class_String(ref sClass), "[Error] Location tlpsbc6");
    }

    static void testLPStrBufferArrayOfStructs()
    {
        LPStrTestStruct[] lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_In_Array_Struct(lpss), "[Error] Location tlpsbaos1");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_In_Array_Struct(lpss), "[Error] Location tlpsbaos2");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_Struct(ref lpss), "[Error] Location tlpsbaos3");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_Struct(ref lpss), "[Error] Location tlpsbaos4");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_Struct(ref lpss), "[Error] Location tlpsbaos5");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_Struct(ref lpss), "[Error] Location tlpsbaos6");
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
