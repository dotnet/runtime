// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

using static TestData;

public partial class PInvoke_Default
{
    static void testLPStrBufferString()
    {
        Assert.IsTrue(LPStrBuffer_In_String(GetInvalidString()), "[Error] Location tlpsbs1");
        Assert.IsTrue(LPStrBuffer_In_String(GetValidString()), "[Error] Location tlpsbs2");

        String cTemp = GetInvalidString();
        String cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tlpsbs3");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tlpsbs4");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_String(ref cTemp), "[Error] Location tlpsbs5");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tlpsbs6");

        cTemp = GetInvalidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tlpsbs7");
        Assert.AreNotEqual(cTempClone, cTemp, "[Error] Location tlpsbs8");

        cTemp = GetValidString();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_String(ref cTemp), "[Error] Location tlpsbs9");
        Assert.AreEqual(cTempClone, cTemp, "[Error] Location tlpsbs10");
    }

    static void testLPStrBufferStringBuilder()
    {
        Assert.IsTrue(LPStrBuffer_In_StringBuilder(GetInvalidStringBuilder()), "[Error] Location tlpsbsb1");
        Assert.IsTrue(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()), "[Error] Location tlpsbsb2");

        StringBuilder cTemp = GetInvalidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb3");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tlpsbsb4");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb5");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tlpsbsb6");

        cTemp = GetInvalidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb7");
        Assert.AreNotEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tlpsbsb8");

        cTemp = GetValidStringBuilder();
        cTempClone = cTemp;
        Assert.IsTrue(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp), "[Error] Location tlpsbsb9");
        Assert.AreEqual(cTempClone.ToString(), cTemp.ToString(), "[Error] Location tlpsbsb10");
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

    static void testLPStrBufferArray()
    {
        String[] s = GetInvalidStringArray();
        Assert.IsTrue(LPStrBuffer_In_Array_String(s), "[Error] Location tlpsba1");

        s = GetValidStringArray();
        Assert.IsTrue(LPStrBuffer_In_Array_String(s), "[Error] Location tlpsba2");

        s = GetInvalidStringArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref s), "[Error] Location tlpsba3");

        s = GetValidStringArray();
        Assert.IsTrue(LPStrBuffer_InByRef_Array_String(ref s), "[Error] Location tlpsba4");

        s = GetInvalidStringArray();
        Assert.IsTrue(LPStrBuffer_InOutByRef_Array_String(ref s), "[Error] Location tlpsba5");

        s = GetValidStringArray();
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

    public static void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping not set, ThrowOnUnmappableChar not set");
        testLPStrBufferString();
        testLPStrBufferStringBuilder();
        testLPStrBufferStruct();
        testLPStrBufferArray();
        testLPStrBufferClass();
        testLPStrBufferArrayOfStructs();
    }
}
