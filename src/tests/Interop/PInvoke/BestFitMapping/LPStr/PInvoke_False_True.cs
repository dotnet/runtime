// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;

using static TestData;

namespace LPStr;

public class PInvoke_False_True
{
    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(false, ThrowOnUnmappableChar = true)]
    public struct LPStrTestStruct
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

    [StructLayout(LayoutKind.Sequential)]
    [BestFitMapping(false, ThrowOnUnmappableChar = true)]
    public class LPStrTestClass
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String str;
    }

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_String([In][MarshalAs(UnmanagedType.LPStr)]String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_String([In][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_String([In, Out][MarshalAs(UnmanagedType.LPStr)]ref String s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_StringBuilder([In][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_StringBuilder([In, Out][MarshalAs(UnmanagedType.LPStr)]ref StringBuilder s);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_Struct_String([In][MarshalAs(UnmanagedType.Struct)]LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_Struct_String([In][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_Struct_String([In, Out][MarshalAs(UnmanagedType.Struct)]ref LPStrTestStruct strStruct);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_Array_String([In][MarshalAs(UnmanagedType.LPArray)]String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_Array_String([In][MarshalAs(UnmanagedType.LPArray)]ref String[] strArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_Array_String([In, Out][MarshalAs(UnmanagedType.LPArray)]ref String[] Array);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_In_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InByRef_Class_String([In][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern bool LPStrBuffer_InOutByRef_Class_String([In, Out][MarshalAs(UnmanagedType.LPStruct)]ref LPStrTestClass strClass);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_In_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InByRef_Array_Struct([In][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);

    [DllImport("LPStr_BestFitMappingNative", BestFitMapping = true, ThrowOnUnmappableChar = false)]
    public static extern bool LPStrBuffer_InOutByRef_Array_Struct([In, Out][MarshalAs(UnmanagedType.LPArray)]ref LPStrTestStruct[] structArray);

    private static LPStrTestStruct GetInvalidStruct() => new LPStrTestStruct() { str = InvalidString };
    private static LPStrTestStruct GetUnmappableStruct() => new LPStrTestStruct() { str = UnmappableString };
    private static LPStrTestStruct GetValidStruct() => new LPStrTestStruct() { str = ValidString };

    public static unsafe void RunTest()
    {
        Console.WriteLine(" -- Validate P/Invokes: BestFitMapping=false, ThrowOnUnmappableChar=true");

        bool bestFitMapping = false;
        bool throwOnUnmappableChar = true;

        Test.ValidateString(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<string>(
                &LPStrBuffer_In_String,
                &LPStrBuffer_InByRef_String,
                &LPStrBuffer_InOutByRef_String));

        Test.ValidateStringBuilder(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<StringBuilder>(
                &LPStrBuffer_In_StringBuilder,
                &LPStrBuffer_InByRef_StringBuilder,
                &LPStrBuffer_InOutByRef_StringBuilder));

        Test.ValidateStringArray(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<string[]>(
                &LPStrBuffer_In_Array_String,
                &LPStrBuffer_InByRef_Array_String,
                &LPStrBuffer_InOutByRef_Array_String));

        Test.Validate(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<LPStrTestStruct>(
                &LPStrBuffer_In_Struct_String,
                &LPStrBuffer_InByRef_Struct_String,
                &LPStrBuffer_InOutByRef_Struct_String),
            new Test.DataContext<LPStrTestStruct, string>(
                GetInvalidStruct(),
                GetUnmappableStruct(),
                GetValidStruct(),
                (LPStrTestStruct s) => s.str));

        Test.Validate(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<LPStrTestClass>(
                &LPStrBuffer_In_Class_String,
                &LPStrBuffer_InByRef_Class_String,
                &LPStrBuffer_InOutByRef_Class_String),
            new Test.DataContext<LPStrTestClass, string>(
                new LPStrTestClass() { str = InvalidString },
                new LPStrTestClass() { str = UnmappableString },
                new LPStrTestClass() { str = ValidString },
                (LPStrTestClass s) => s.str));

        Test.Validate(
            bestFitMapping,
            throwOnUnmappableChar,
            new Test.Functions<LPStrTestStruct[]>(
                &LPStrBuffer_In_Array_Struct,
                &LPStrBuffer_InByRef_Array_Struct,
                &LPStrBuffer_InOutByRef_Array_Struct),
            new Test.DataContext<LPStrTestStruct[], string>(
                new LPStrTestStruct[] { GetInvalidStruct(), GetInvalidStruct() },
                new LPStrTestStruct[] { GetUnmappableStruct(), GetUnmappableStruct() },
                new LPStrTestStruct[] { GetValidStruct(), GetValidStruct() },
                (LPStrTestStruct[] s) => s[0].str));
    }
}
