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
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_String(GetInvalidString()));
        Assert.True(LPStrBuffer_In_String(GetValidString()));

        String cTemp = GetInvalidString();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_String(ref cTemp));

        cTemp = GetValidString();
        Assert.True(LPStrBuffer_InByRef_String(ref cTemp));

        cTemp = GetInvalidString();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_String(ref cTemp));

        cTemp = GetValidString();
        String cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_String(ref cTemp));
        Assert.Equal(cTempClone, cTemp);
    }

    static void testLPStrBufferStringBuilder()
    {
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_StringBuilder(GetInvalidStringBuilder()));
        Assert.True(LPStrBuffer_In_StringBuilder(GetValidStringBuilder()));

        StringBuilder cTemp = GetInvalidStringBuilder();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        Assert.True(LPStrBuffer_InByRef_StringBuilder(ref cTemp));

        cTemp = GetInvalidStringBuilder();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_StringBuilder(ref cTemp));

        cTemp = GetValidStringBuilder();
        StringBuilder cTempClone = cTemp;
        Assert.True(LPStrBuffer_InOutByRef_StringBuilder(ref cTemp));
        Assert.Equal(cTempClone.ToString(), cTemp.ToString() );
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
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_Struct_String(GetInvalidStruct()));
        Assert.True(LPStrBuffer_In_Struct_String(GetValidStruct()));

        LPStrTestStruct cTemp = GetInvalidStruct();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_Struct_String(ref cTemp));

        cTemp = GetValidStruct();
        Assert.True(LPStrBuffer_InByRef_Struct_String(ref cTemp));

        cTemp = GetInvalidStruct();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_Struct_String(ref cTemp));

        cTemp = GetValidStruct();
        LPStrTestStruct cTempClone = new LPStrTestStruct();
        cTempClone.str = cTemp.str;
        Assert.True(LPStrBuffer_InOutByRef_Struct_String(ref cTemp));
        Assert.Equal(cTempClone.str, cTemp.str);
    }

    static void testLPStrBufferClass()
    {
        LPStrTestClass cTest = new LPStrTestClass();
        cTest.str = GetInvalidString();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_Class_String(cTest));

        cTest.str = GetValidString();
        Assert.True(LPStrBuffer_In_Class_String(cTest));

        LPStrTestClass cTemp = new LPStrTestClass();
        cTemp.str = GetInvalidString();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_Class_String(ref cTemp));

        cTemp.str = GetValidString();
        Assert.True(LPStrBuffer_InByRef_Class_String(ref cTemp));

        cTemp.str = GetInvalidString();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_Class_String(ref cTemp));

        cTemp.str = GetValidString();
        LPStrTestClass cTempClone = new LPStrTestClass();
        cTempClone.str = cTemp.str;
        Assert.True(LPStrBuffer_InOutByRef_Class_String(ref cTemp));
        Assert.Equal(cTempClone.str, cTemp.str);
    }

    static void testLPStrBufferArray()
    {
        String[] cTest = null;
        cTest = GetInvalidStringArray();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_Array_String(cTest));

        cTest = GetValidStringArray();
        Assert.True(LPStrBuffer_In_Array_String(cTest));

        String[] cTemp = GetInvalidStringArray();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_Array_String(ref cTemp));

        cTemp = GetValidStringArray();
        Assert.True(LPStrBuffer_InByRef_Array_String(ref cTemp));

        cTemp = GetInvalidStringArray();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_Array_String(ref cTemp));

        cTemp = GetValidStringArray();
        String[] cTempClone = new String[3];
        cTempClone[0] = cTemp[0];
        Assert.True(LPStrBuffer_InOutByRef_Array_String(ref cTemp));
        Assert.Equal(cTempClone[0], cTemp[0]);
    }

    static void testLPStrBufferArrayOfStructs()
    {
        LPStrTestStruct[] lpss = null;
        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_In_Array_Struct(lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.True(LPStrBuffer_In_Array_Struct(lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        Assert.True(LPStrBuffer_InByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetInvalidStruct();
        lpss[1] = GetInvalidStruct();
        Assert.Throws<ArgumentException>(() => LPStrBuffer_InOutByRef_Array_Struct(ref lpss));

        lpss = new LPStrTestStruct[2];
        lpss[0] = GetValidStruct();
        lpss[1] = GetValidStruct();
        LPStrTestStruct[] lpssClone = new LPStrTestStruct[2];
        lpssClone[0].str = lpss[0].str;
        lpssClone[1].str = lpss[1].str;
        Assert.True(LPStrBuffer_InOutByRef_Array_Struct(ref lpss));
        Assert.Equal(lpss[0].str, lpssClone[0].str);
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
