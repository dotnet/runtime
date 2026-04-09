// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;


// UTF8
class UTF8StringTests
{
	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.LPUTF8Str)]
	public static extern string StringParameterInOut([In, Out][MarshalAs(UnmanagedType.LPUTF8Str)]string s, int index);
	public static bool TestInOutStringParameter(string orgString, int index)
	{
		string passedString = orgString;
		string expectedNativeString = passedString;

		string nativeString = StringParameterInOut(passedString, index);
		if (!(nativeString == expectedNativeString))
		{
			Console.WriteLine("StringParameterInOut: nativeString != expectedNativeString ");
			return false;
		}
		return true;
	}

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.LPUTF8Str)]
	public static extern string StringParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)]string s, int index);
	public static bool TestOutStringParameter(string orgString, int index)
	{
		string passedString = orgString;
		string expectedNativeString = passedString;
		string nativeString = StringParameterInOut(passedString, index);
		if (!(nativeString == expectedNativeString))
		{
			Console.WriteLine("StringParameterInOut: nativeString != expectedNativeString ");
			return false;
		}
		return true;
	}

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StringParameterRefOut([MarshalAs(UnmanagedType.LPUTF8Str)]out string s, int index);
	public static bool TestStringPassByOut(string orgString, int index)
	{
		// out string
		string expectedNative = string.Empty;
		StringParameterRefOut(out expectedNative, index);
		if (orgString != expectedNative)
		{
			Console.WriteLine ("TestStringPassByOut : expectedNative != outString");
			return false;
		}
		return true;
	}

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StringParameterRef([MarshalAs(UnmanagedType.LPUTF8Str)]ref string s, int index);
	public static bool TestStringPassByRef(string orgString, int index)
	{
		string orgCopy = new string(orgString.ToCharArray());
		StringParameterRef(ref orgString, index);
		if (orgString != orgCopy)
		{
			Console.WriteLine("TestStringPassByOut : string mismatch");
			return false;
		}
		return true;
	}

	public static bool EmptyStringTest()
	{
		StringParameterInOut(string.Empty, 0);
		StringParameterOut(string.Empty, 0);
		return true;
	}
}

// UTF8 stringbuilder
class UTF8StringBuilderTests
{
	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StringBuilderParameterInOut([In,Out][MarshalAs(UnmanagedType.LPUTF8Str)]StringBuilder s, int index);
	public static bool TestInOutStringBuilderParameter(string expectedString, int index)
	{
		StringBuilder nativeStrBuilder = new StringBuilder(expectedString);

		StringBuilderParameterInOut(nativeStrBuilder, index);

		if (!nativeStrBuilder.ToString().Equals(expectedString))
		{
			Console.WriteLine($"TestInOutStringBuilderParameter: nativeString != expectedNativeString index={index} got={nativeStrBuilder} and expected={expectedString} ");
			return false;
		}
		return true;
	}

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void StringBuilderParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)]StringBuilder s, int index);
	public static bool TestOutStringBuilderParameter(string expectedString, int index)
	{
		// string builder capacity
		StringBuilder nativeStringBuilder = new StringBuilder(expectedString.Length);

		StringBuilderParameterOut(nativeStringBuilder, index);

		if (!nativeStringBuilder.ToString().Equals(expectedString))
		{
			Console.WriteLine("TestOutStringBuilderParameter: string != expectedString ");
			return false;
		}
		return true;
	}


	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.LPUTF8Str,SizeConst = 512)]
	public static extern StringBuilder StringBuilderParameterReturn(int index);
	public static bool TestReturnStringBuilder(string expectedReturn, int index)
	{
		StringBuilder nativeString = StringBuilderParameterReturn(index);
		if (!expectedReturn.Equals(nativeString.ToString()))
		{
			Console.WriteLine(string.Format( "TestReturnStringBuilder: nativeString {0} != expectedNativeString {1}",nativeString.ToString(),expectedReturn) );
			return false;
		}
		return true;
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

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void TestStructWithUtf8Field(Utf8Struct utfStruct);
	public static bool  TestUTF8StructMarshalling(string[] utf8Strings)
	{
		Utf8Struct utf8Struct = new Utf8Struct();
		for (int i = 0; i < utf8Strings.Length; i++)
		{
			utf8Struct.FirstName = utf8Strings[i];
			utf8Struct.index = i;
			TestStructWithUtf8Field(utf8Struct);
		}
		return true;
	}
}

// UTF8 string as delegate parameter
class UTF8DelegateMarshalling
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void DelegateUTF8Parameter([MarshalAs(UnmanagedType.LPUTF8Str)]string utf8String, int index);

	[DllImport("libtest", CallingConvention = CallingConvention.Cdecl)]
	public static extern void Utf8DelegateAsParameter(DelegateUTF8Parameter param);

	static bool failed;
	public static bool TestUTF8DelegateMarshalling()
	{
		failed = false;
		Utf8DelegateAsParameter(new DelegateUTF8Parameter(Utf8StringCallback));

		return !failed;
	}

	public static void Utf8StringCallback(string nativeString, int index)
	{
		if (string.CompareOrdinal(nativeString, Test.utf8Strings[index]) != 0)
		{
			Console.WriteLine("Utf8StringCallback string do not match");
			failed = true;
		}
	}
}

class Test
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

	public static int Main(string[] args)
	{
		// Test string as [In,Out] parameter
		for (int i = 0; i < utf8Strings.Length; i++)
			if (!UTF8StringTests.TestInOutStringParameter(utf8Strings[i], i))
				return i+1;

		// Test string as [Out] parameter
		for (int i = 0; i < utf8Strings.Length; i++)
			if (!UTF8StringTests.TestOutStringParameter(utf8Strings[i], i))
				return i+100;

		for (int i = 0; i < utf8Strings.Length - 1; i++)
			if (!UTF8StringTests.TestStringPassByOut(utf8Strings[i], i))
				return i+200;

		for (int i = 0; i < utf8Strings.Length - 1; i++)
			if (!UTF8StringTests.TestStringPassByRef(utf8Strings[i], i))
				return i+300;


		// Test StringBuilder as [In,Out] parameter
		for (int i = 0; i < utf8Strings.Length - 1; i++)
			if (!UTF8StringBuilderTests.TestInOutStringBuilderParameter(utf8Strings[i], i))
				return i+400;

#if NOT_YET
		// This requires support for [Out] in StringBuilder

		// Test StringBuilder as [Out] parameter
		for (int i = 0; i < utf8Strings.Length - 1; i++){
			if (!UTF8StringBuilderTests.TestOutStringBuilderParameter(utf8Strings[i], i))
				return i+500;
		}

#endif

        	// utf8 string as struct fields
		if (!UTF8StructMarshalling.TestUTF8StructMarshalling(utf8Strings))
			return 600;

		// delegate
		try {
			UTF8DelegateMarshalling.TestUTF8DelegateMarshalling();
		} catch (ExecutionEngineException){
			// Known issue on AOT - we do not AOT this yet.
		}

#if NOT_YET
		// This requires special support for StringBuilder return values
        	// Test StringBuilder as return value
        	for (int i = 0; i < utf8Strings.Length - 1; i++)
			if (!UTF8StringBuilderTests.TestReturnStringBuilder(utf8Strings[i], i))
				return 700+i;
#endif
        	// String.Empty tests
		if (!UTF8StringTests.EmptyStringTest())
			return 800;

		return 0;
	}
}
