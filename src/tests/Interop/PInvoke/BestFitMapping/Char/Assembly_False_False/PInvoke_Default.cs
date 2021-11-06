// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public partial class PInvoke_Default
{
    static void testChar()
    {
        Assert.True(Char_In(GetInvalidChar()));

        Assert.True(Char_In(GetValidChar()));

        char cTemp = GetInvalidChar();
        Assert.True(Char_InByRef(ref cTemp));

        cTemp = GetValidChar();
        Assert.True(Char_InByRef(ref cTemp));

        cTemp = GetInvalidChar();
        Assert.True(Char_InOutByRef(ref cTemp));
        Assert.Equal('?', cTemp);

        cTemp = GetValidChar();
        char cTempClone = cTemp;
        Assert.True(Char_InOutByRef(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testCharBufferString()
    {
        Assert.True(CharBuffer_In_String(GetInvalidString()));

        Assert.True(CharBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        Assert.True(CharBuffer_InByRef_String(ref cTemp));

        cTemp = GetValidString();
        Assert.True(CharBuffer_InByRef_String(ref cTemp));

        cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));
        Assert.NotEqual(cTempClone, cTemp);

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testCharBufferStringBuilder()
    {
        StringBuilder sb = GetInvalidStringBuilder();
        Assert.True(CharBuffer_In_StringBuilder(sb));

        Assert.True(CharBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        Assert.True(CharBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.NotEqual(cTempClone.ToString(), cTemp.ToString());

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.True(CharBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString());
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping not set, ThrowOnUnmappableChar not set");
        testChar();
        testCharBufferString();
        testCharBufferStringBuilder();
    }
}
