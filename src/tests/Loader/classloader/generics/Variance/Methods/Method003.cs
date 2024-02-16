// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test tests constraints on method type parameters 
// for generic methods of generic variant interfaces

// POSITIVE tests

using System;
using Xunit;

public class C : IMinusT<int> { }
public class D : IMinusT<string[]> { }
public class E : IMinusT<object> { }



public class A5 : Test001PlusT<int>
{
	public void method1<M>(IMinusT<int> t) where M : IMinusT<int>
	{
	}
}

public class A6 : Test002PlusT<string>
{
	public string[] method2<M>(IMinusT<string[]> t) where M : IMinusT<string[]>
	{
		return new string[10];
	}
}

public class A7 : Test001MinusT<object>
{
	public IMinusT<object[]> method1<M>(object t) where M : IPlusT<object>
	{
		return (IMinusT<object[]>)new E();
	}
}


public class TestClass
{
	static int iTestCount = 0;
	static int iErrorCount = 0;
	static int iExitCode = 101;

	public static void Eval(string location, bool exp)
	{
		++iTestCount;

		if (!(exp))
		{
			iErrorCount++;
			Console.WriteLine("Test Failed at location: {0} @ count {1} ", location, iTestCount);
		}
	}
	

	public static void LoadTypeInternal(string testType)
	{
		switch (testType)
		{
			case "Test001PlusT": 
			{
				// positive test
				// return type: void
				// argument type: contravariant
				// method type constraint: contravariant

				Test001PlusT<int> test = (Test001PlusT<int>)new A5();
				test.method1<IMinusT<int>>((IMinusT<int>)new C());
				break;
			}

			case "Test002PlusT":
			{
				// positive test
				// return type: covariant
				// argument type: contravariant
				// method type constraint: contravariant

				Test002PlusT<string> test = (Test002PlusT<string>)new A6();
				string[] st = test.method2<IMinusT<string[]>>((IMinusT<string[]>)new D());
				break;
			}

				case "Test001MinusT":
			{
				// positive test
				// return type: covariant
				// argument type: contravariant
				// method type constraint: contravariant

				Test001MinusT<object> test = (Test001MinusT<object>)new A7();
				IMinusT<object[]> obj = test.method1<IPlusT<object>>(new object());
				break;
			}
	

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
		catch (MissingMethodException)
		{
			if (expected)
			{
				Console.WriteLine("Unexpected Exception MissingMethodException");
				return false;
			}
			else
			{
				return true;
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
		catch (Exception E)
		{
			Console.WriteLine("Unexpected Exception {0}", E);
			return false;
		}
	}

	private static bool RunTests()
	{
		// positive
		Eval("Test101", LoadType("Test001PlusT", true));
		Eval("Test102", LoadType("Test002PlusT", true));
		Eval("Test103", LoadType("Test001MinusT", true));

		if (iErrorCount > 0)
		{
			Console.WriteLine("Total test cases: " + iTestCount + "  Failed test cases: " + iErrorCount);
			return false;
		}
		else
		{
			Console.WriteLine("Total test cases: " + iTestCount);
			return true;
		}
	}

	[Fact]
	public static int TestEntryPoint()
	{

		if (RunTests())
		{
			iExitCode = 100;
			Console.WriteLine("All test cases passed");
		}
		else
		{
			iExitCode = 101;
			Console.WriteLine("Test failed");
		}
		return iExitCode;
	}

}
