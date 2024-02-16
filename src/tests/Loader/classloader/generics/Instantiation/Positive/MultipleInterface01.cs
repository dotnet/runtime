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

class Gen<T> : IGenX<T[]>, IGenY<T> 
{
	string IGenX<T[]>.m(T[] t)
	{
   		return "IGenX.m";
  	}
  	string IGenY<T>.m(T[] tArr) 
  	{
    		return "IGenY.m";
  	}
}

public class Test_MultipleInterface01
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

		Gen<int> GenInt = new Gen<int>();
		Eval(((IGenX<int[]>)GenInt).m(null).Equals("IGenX.m"));
		Eval(((IGenY<int>)GenInt).m(null).Equals("IGenY.m"));
		
		Gen<string> GenString = new Gen<string>();
		Eval(((IGenX<string[]>)GenString).m(null).Equals("IGenX.m"));
		Eval(((IGenY<string>)GenString).m(null).Equals("IGenY.m"));
		
		
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

