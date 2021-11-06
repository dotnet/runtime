// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public class PInvoke_True_False
{
    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool Char_In([In]char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool Char_InByRef([In]ref char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool Char_InOutByRef([In, Out]ref char c);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_In_String([In]String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_InByRef_String([In]ref String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_InOutByRef_String([In, Out]ref String s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_In_StringBuilder([In]StringBuilder s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_InByRef_StringBuilder([In]ref StringBuilder s);

    [DllImport("Char_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool CharBuffer_InOutByRef_StringBuilder([In, Out]ref StringBuilder s);

    static void testChar()
    {
        Assert.True(Char_In(GetInvalidChar()));

        Assert.True(Char_In(GetValidChar()));

        char cTemp = GetInvalidChar();
        char cTempClone = cTemp;
        Assert.True(Char_InByRef(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.True(Char_InByRef(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetInvalidChar();
        cTempClone = cTemp;
        Assert.True(Char_InOutByRef(ref cTemp));
        Assert.NotEqual(cTempClone, cTemp);

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.True(Char_InOutByRef(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testCharBufferString()
    {
        Assert.True(CharBuffer_In_String(GetInvalidString()));

        Assert.True(CharBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));
        Assert.NotEqual(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testCharBufferStringBuilder()
    {
        Assert.True(CharBuffer_In_StringBuilder(GetInvalidStringBuilder()));

        Assert.True(CharBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.NotEqual(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping=true, ThrowOnUnmappableChar=false");
        testChar();
        testCharBufferString();
        testCharBufferStringBuilder();
    }
}
