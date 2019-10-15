// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

[assembly: BestFitMapping(true, ThrowOnUnmappableChar = true)]

public class BFM_CharMarshaler
{
    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool Char_In([In]char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool Char_InByRef([In]ref char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool Char_InOutByRef([In, Out]ref char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_In_String([In]String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_InByRef_String([In]ref String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_InOutByRef_String([In, Out]ref String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_In_StringBuilder([In]StringBuilder s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_InByRef_StringBuilder([In]ref StringBuilder s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = true)]
    public static extern bool CharBuffer_InOutByRef_StringBuilder([In, Out]ref StringBuilder s);

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

    static char GetInvalidChar()
    {
        return (char)0x2216;
    }

    static char GetValidChar()
    {
        return 'c';
    }

    static void testChar()
    {
        Assert.IsTrue(Char_In(GetInvalidChar()), "[Error] Location tc1");

        Assert.IsTrue(Char_In(GetValidChar()), "[Error] Location tc2");

        char cTemp = GetInvalidChar();
        char cTempClone = GetInvalidChar();
        Assert.IsTrue(Char_InByRef(ref cTemp), "[Error] Location tc3");

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InByRef(ref cTemp), "[Error] Location tc4");

        cTemp = GetInvalidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InOutByRef(ref cTemp), "[Error] Location tc5");

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InOutByRef(ref cTemp), "[Error] Location tc6");
    }

    static void testCharBufferString()
    {
        Assert.IsTrue(CharBuffer_In_String(GetInvalidString()), "[Error] Location tcbs1");

        Assert.IsTrue(CharBuffer_In_String(GetValidString()), "[Error] Location tcbs2");

        String cTemp = GetInvalidString();
        String cTempClone = GetInvalidString();
        Assert.IsTrue(CharBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs3");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs4");

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs5");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs6");
    }

    static void testCharBufferStringBuilder()
    {
        Assert.IsTrue(CharBuffer_In_StringBuilder(GetInvalidStringBuilder()), "[Error] Location tcbsb1");

        Assert.IsTrue(CharBuffer_In_StringBuilder(GetValidStringBuilder()), "[Error] Location tcbsb2");

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb3");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb4");

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb5");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb6");
    }

    static void runTest()
    {
        testChar();
        testCharBufferString();
        testCharBufferStringBuilder();
    }

    public static int Main()
    {
        if (System.Globalization.CultureInfo.CurrentCulture.Name != "en-US")
        {
            Console.WriteLine("Non english platforms are not supported");
            Console.WriteLine("passing without running tests");

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