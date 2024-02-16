// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

class Foo<U>
{
	public string Function<T>(U u,T t)
	{
		return u.ToString()+t.ToString();
	}
		
}

public class Test_method002
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

		Eval(new Foo<int>().Function<int>(1,1).Equals("11"));
		Eval(new Foo<string>().Function<int>("string",1).Equals("string1"));
		Eval(new Foo<int>().Function<string>(1,"string").Equals("1string"));
		Eval(new Foo<string>().Function<string>("string1","string2").Equals("string1string2"));
		
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
