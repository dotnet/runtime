// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base<U>
{
	public virtual T Function<T>(T i)
	{
		return default(T);
	}
}

public class FooInt : Base<int>
{
	public override T Function<T>(T i)
	{
		return i;
	}	
}


public class FooObject : Base<object>
{
	public override T Function<T>(T i)
	{
		return i;
	}
		
}

public class FooString : Base<string>
{
	public override T Function<T>(T i)
	{
		return i;
	}
		
}

public class Test_method001h
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
		Base<int> fInt = new FooInt();
		Eval(fInt.Function<int>(1).Equals(1));
		Eval(fInt.Function<string>("string").Equals("string"));

		Base<object> fObject = new FooObject();
		Eval(fObject.Function<int>(1).Equals(1));
		Eval(fObject.Function<string>("string").Equals("string"));
		
		Base<string> fString = new FooString();
		Eval(fString.Function<int>(1).Equals(1));
		Eval(fString.Function<string>("string").Equals("string"));

		
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
