// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

interface IFoo<T> 
{	
	string Function<U>(U u, T t);		
}

class Foo<T> : IFoo<T>
{
	public string Function<U>(U u, T t)
	{
		return u.ToString()+t.ToString();
	}		
}

public class Test_method008
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
		IFoo<int> IFooInt = new Foo<int>();
		IFoo<string> IFooString = new Foo<string>();

		Eval(IFooInt.Function<int>(1,1).Equals("11"));
		Eval(IFooInt.Function<string>("string",1).Equals("string1"));

		Eval(IFooString.Function<int>(1,"string").Equals("1string"));
		Eval(IFooString.Function<string>("string1","string2").Equals("string1string2"));
		
		
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

