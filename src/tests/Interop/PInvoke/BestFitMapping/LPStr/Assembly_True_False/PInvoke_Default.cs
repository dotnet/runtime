// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

using static TestData;

public partial class PInvoke_Default
{
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
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping not set, ThrowOnUnmappableChar not set");
        testLPStrBufferString();
        testLPStrBufferStringBuilder();
        testLPStrBufferStruct();
        testLPStrBufferArray();
        testLPStrBufferClass();
        testLPStrBufferArrayOfStructs();
    }
}
