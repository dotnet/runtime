// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Negative tests which test method signatures for generic covariant/contravariant 
// delegate's methods 

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
			case "Test001PlusT": return typeof(Test101PlusT<int>);
			case "Test002PlusT": return typeof(Test102PlusT<string>);
			case "Test003PlusT": return typeof(Test103PlusT<object>);
			case "Test004PlusT": return typeof(Test104PlusT<Base>);
			case "Test005PlusT": return typeof(Test105PlusT<GVal<Sub[]>>);

			case "Test001MinusT": return typeof(Test101MinusT<int>);
			case "Test002MinusT": return typeof(Test102MinusT<string>);
			case "Test003MinusT": return typeof(Test103MinusT<object>);
			case "Test004MinusT": return typeof(Test104MinusT<Base>);
			case "Test005MinusT": return typeof(Test105MinusT<GRef<Sub[]>>);

			case "Test001PlusTMinusU": return typeof(Test101PlusTMinusU<int,GSubGRefT<Sub[]>>);
			case "Test002PlusTMinusU": return typeof(Test102PlusTMinusU<int,GVal<string>[]>);
			case "Test003PlusTMinusU": return typeof(Test103PlusTMinusU<Base, Sub>);

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

		Eval("Test106", LoadType("Test001PlusTMinusU", false));
		Eval("Test107", LoadType("Test002PlusTMinusU", false));
		Eval("Test108", LoadType("Test003PlusTMinusU", false));

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
