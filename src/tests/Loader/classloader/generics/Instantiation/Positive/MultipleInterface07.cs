// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public interface IGenX<T> 
{
	string m(T t);
}

public interface IGenY<T> 
{
	string m(T[] t);
}

class GenInt : IGenX<int[]>, IGenY<int> 
{
  	public string m(int[] t) 
  	{
    		return "m";
  	}
}

class GenString : IGenX<string[]>, IGenY<string> 
{
  	public string m(string[] t) 
  	{
    		return "m";
  	}
}

public class Test_MultipleInterface07
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

		GenInt IGenInt = new GenInt();
		Eval(((IGenX<int[]>)IGenInt).m(null).Equals("m"));
		Eval(((IGenY<int>)IGenInt).m(null).Equals("m"));
		
		GenString IGenString = new GenString();
		Eval(((IGenX<string[]>)IGenString).m(null).Equals("m"));
		Eval(((IGenY<string>)IGenString).m(null).Equals("m"));
		
		
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

