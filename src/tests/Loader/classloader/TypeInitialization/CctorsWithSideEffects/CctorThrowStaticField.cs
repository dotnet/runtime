// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

A .cctor has only one chance to run in any appdomain. 
If it fails, the 2nd time we try to access a static field we check if .cctor has been run. And it has, but failed so we fail again.

Test_CctorThrowStaticField throws an exception inside .cctor.
Try to access a static field twice.
Expected: Should return the same exception.

*/

using System;
using Xunit;


public class A 
{
	public static int i;
	
	static A()
	{
		Console.WriteLine("In A.cctor");

		A.i = 5;
		
		throw new Exception();
	}
}


public struct B 
{
	public static int i;
	
	static B()
	{
		Console.WriteLine("In B.cctor");

		B.i = 5;
		
		throw new Exception();
	}
}


public class Test_CctorThrowStaticField
{	
	[Fact]
	public static int TestEntryPoint()
	{ 
		bool result = true;
		
		try
		{
			Console.WriteLine("Accessing class's static field");
			Console.WriteLine("A.i: " +A.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 1st time");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 1st time: " + e);
			result = false;
		}


		try
		{
			Console.WriteLine("A.i: " +A.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 2nd time\n");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 2nd time: " + e);
			result = false;
		}


		Console.WriteLine("Accessing struct's static field");
		try
		{
			Console.WriteLine("B.i: " +B.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 1st time");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 1st time: " + e);
			result = false;
		}


		try
		{
			Console.WriteLine("B.i: " +B.i);
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			result = false;
		}
		catch (TypeInitializationException)
		{
			Console.WriteLine("Caught expected exception 2nd time\n");
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 2nd time: " + e);
			result = false;
		}

		if (result)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 101;
		}
		
	}
}
