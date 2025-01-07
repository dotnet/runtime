// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}
public struct GVal<T> {}

public class TestClass
{
	static int iTestCount= 0;	
	static int iErrorCount= 0;	
	static int iExitCode = 101;

	public static void Eval(string location, bool exp)
	{
		++iTestCount;

		if ( !(exp))
		{
			iErrorCount++;
			Console.WriteLine("Test Failed at location: {0} @ count {1} ", location, iTestCount);
		}
	}
	public static Type LoadTypeInternal(string testType)
	{
		switch (testType)
		{
			case "Test001PlusT" : return typeof(Test001PlusT<int>); 
			case "Test002PlusT" : return typeof(Test002PlusT<string>); 
			case "Test003PlusT" : return typeof(Test003PlusT<object>); 
			case "Test004PlusT" : return typeof(Test004PlusT<Base>); 
			case "Test005PlusT" : return typeof(Test005PlusT<GVal<Sub[]>>);

			case "Test001MinusT" : return typeof(Test001MinusT<int>); 
			case "Test002MinusT" : return typeof(Test002MinusT<string>); 
			case "Test003MinusT" : return typeof(Test003MinusT<object>); 
			case "Test004MinusT" : return typeof(Test004MinusT<Base>); 
			case "Test005MinusT" : return typeof(Test005MinusT<GVal<Sub[]>>);
			
			default: throw new Exception("Unexpected testType");
		}
	}

	public static void LoadTypeWrapper(string testType)
	{
		LoadTypeInternal(testType);
	}
	
	public static bool LoadType(string testType, bool expected)
	{
		try
		{
			LoadTypeWrapper(testType);

			if (expected)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		catch (TypeLoadException)
		{
			if (expected)
			{
				Console.WriteLine("Unexpected Exception TypeLoadException");
				return false;
			}
			else
			{
				return true;
			}	
		}
		catch(Exception E)
		{
			Console.WriteLine("Unexpected Exception {0}", E);
			return false;
		}
	}
	
	private static bool RunTests()
	{
		Eval("Test001", LoadType("Test001PlusT", false));
		Eval("Test002", LoadType("Test002PlusT", false));
		Eval("Test003", LoadType("Test003PlusT", false));
		Eval("Test004", LoadType("Test004PlusT", false));
		Eval("Test005", LoadType("Test005PlusT", false));

		Eval("Test101", LoadType("Test001MinusT", false));
		Eval("Test102", LoadType("Test002MinusT", false));
		Eval("Test103", LoadType("Test003MinusT", false));
		Eval("Test104", LoadType("Test004MinusT", false));
		Eval("Test105", LoadType("Test005MinusT", false));
			
		if( iErrorCount > 0 )
		{
			Console.WriteLine( "Total test cases: " + iTestCount + "  Failed test cases: " + iErrorCount );
			return false;
		}
		else
		{
			Console.WriteLine( "Total test cases: " + iTestCount );
			return true;
		}	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{			
	
		if( RunTests() )
		{
			iExitCode = 100;
			Console.WriteLine( "All test cases passed" );
		}
		else
		{
			iExitCode = 101;
			Console.WriteLine( "Test failed" );
		}
		return iExitCode;
	}
	
}
