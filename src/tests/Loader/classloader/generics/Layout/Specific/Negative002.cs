// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Auto)]
public class GenBase<T>
{
	T t;
	T Dummy(T t) { this.t = t; return t;}
}

[StructLayout(LayoutKind.Sequential)]
public class Gen<T> : GenBase<T>
{
	T Dummy(T t) { return t;}
}

public class GenTest<T>
{
	private Gen<T> InternalTest()
	{
		return new Gen<T>();
	}

	private void IndirectTest()
	{
		InternalTest();
	}
	public bool Test_Negative002()
	{
		try
		{
			IndirectTest();
			Console.WriteLine("Test did not throw expected TypeLoadException");
			return false;
		}
		catch(TypeLoadException)
		{
			return true;
		}
		catch(Exception E)
		{
			Console.WriteLine("Test caught unexpected Exception " + E);
			return false;
		}
	}
}

public class Test_Negative002
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

		Eval(new GenTest<int>().Test_Negative002());
		Eval(new GenTest<double>().Test_Negative002());
		Eval(new GenTest<Guid>().Test_Negative002());
		Eval(new GenTest<string>().Test_Negative002());
		Eval(new GenTest<object>().Test_Negative002());
		
		
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











