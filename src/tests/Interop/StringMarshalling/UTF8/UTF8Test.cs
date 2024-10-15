// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Xunit;

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
            throw new Exception("StringParameterInOut: nativeString != expectedNativeString ");
        }
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static extern string StringParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)]string s, int index);
    public static void TestOutStringParameter(string orgString, int index)
    {
        string passedString = orgString;
        string expectedNativeString = passedString;
        string nativeString = StringParameterInOut(passedString, index);
        if (!(nativeString == expectedNativeString))
        {
            throw new Exception("StringParameterInOut: nativeString != expectedNativeString ");
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
            throw new Exception("TestInOutStringBuilderParameter: nativeString != expectedNativeString ");
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
            throw new Exception("TestOutStringBuilderParameter: string != expectedString ");
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
            throw new Exception(string.Format( "TestReturnStringBuilder: nativeString {0} != expectedNativeString {1}",nativeString.ToString(),expectedReturn) );
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

    unsafe struct UnmanagedStruct
    {
      public fixed byte psz[8];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    struct ManagedStruct
    {
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
      public string str;
    }

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void TestStructWithUtf8Field(Utf8Struct utfStruct);

    [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetStringInStruct(ref Utf8Struct utfStruct, [MarshalAs(UnmanagedType.LPUTF8Str)] string str);

    public static void TestUTF8StructMarshalling(string[] utf8Strings)
    {
        Utf8Struct utf8Struct = new Utf8Struct();
        for (int i = 0; i < utf8Strings.Length; i++)
        {
            utf8Struct.FirstName = utf8Strings[i];
            utf8Struct.index = i;
            TestStructWithUtf8Field(utf8Struct);
        }
        if (!OperatingSystem.IsWindows())
         CompareWithUTF8Encoding();

        string testString = "StructTestString\uD83D\uDE00";

        SetStringInStruct(ref utf8Struct, testString);

        if (utf8Struct.FirstName != testString)
        {
            throw new Exception("Incorrect UTF8 string marshalled back from native to managed.");
        }
   }

   unsafe static void CompareWithUTF8Encoding()
   {
       // Compare results with UTF8Encoding
        UnmanagedStruct ums;
        ums.psz[0] = 0xFF;
        ums.psz[1] = (byte)'a';
        ums.psz[2] = (byte)'b';
        ums.psz[3] = (byte)'c';
        ums.psz[4] = (byte)'d';
        ums.psz[5] = 0;

        IntPtr ptr = (IntPtr)(&ums);
        ManagedStruct ms = Marshal.PtrToStructure<ManagedStruct>(ptr);
	string actual = ms.str;

        UTF8Encoding uTF8Encoding = new UTF8Encoding();
        byte [] b = new byte[5];
        b[0] = 0xFF;
        b[1] = (byte)'a';
        b[2] = (byte)'b';
        b[3] = (byte)'c';
        b[4] = (byte)'d';
        string expected = uTF8Encoding.GetString(b);
        if (actual != expected)
	{
           Console.WriteLine("Actual:" + actual + " Length:" + actual.Length);
           Console.WriteLine("Expected:" + expected + " Length:" + expected.Length);
           throw new Exception("UTF8Encoding.GetString doesn't match with Utf8 String Marshaller result");
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


public class Test
{
    //test strings
    public static string[] utf8Strings = {
                                "Managed",
                                 "S\u00EEne kl\u00E2wen durh die wolken sint geslagen" ,
                                 "\u0915\u093E\u091A\u0902 \u0936\u0915\u094D\u0928\u094B\u092E\u094D\u092F\u0924\u094D\u0924\u0941\u092E\u094D \u0964 \u0928\u094B\u092A\u0939\u093F\u0928\u0938\u094D\u0924\u093F \u092E\u093E\u092E\u094D",
                                 "\u6211\u80FD\u541E\u4E0B\u73BB\u7483\u800C\u4E0D\u4F24\u8EAB\u4F53",
                                 "\u10E6\u10DB\u10D4\u10E0\u10D7\u10E1\u10D8 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4, \u10DC\u10E3\u10D7\u10E3 \u10D9\u10D5\u10DA\u10D0 \u10D3\u10D0\u10DB\u10EE\u10E1\u10DC\u10D0\u10E1 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E1\u10DD\u10E4\u10DA\u10D8\u10E1\u10D0 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4, \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10E0\u10DD\u10DB\u10D0\u10E1\u10D0, \u10EA\u10D4\u10EA\u10EE\u10DA\u10E1, \u10EC\u10E7\u10D0\u10DA\u10E1\u10D0 \u10D3\u10D0 \u10DB\u10D8\u10EC\u10D0\u10E1\u10D0, \u10F0\u10D0\u10D4\u10E0\u10D7\u10D0 \u10D7\u10D0\u10DC\u10D0 \u10DB\u10E0\u10DD\u10DB\u10D0\u10E1\u10D0; \u10DB\u10DD\u10DB\u10EA\u10DC\u10D4\u10E1 \u10E4\u10E0\u10D7\u10D4\u10DC\u10D8 \u10D3\u10D0 \u10D0\u10E6\u10D5\u10E4\u10E0\u10D8\u10DC\u10D3\u10D4, \u10DB\u10D8\u10D5\u10F0\u10EE\u10D5\u10D3\u10D4 \u10DB\u10D0\u10E1 \u10E9\u10D4\u10DB\u10E1\u10D0 \u10DC\u10D3\u10DD\u10DB\u10D0\u10E1\u10D0, \u10D3\u10E6\u10D8\u10E1\u10D8\u10D7 \u10D3\u10D0 \u10E6\u10D0\u10DB\u10D8\u10D7 \u10D5\u10F0\u10EE\u10D4\u10D3\u10D5\u10D8\u10D3\u10D4 \u10DB\u10D6\u10D8\u10E1\u10D0 \u10D4\u10DA\u10D5\u10D0\u10D7\u10D0 \u10D9\u10E0\u10D7\u10DD\u10DB\u10D0\u10D0\u10E1\u10D0\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,",
                                 "\u03A4\u03B7 \u03B3\u03BB\u03CE\u03C3\u03C3\u03B1 \u03BC\u03BF\u03C5 \u03AD\u03B4\u03C9\u03C3\u03B1\u03BD \u03B5\u03BB\u03BB\u03B7\u03BD\u03B9\u03BA\u03AE",
                                 null,
                               };

    [Fact]
    [OuterLoop]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void TestEntryPoint()
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
    }
}
