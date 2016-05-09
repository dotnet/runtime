// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;


// UTF8 
class UTF8StringTests
{
    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static extern string StringParameterInOut([In, Out][MarshalAs(UnmanagedType.LPUTF8Str)]string s, int index);
    public static void TestInOutStringParameter(string orgString, int index)
    {
        string passedString = orgString;
        string expectedNativeString = passedString;

        string nativeString = StringParameterInOut(passedString, index);
        if (!(nativeString == expectedNativeString))
        {
            throw new Exception("StringParameterInOut: nativeString != expecedNativeString ");
        }
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static extern string StringParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)]string s, int index);
    public static void TestOutStringParameter(string orgString, int index)
    {
        string passedString = orgString;
        string expecedNativeString = passedString;
        string nativeString = StringParameterInOut(passedString, index);
        if (!(nativeString == expecedNativeString))
        {
            throw new Exception("StringParameterInOut: nativeString != expecedNativeString ");
        }
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StringParameterRefOut([MarshalAs(UnmanagedType.LPUTF8Str)]out string s, int index);
    public static void TestStringPassByOut(string orgString, int index)
    {
        // out string 
        string expectedNative = string.Empty;
        StringParameterRefOut(out expectedNative, index);
        if (orgString != expectedNative)
        {
            throw new Exception("TestStringPassByOut : expectedNative != outString");
        }
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StringParameterRef([MarshalAs(UnmanagedType.LPUTF8Str)]ref string s, int index);
    public static void TestStringPassByRef(string orgString, int index)
    {
        string orgCopy = new string(orgString.ToCharArray());
        StringParameterRef(ref orgString, index);
        if (orgString != orgCopy)
        {
            throw new Exception("TestStringPassByOut : string mismatch");
        }
    }

    public static void EmptyStringTest()
    {
        StringParameterInOut(string.Empty, 0);
        StringParameterOut(string.Empty, 0);
    }
}

// UTF8 stringbuilder
class UTF8StringBuilderTests
{
    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StringBuilderParameterInOut([In, Out][MarshalAs(UnmanagedType.LPUTF8Str)]StringBuilder s, int index);
    public static void TestInOutStringBuilderParameter(string expectedString, int index)
    {
        StringBuilder nativeStrBuilder = new StringBuilder(expectedString);
        StringBuilderParameterInOut(nativeStrBuilder, index);

        if (!nativeStrBuilder.ToString().Equals(expectedString))
        {
            throw new Exception("TestInOutStringBuilderParameter: nativeString != expecedNativeString ");
        }        
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StringBuilderParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)]StringBuilder s, int index);
    public static void TestOutStringBuilderParameter(string expectedString, int index)
    {
        // string builder capacity
        StringBuilder nativeStringBuilder = new StringBuilder(expectedString.Length);
        StringBuilderParameterOut(nativeStringBuilder, index);

        if (!nativeStringBuilder.ToString().Equals(expectedString))
        {
            throw new Exception("TestOutStringBuilderParameter: string != expecedString ");
        }
    }


    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str,SizeConst = 512)]
    public static extern StringBuilder StringBuilderParameterReturn(int index);
    public static void TestReturnStringBuilder(string expectedReturn, int index)
    {
        StringBuilder nativeString = StringBuilderParameterReturn(index);
        if (!expectedReturn.Equals(nativeString.ToString()))
        {
            throw new Exception(string.Format( "TestReturnStringBuilder: nativeString {0} != expecedNativeString {1}",nativeString.ToString(),expectedReturn) );
        }
    }
}

// UTF8 string as struct field
class UTF8StructMarshalling
{
    public struct Utf8Struct
    {
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string FirstName;
        public int index;
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TestStructWithUtf8Field(Utf8Struct utfStruct);
    public static void TestUTF8StructMarshalling(string[] utf8Strings)
    {
        Utf8Struct utf8Struct = new Utf8Struct();
        for (int i = 0; i < utf8Strings.Length; i++)
        {
            utf8Struct.FirstName = utf8Strings[i];
            utf8Struct.index = i;
            TestStructWithUtf8Field(utf8Struct);
        }
    }
}

// UTF8 string as delegate parameter
class UTF8DelegateMarshalling
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DelegateUTF8Parameter([MarshalAs(UnmanagedType.LPUTF8Str)]string utf8String, int index);


    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Utf8DelegateAsParameter(DelegateUTF8Parameter param);

   
    public static void TestUTF8DelegateMarshalling()
    {        
        Utf8DelegateAsParameter(new DelegateUTF8Parameter(Utf8StringCallback));
    }

    public static void Utf8StringCallback(string nativeString, int index)
    {
        if (string.CompareOrdinal(nativeString, Test.utf8Strings[index]) != 0)
        {
            throw new Exception("Utf8StringCallback string do not match");
        }
    }
}

class Test
{
    //test strings
    public static string[] utf8Strings = {
                                "Managed",
                                 "Sîne klâwen durh die wolken sint geslagen" ,
                                 "काचं शक्नोम्यत्तुम् । नोपहिनस्ति माम्",
                                 "我能吞下玻璃而不伤身体",
                                 "ღმერთსი შემვედრე,შემვედრე, ნუთუ კვლა დამხსნას შემვედრე,სოფლისა შემვედრე, შემვედრე,შემვედრე,შემვედრე,შრომასა, ცეცხლს, წყალსა და მიწასა, ჰაერთა თანა მრომასა; მომცნეს ფრთენი და აღვფრინდე, მივჰხვდე მას ჩემსა ნდომასა, დღისით და ღამით ვჰხედვიდე მზისა ელვათა კრთომაასაშემვედრე,შემვედრე,",
                                 "Τη γλώσσα μου έδωσαν ελληνική",
                                 null,
                               };

    public static int Main(string[] args)
    {
        // Test string as [In,Out] parameter
        for (int i = 0; i < utf8Strings.Length; i++)
            UTF8StringTests.TestInOutStringParameter(utf8Strings[i], i);

        // Test string as [Out] parameter
        for (int i = 0; i < utf8Strings.Length; i++)
            UTF8StringTests.TestOutStringParameter(utf8Strings[i], i);

        for (int i = 0; i < utf8Strings.Length - 1; i++)
            UTF8StringTests.TestStringPassByOut(utf8Strings[i], i);

        for (int i = 0; i < utf8Strings.Length - 1; i++)
            UTF8StringTests.TestStringPassByRef(utf8Strings[i], i);


        // Test StringBuilder as [In,Out] parameter
        for (int i = 0; i < utf8Strings.Length - 1; i++)
            UTF8StringBuilderTests.TestInOutStringBuilderParameter(utf8Strings[i], i);

        // Test StringBuilder as [Out] parameter
        for (int i = 0; i < utf8Strings.Length - 1; i++)
            UTF8StringBuilderTests.TestOutStringBuilderParameter(utf8Strings[i], i);

        // utf8 string as struct fields
        UTF8StructMarshalling.TestUTF8StructMarshalling(utf8Strings);

        // delegate
        UTF8DelegateMarshalling.TestUTF8DelegateMarshalling();

        // Test StringBuilder as [Out] parameter
        for (int i = 0; i < utf8Strings.Length - 1; i++)
            UTF8StringBuilderTests.TestReturnStringBuilder(utf8Strings[i], i);

        // String.Empty tests
        UTF8StringTests.EmptyStringTest();

        return 100;
    }
}