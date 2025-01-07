// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test tests constraints on method type parameters 
// for generic methods of generic variant interfaces

// NEGATIVE tests

using System;
using Xunit;

public class Base { }

public class C : IMinusT<int> { }
public class D : IMinusT<string[]> { }
public class E : IMinusT<object> { }

public class F : IMinusT<Base> { }

public class A : Test101PlusT<int>
{
	public void method1<M>(IMinusT<int> t) where M : IPlusT<int>
	{
	}
}

public class A2 : Test102PlusT<string>
{
	public string[] method2<M>(IMinusT<string[]> t) where M : IPlusT<string>
	{
		return new string[10];
	}
}

public class A3 : Test101MinusT<object>
{
	public IMinusT<object[]> method1<M>(object t) where M : IMinusT<object>
	{
		return (IMinusT<object[]>)new E();
	}
}

public class A4 : Test102MinusT<Base>
{
	public IMinusT<Base>[] method2<M>() where M : IMinusT<Base>
	{
		return (IMinusT<Base>[])new F[10];
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
			case "Test101PlusT":
				{
					// negative test
					// return type: void
					// argument type: contravariant
					// method type constraint: covariant
					Test101PlusT<int> test = (Test101PlusT<int>)new A();
					test.method1<IPlusT<int>>((IMinusT<int>)new C());
					break;
				}
			case "Test102PlusT":
				{
					// negative test
					// return type: covariant
					// argument type: contravariant
					// method type constraint: covariant
					Test102PlusT<string> test = (Test102PlusT<string>)new A2();
					string[] st = test.method2<IPlusT<string>>((IMinusT<string[]>)new D());
					break;
				}

			case "Test101MinusT":
				{
					// negative test
					// return type: covariant
					// argument type: contravariant
					// method type constraint: covariant
					Test101MinusT<object> test = (Test101MinusT<object>)new A3();
					IMinusT<object[]> obj = test.method1<IMinusT<object>>(new object());
					break;
				}


			case "Test102MinusT":
				{
					// negative test
					// return type: covariant
					// argument type: void
					// method type constraint: covariant
					Test102MinusT<Base> test = (Test102MinusT<Base>)new A4();
					test.method2<IMinusT<Base>>();
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
		// negative
		Eval("Test001", LoadType("Test101PlusT", false));
		Eval("Test002", LoadType("Test102PlusT", false));
		Eval("Test003", LoadType("Test101MinusT", false));
		Eval("Test004", LoadType("Test102MinusT", false));

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
