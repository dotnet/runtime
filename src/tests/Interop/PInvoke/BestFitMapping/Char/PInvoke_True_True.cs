// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public class PInvoke_True_True
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

    static void testChar()
    {
        Assert.True(Char_In(GetInvalidChar()));

        Assert.True(Char_In(GetValidChar()));

        char cTemp = GetInvalidChar();
        char cTempClone = GetInvalidChar();
        Assert.True(Char_InByRef(ref cTemp));

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.True(Char_InByRef(ref cTemp));

        cTemp = GetInvalidChar();
        cTempClone = cTemp;
        Assert.True(Char_InOutByRef(ref cTemp));

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.True(Char_InOutByRef(ref cTemp));
    }

    static void testCharBufferString()
    {
        Assert.True(CharBuffer_In_String(GetInvalidString()));

        Assert.True(CharBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        String cTempClone = GetInvalidString();
        Assert.True(CharBuffer_InByRef_String(ref cTemp));

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_String(ref cTemp));

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));
    }

    static void testCharBufferStringBuilder()
    {
        Assert.True(CharBuffer_In_StringBuilder(GetInvalidStringBuilder()));

        Assert.True(CharBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping=true, ThrowOnUnmappableChar=true");
        testChar();
        testCharBufferString();
        testCharBufferStringBuilder();
    }
}
