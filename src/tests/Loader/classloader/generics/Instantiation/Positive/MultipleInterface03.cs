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
	string m(T[] tArr);
}

class GenInt : IGenX<int[]>, IGenY<int> 
{
	string IGenX<int[]>.m(int[] t)
	{
   		return "IGenX.m";
  	}
  	string IGenY<int>.m(int[] tArr) 
  	{
    		return "IGenY.m";
  	}
}

class GenString : IGenX<string[]>, IGenY<string> 
{
	string IGenX<string[]>.m(string[] t)
	{
   		return "IGenX.m";
  	}
  	string IGenY<string>.m(string[] tArr) 
  	{
    		return "IGenY.m";
  	}
}

public class Test_MultipleInterface03
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
		Eval(((IGenX<int[]>)IGenInt).m(null).Equals("IGenX.m"));
		Eval(((IGenY<int>)IGenInt).m(null).Equals("IGenY.m"));
		
		GenString IGenString = new GenString();
		Eval(((IGenX<string[]>)IGenString).m(null).Equals("IGenX.m"));
		Eval(((IGenY<string>)IGenString).m(null).Equals("IGenY.m"));
		
		
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

