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
			case "Test101PlusT" : return typeof(Test101PlusT<int>); 
			case "Test102PlusT" : return typeof(Test102PlusT<string>); 
			case "Test103PlusT" : return typeof(Test103PlusT<object>); 
			case "Test104PlusT" : return typeof(Test104PlusT<Base>); 
			case "Test105PlusT" : return typeof(Test105PlusT<GVal<Sub[]>>);

			case "Test101MinusT" : return typeof(Test101MinusT<int>); 
			case "Test102MinusT" : return typeof(Test102MinusT<string>); 
			case "Test103MinusT" : return typeof(Test103MinusT<object>); 
			case "Test104MinusT" : return typeof(Test104MinusT<Base>); 
			case "Test105MinusT" : return typeof(Test105MinusT<GVal<Sub[]>>);
			
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
		Eval("Test001", LoadType("Test101PlusT", true));
		Eval("Test002", LoadType("Test102PlusT", true));
		Eval("Test003", LoadType("Test103PlusT", true));
		Eval("Test004", LoadType("Test104PlusT", true));
		Eval("Test005", LoadType("Test105PlusT", true));

		Eval("Test101", LoadType("Test101MinusT", true));
		Eval("Test102", LoadType("Test102MinusT", true));
		Eval("Test103", LoadType("Test103MinusT", true));
		Eval("Test104", LoadType("Test104MinusT", true));
		Eval("Test105", LoadType("Test105MinusT", true));
			
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
