// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

using static TestData;

public partial class PInvoke_Default
{
    static void testChar()
    {
        Assert.IsTrue(Char_In(GetInvalidChar()), "[Error] Location tc1");

        Assert.IsTrue(Char_In(GetValidChar()), "[Error] Location tc2");

        char cTemp = GetInvalidChar();
        char cTempClone = cTemp;
        Assert.IsTrue(Char_InByRef(ref cTemp), "[Error] Location tc3");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tc4");

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InByRef(ref cTemp), "[Error] Location tc5");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tc6");

        cTemp = GetInvalidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InOutByRef(ref cTemp), "[Error] Location tc7");
        Assert.AreNotEqual(cTempClone, cTemp, "[Error] Location tc8");

        cTemp = GetValidChar();
        cTempClone = cTemp;
        Assert.IsTrue(Char_InOutByRef(ref cTemp), "[Error] Location tc9");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tc10");
    }

    static void testCharBufferString()
    {
        Assert.IsTrue(CharBuffer_In_String(GetInvalidString()), "[Error] Location tcbs1");

        Assert.IsTrue(CharBuffer_In_String(GetValidString()), "[Error] Location tcbs2");

        String cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs3");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tcbs4");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_String(ref cTemp), "[Error] Location tcbs5");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tcbs6");

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs7");
        Assert.AreNotEqual(cTempClone, cTemp, "[Error] Location tcbs8");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_String(ref cTemp), "[Error] Location tcbs9");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tcbs10");
    }

    static void testCharBufferStringBuilder()
    {
        Assert.IsTrue(CharBuffer_In_StringBuilder(GetInvalidStringBuilder()), "[Error] Location tcbsb1");

        Assert.IsTrue(CharBuffer_In_StringBuilder(GetValidStringBuilder()), "[Error] Location tcbsb2");

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb3");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb4");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb5");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb6");

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb7");
        Assert.AreNotEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb8");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(CharBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tcbsb9");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tcbsb10");
    }

    static char[] GetInvalidArray()
    {
        char[] c = new char[3];

        c[0] = (char)0x2216;
        c[1] = (char)0x2216;
        c[2] = (char)0x2216;

        return c;
    }

    static char[] GetValidArray()
    {
        char[] c = new char[3];

        c[0] = 'a';
        c[1] = 'b';
        c[2] = 'c';

        return c;
    }

    static void testCharArrayWithOffset()
    {
        char[] c = GetInvalidArray();
        ArrayWithOffset arrWOff_0 = new ArrayWithOffset(c, 0);
        Assert.IsTrue(Char_InOut_ArrayWithOffset(arrWOff_0), "[Error] Location ctlpsawo11");

        c = GetValidArray();
        ArrayWithOffset arrWOff_1 = new ArrayWithOffset(c, 1);
        Assert.IsTrue(Char_InOut_ArrayWithOffset(arrWOff_1), "[Error] Location ctlpsawo22");
    }

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping not set, ThrowOnUnmappableChar not set");
        testChar();
        testCharBufferString();
        testCharBufferStringBuilder();
        testCharArrayWithOffset();
    }
}
