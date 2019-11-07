// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

public class Test
{

	public static int Main()
	{
		int retVal = 100;
		
		int val  = 20;
		int adder= 5;
		int foo = Interlocked.Add(ref val, adder);
		Console.WriteLine("NewValue: " + val);
		Console.WriteLine("OldValue: " + foo);
		//if(foo != 20 && val != 25)
		if ((val != 25) || (foo != val))
			retVal = -1;

		long val1  = 20;
		long adder1= 5;
		long foo1 = Interlocked.Add(ref val1, adder1);
		Console.WriteLine("NewValue: " + val1);
		Console.WriteLine("OldValue: " + foo1);
		//if(foo1 != 20 && val1 != 25)
		if ((val1 != 25) || (foo1 != val1))
			retVal = -1;

		if (retVal == 100)
			Console.WriteLine("Test passed");
		else
			Console.WriteLine("Test failed");
		
		return retVal;
		
	}
}
