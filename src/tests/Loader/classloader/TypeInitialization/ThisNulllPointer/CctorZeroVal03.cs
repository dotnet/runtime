// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Call a non-virtual instance method of a zero-constructed value type 
// The method accesses type's instance fields.

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
		i = 5;	
		
	}
}


public class Test_CctorZeroVal03
{
	[Fact]
	public static int TestEntryPoint()
	{
 			
		try
		{	
			A a = new A();
			
			a.methodA();

            if (!FLAG.success)
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
