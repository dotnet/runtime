// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Foo 
{
	internal protected T Function<T>(T i)
	{
		return i;
	}
		
}

public class FooSub : Foo
{
	public T FunctionX<T>(T i)
	{
		return base.Function<T>(i);
	}

}

public class Test_method001c 
{
	public static int counter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{
		FooSub t = new FooSub();

		Eval(t.FunctionX<int>(1).Equals(1));
		Eval(t.FunctionX<string>("string").Equals("string"));
		
		
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
		
	}
}
