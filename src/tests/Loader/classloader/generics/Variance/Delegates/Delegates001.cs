// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Positive tests which test method signatures for generic covariant/contravariant 
// delegate's methods 
// Argument types have to be contravariant
// Return types have to be covariant

using System;
using Xunit;

public class Base { }
public class Sub : Base { }

public class GBase<T> { }
public class GSubGRefT<T> : GBase<GRef<T>> { }

public class GRef<T> { }
public struct GVal<T> { }

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
	public static Type LoadTypeInternal(string testType)
	{

		switch (testType)
		{
			case "Test001PlusT": return typeof(Test001PlusT<int>);
			case "Test002PlusT": return typeof(Test002PlusT<string>);
			case "Test003PlusT": return typeof(Test003PlusT<object>);
			case "Test004PlusT": return typeof(Test004PlusT<Base>);
			case "Test005PlusT": return typeof(Test005PlusT<GVal<Sub[]>>);

			case "Test001MinusT": return typeof(Test001MinusT<int>);
			case "Test002MinusT": return typeof(Test002MinusT<string>);
			case "Test003MinusT": return typeof(Test003MinusT<object>);
			case "Test004MinusT": return typeof(Test004MinusT<Base>);
			case "Test005MinusT": return typeof(Test005MinusT<GRef<Sub[]>>);

			case "Test001PlusTMinusU": return typeof(Test001PlusTMinusU<int,GSubGRefT<Sub[]>>);
			case "Test002PlusTMinusU": return typeof(Test002PlusTMinusU<int,GVal<string>[]>);
			case "Test003PlusTMinusU": return typeof(Test003PlusTMinusU<Base, Sub>);

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

	public static bool RunTests()
	{
		Eval("Test001", LoadType("Test001PlusT", true));
		Eval("Test002", LoadType("Test002PlusT", true));
		Eval("Test003", LoadType("Test003PlusT", true));
		Eval("Test004", LoadType("Test004PlusT", true));
		Eval("Test005", LoadType("Test005PlusT", true));

		Eval("Test101", LoadType("Test001MinusT", true));
		Eval("Test102", LoadType("Test002MinusT", true));
		Eval("Test103", LoadType("Test003MinusT", true));
		Eval("Test104", LoadType("Test004MinusT", true));
		Eval("Test105", LoadType("Test005MinusT", true));

		Eval("Test106", LoadType("Test001PlusTMinusU", true));
		Eval("Test107", LoadType("Test002PlusTMinusU", true));
		Eval("Test108", LoadType("Test003PlusTMinusU", true));

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
