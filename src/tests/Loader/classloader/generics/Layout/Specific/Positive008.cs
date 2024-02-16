// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;


[StructLayout(LayoutKind.Sequential, Pack=8)]	
public struct GenStruct<T>
{
	T t;
	
	T Dummy(T t) { this.t = t;  return t;}
}

[StructLayout(LayoutKind.Explicit, Pack=8)]
public struct NonGen 
{
	[FieldOffset(0)] 
	GenStruct<object> genStruct;

	[FieldOffset(0)] 
	object u;
	
	GenStruct<object> Dummy(GenStruct<object> t) { this.genStruct = t;  return t;}
	object Dummy(object u) { this.u= u; return u;}	
}

public class GenTest
{
	private NonGen InternalTest()
	{
		return new NonGen();
	}

	private void IndirectTest()
	{
		InternalTest();
	}
	public bool Test_Positive008()
	{
		try
		{
			IndirectTest();
			return true;
		}
		
		catch(Exception E)
		{
			Console.WriteLine("Test caught unexpected Exception " + E);
			return false;
		}
	}
}

public class Test_Positive008
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

		Eval(new GenTest().Test_Positive008());
		
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











