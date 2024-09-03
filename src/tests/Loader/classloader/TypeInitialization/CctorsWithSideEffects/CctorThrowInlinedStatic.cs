// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

A .cctor has only one chance to run in any appdomain. 
If it fails, the 2nd time we try to access a static field we check if .cctor has been run. And it has, but failed so we fail again.

Test_CctorThrowInlinedStatic throws an exception inside .cctor.
Try to access a static method twice for inlined and not inlined methods.
Expected: Should return the same exception.

*/


using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

public class Foo
{
	public static void Meth_In()
	{
		// NotInlined.NotInlinedMeth is not inlined
		NotInlined.NotInlinedMeth();
	}

	public static void Meth_NotIn()
	{
		// Inlined.InlinedMeth is  inlined
		Inlined.InlinedMeth();
	}

	public static void ValMeth_In()
	{
		// NotInlinedVal.NotInlinedValMeth is not inlined
		NotInlinedVal.NotInlinedValMeth();
	}

	public static void ValMeth_NotIn()
	{
		// InlinedVal.InlinedValMeth is  inlined
		InlinedVal.InlinedValMeth();
	}
}

public class NotInlined
{

	static NotInlined()
	{
		Console.WriteLine("Inside NotInlined::.cctor");
		throw new Exception();
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedMeth()
	{
	}
}


public class Inlined
{

	static Inlined()
	{
		Console.WriteLine("Inside Inlined::.cctor");
		throw new Exception();
	}

	public static void InlinedMeth()
	{
	}
}


public struct NotInlinedVal
{

	static NotInlinedVal()
	{
		Console.WriteLine("Inside NotInlinedVal::.cctor");
		throw new Exception();
	}

	[MethodImpl(MethodImplOptions.NoInlining)] 
	public static void NotInlinedValMeth()
	{
	}
}


public class InlinedVal
{

	static InlinedVal()
	{
		Console.WriteLine("Inside InlinedVal::.cctor");
		throw new Exception();
	}

	public static void InlinedValMeth()
	{
	}
}

public class Test_CctorThrowInlinedStatic
{


	public static bool RunTest(int i)
	{
		bool result = true;
		
		switch (i)
		{
			case 1:
			{
				try
				{
					Console.WriteLine("Accessing class's inlined static method");
					Foo.Meth_In();
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
					Foo.Meth_In();
					Console.WriteLine("Did not catch expected TypeInitializationException exception\n");
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

				return result;
			}
			case 2:
			{
				try
				{
					Console.WriteLine("Accessing struct's inlined static method");
					Foo.ValMeth_In();
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
					Foo.ValMeth_In();
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

				return result;
			}
			case 3:
			{
				try
				{
					Console.WriteLine("Accessing class's not inlined static method");

					Foo.Meth_NotIn();

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
					Foo.Meth_NotIn();

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

				return result;
			}
			case 4:
			{
				try
				{
					Console.WriteLine("Accessing struct's not inlined static method");
					Foo.ValMeth_NotIn();

					
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
					Foo.ValMeth_NotIn();
					
					Console.WriteLine("Did not catch expected TypeInitializationException exception\n");
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

				return result;
			}
			
			default :
				return false;
		}
	}


	[Fact]
	public static int TestEntryPoint()
	{
		bool pass = true;
		
		// run tests
		if (!RunTest(1))
			pass = false;
		
		if (!RunTest(2))
			pass = false;
		
		if (!RunTest(3))
			pass = false;
		
		if (!RunTest(4))
			pass = false;

		if (pass)
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
