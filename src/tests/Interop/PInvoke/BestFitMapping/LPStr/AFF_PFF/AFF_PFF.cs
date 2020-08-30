// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

[assembly: BestFitMapping(false, ThrowOnUnmappableChar = false)]

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

public class BFM_LPStrMarshaler
{
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
        Assert.IsTrue(LPStrBuffer_In_String(GetInvalidString()), "[Error] Location tcbs1");

        Assert.IsTrue(LPStrBuffer_In_String(GetValidString()), "[Error] Location tcbs2");

        String cTemp = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs3");

        cTemp = GetValidString();
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs4");

        cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs5");
        Assert.AreNotEqual(cTempClone, cTemp, "[Error] Location tcbs6");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs7");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tcbs88");
    }

    static void testLPStrBufferStringBuilder()
    {
        StringBuilder sb = GetInvalidStringBuilder();
        Assert.IsTrue(LPStrBuffer_In_StringBuilder(sb), "[Error] Location tcbsb1");

        Assert.IsTrue(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()), "[Error] Location tcbsb2");
;
        StringBuilder cTemp = GetInvalidStringBuilder();
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb3");

        cTemp = GetValidStringBuilder();
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb4");

        cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb5");
        Assert.AreNotEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb6");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb7");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb8");
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

    static void testLPStrBufferStruct()
    {
        LPStrTestStruct lpss = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_In_Struct_String(lpss), "[Error] Location tlpsbs1");

        Assert.IsTrue(LPStrBuffer_In_Struct_String(GetValidStruct()), "[Error] Location tlpsbs2");

        LPStrTestStruct cTemp = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Struct_String(ref cTemp), "[Error] Location tlpsbs3");

        cTemp = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Struct_String(ref cTemp), "[Error] Location tlpsbs4");

        cTemp = GetInvalidStruct();
        LPStrTestStruct cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_Struct_String(ref cTemp), "[Error] Location tlpsbs5");
        Assert.AreNotEqual(cTempClone.str, cTemp.str, "[Error] Location tlpsbs6");

        cTemp = GetValidStruct();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_Struct_String(ref cTemp), "[Error] Location tlpsbs7");
        Assert.AreEqual(cTempClone.str, cTemp.str, "[Error] Location tlpsbs8");
    }

    static void testLPStrBufferClass()
    {
        LPStrTestClass lpss = new LPStrTestClass();
        lpss.str = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_In_Class_String(lpss), "[Error] Location tlpsbc1");

        lpss.str = GetValidString();
        Assert.IsTrue(LPStrBuffer_In_Class_String(lpss), "[Error] Location tlpsbc2");

        LPStrTestClass cTemp = new LPStrTestClass();
        cTemp.str = GetInvalidString();
        Assert.IsTrue(LPStrBuffer_InByRef_Class_String(ref cTemp), "[Error] Location tlpsbc3");

        cTemp.str = GetValidString();
        Assert.IsTrue(LPStrBuffer_InByRef_Class_String(ref cTemp), "[Error] Location tlpsbc4");

        cTemp.str = GetInvalidString();
        LPStrTestClass cTempClone = new LPStrTestClass();
        cTempClone.str = cTemp.str;
        Assert.IsTrue(LPStrBuffer_InOutByRef_Class_String(ref cTemp), "[Error] Location tlpsbc5");
        Assert.AreNotEqual(cTempClone.str, cTemp.str, "[Error] Location tlpsbc6");

        cTemp.str = GetValidString();
        cTempClone.str = cTemp.str;
        Assert.IsTrue(LPStrBuffer_InOutByRef_Class_String(ref cTemp), "[Error] Location tlpsbc7");
        Assert.AreEqual(cTempClone.str, cTemp.str, "[Error] Location tlpsbc8");
    }

    static void testLPStrBufferArray()
    {
        String[] lpss = GetInvalidArray();
        Assert.IsTrue(LPStrBuffer_In_Array_String(lpss), "[Error] Location tlpsba1");

        Assert.IsTrue(LPStrBuffer_In_Array_String(GetValidArray()), "[Error] Location tlpsba2");

        String[] cTemp = GetInvalidArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref cTemp), "[Error] Location tlpsba3");

        cTemp = GetValidArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref cTemp), "[Error] Location tlpsba4");

        cTemp = GetInvalidArray();
        String[] cTempClone = new String[3];
        cTempClone[0] = cTemp[0];
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_String(ref cTemp), "[Error] Location tlpsba5");
        Assert.AreNotEqual(cTempClone[0], cTemp[0], "[Error] Location tlpsba6");

        cTemp = GetValidArray();
        cTempClone[0] = cTemp[0];
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_String(ref cTemp), "[Error] Location tlpsba7");
        Assert.AreEqual(cTempClone[0], cTemp[0], "[Error] Location tlpsba8");
    }

    static void testLPStrBufferArrayOfStructs()
    {
        LPStrTestStruct[] lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_In_Array_Struct(lpss), "[Error] Location tlpsba11");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_In_Array_Struct(lpss), "[Error] Location tlpsba22");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_Struct(ref lpss), "[Error] Location tlpsba33");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_Struct(ref lpss), "[Error] Location tlpsba44");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();

        LPStrTestStruct[] lpssClone = new LPStrTestStruct[2];
        lpssClone[0].str = lpss[0].str;
        lpssClone[1].str = lpss[1].str;

        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_Struct(ref lpss), "[Error] Location tlpsba55");
        Assert.AreNotEqual(lpssClone[0].str, lpss[0].str, "[Error] Location tlpsba66");

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();

        lpssClone = new LPStrTestStruct[2];
        lpssClone[0].str = lpss[0].str;
        lpssClone[1].str = lpss[1].str;

        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_Struct(ref lpss), "[Error] Location tlpsba77");
        Assert.AreEqual(lpssClone[0].str, lpss[0].str, "[Error] Location tlpsba88");
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
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
