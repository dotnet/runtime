// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Call a non-virtual instance method of a zero-constructed value type 
// The method doesn't access type's static fields.

using System;
using System.IO;
using Xunit;

public class FLAG
{
    public static bool success = false;
}


public struct A
{
	public static int i;

	static A()
	{

		Console.WriteLine("In A.cctor");
        FLAG.success = true;
	}

	public void methodA()
	{	
	}
}


public class Test_CctorZeroVal01
{
	[Fact]
	public static int TestEntryPoint()
	{

		Console.WriteLine("\n============================================================");
		Console.WriteLine("NOTE: This test will fail with NGEN");
		Console.WriteLine("We do not guarantee to execute static .cctor for structs");
		Console.WriteLine("unless the instance .ctor is explicitly called\n");
		Console.WriteLine("============================================================");

		try
		{	
			// this will trigger A::.cctor
			A a = new A();

			a.methodA();

            if(!FLAG.success)
			{
				Console.WriteLine("FAIL: Cctor wasn't called");
				return 101;
			}
			else
			{
				Console.WriteLine("PASS: Cctor was called");
				return 100;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			return 102;
		}
	}
}

