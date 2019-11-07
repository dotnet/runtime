// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential, Pack=8)]	
public struct GenStruct<T>
{
	T t;
	
	T Dummy(T t) { this.t = t;  return t;}
}

[StructLayout(LayoutKind.Explicit, Pack=8)]
public class NonGen 
{
	[FieldOffset(0)] 
	GenStruct<int> genStruct;

	[FieldOffset(0)] 
	int u;
	
	GenStruct<int> Dummy(GenStruct<int> t) { this.genStruct = t;  return t;}
	int Dummy(int u) { this.u= u; return u;}	
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
	public bool Test()
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

public class Test
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
	
	public static int Main()
	{

		Eval(new GenTest().Test());
		
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











