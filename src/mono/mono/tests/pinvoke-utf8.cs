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
			Console.WriteLine("StringParameterInOut: nativeString != expecedNativeString ");
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
		string expecedNativeString = passedString;
		string nativeString = StringParameterInOut(passedString, index);
		if (!(nativeString == expecedNativeString))
		{
			Console.WriteLine("StringParameterInOut: nativeString != expecedNativeString ");
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
			Console.WriteLine($"TestInOutStringBuilderParameter: nativeString != expecedNativeString index={index} got={nativeStrBuilder} and expected={expectedString} ");
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
			Console.WriteLine("TestOutStringBuilderParameter: string != expecedString ");
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
			Console.WriteLine(string.Format( "TestReturnStringBuilder: nativeString {0} != expecedNativeString {1}",nativeString.ToString(),expectedReturn) );
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
