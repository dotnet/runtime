// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class A<T>{}
public struct S<T>{}

public abstract class Base<U>
{
	public abstract T Function<T>(T i);
}
public class Foo<U> : Base<U>
{
	public override T Function<T>(T i)
	{
		return i;
	}
		
}

public class Test_method001j
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
		Base<int> f = new Foo<int>();
		Eval(f.Function<int>(1).Equals(1));
		Eval(f.Function<string>("string").Equals("string"));

		Base<object> f2 = new Foo<object>();
		Eval(f2.Function<int>(1).Equals(1));
		Eval(f2.Function<string>("string").Equals("string"));
		

		Base<A<int>> f3 = new Foo<A<int>>();
		Eval(f3.Function<int>(1).Equals(1));
		Eval(f3.Function<string>("string").Equals("string"));

		Base<S<object>> f4 = new Foo<S<object>>();
		Eval(f4.Function<int>(1).Equals(1));
		Eval(f4.Function<string>("string").Equals("string"));

		
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
