// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

A .cctor has only one chance to run in any appdomain. 
If it fails, the 2nd time we try to access a static field we check if .cctor has been run. And it has, but failed so we fail again.

Test_CctorThrowMethodAccess throws an exception inside .cctor.
Try to access a static method twice.
Expected: Should return the same exception.

*/

using System;
using Xunit;

// TEST1
// static method access
public class A 
{
	static A()
	{
		Console.WriteLine("In A.cctor");
		
		throw new Exception();
	}

	public static void methA()
	{}
}

// TEST2
// static method access
public struct B 
{
	static B()
	{
		Console.WriteLine("In B.cctor");
		
		throw new Exception();
	}

	public static void methB()
	{}
}

// TEST3
// instance constructor trigger
public class C
{	
	static C()
	{
		Console.WriteLine("In C.cctor");
		
		throw new Exception();
	}
}

// TEST4
// instance constructor trigger
public struct D
{
	static D()
	{
		Console.WriteLine("In D.cctor");
		
		throw new Exception();
	}

	public D(int i){}
}

/*
// this will fail due to current postponed bug so I've disabled it for now
// TEST5
// instance method trigger (for this null pointer)
public class E
{	
	static E()
	{
		Console.WriteLine("In E.cctor");
		
		throw new Exception();
	}

	public void methE()
	{}
}
*/

// TEST6
// instance method trigger (for zero initialized valuetype)
public struct F
{
	static F()
	{
		Console.WriteLine("In F.cctor");
		
		throw new Exception();
	}

	public void methF()
	{}
}

public interface IG
{
	void methG();
}

// TEST7
// virtual instance method trigger (for zero initialized valuetype)
public struct G : IG
{
	static G()
	{
		Console.WriteLine("In G.cctor");
		
		throw new Exception();
	}

	public void methG()
	{}
}



public class Test_CctorThrowMethodAccess
{	
	public static bool RunTest(string s)
	{
		bool result = true;
		
		switch (s)
		{
			case "A":
			{
				try
				{
					Console.WriteLine("Accessing class's static method");
					A.methA();
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
					A.methA();
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
			case "B":
			{
				try
				{
					Console.WriteLine("Accessing struct's static method");
					B.methB();
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
					B.methB();
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
			case "C":
			{
				try
				{
					Console.WriteLine("Instantiating class");

					C c = new C();

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
					C c = new C();

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
			case "D":
			{
				try
				{
					Console.WriteLine("Instantiating struct");
					D d = new D(6);

					// to get rid of compiler warning that var d is never used
					string str = d.ToString();
					
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
					D d = new D(6);

					// to get rid of compiler warning that var d is never used
					string str = d.ToString();
					
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
			case "F":
			{
				try
				{
					Console.WriteLine("Accessing struct's instance method (zero initialized struct)");
					F f = new F();

					f.methF();
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
					F f = new F();

					f.methF();
					
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
			case "G":
			{
				try
				{
					Console.WriteLine("Accessing struct's virtual instance method (zero initialized struct)");
					IG g = new G();

					g.methG();
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
					IG g = new G();

					g.methG();
					
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
			default :
				return false;
		}
	}

	
	[Fact]
	public static int TestEntryPoint()
	{ 
		bool pass = true;
		

		Console.WriteLine("\n============================================================");
		Console.WriteLine("NOTE: This test will fail with NGEN");
		Console.WriteLine("We do not guarantee to execute static .cctor for structs");
		Console.WriteLine("unless the instance .ctor is explicitly called\n");
		Console.WriteLine("============================================================");


		// run tests
		if (!RunTest(typeof(A).ToString()))
			pass = false;
		
		if (!RunTest(typeof(B).ToString()))
			pass = false;
		
		if (!RunTest(typeof(C).ToString()))
			pass = false;
		
		if (!RunTest(typeof(D).ToString()))
			pass = false;

		if (!RunTest(typeof(F).ToString()))
			pass = false;
		
		if (!RunTest(typeof(G).ToString()))
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
