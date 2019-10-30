// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is regression test for VSWhidbey 237932
// The issue here was that the ThreadStatic field was previously shared for all C1<objref> types
// and so when we created C1<System.OverflowException> and C1<System.InvalidCastException> the ThreadStatic
// field got incremented to 2, which is wrong.



using System;
using System.Threading;


public class Test
{
	public static int Main()
	{
		C1<System.OverflowException> cOverflow = new C1<System.OverflowException>();
		C1<System.InvalidCastException> cCast = new C1<System.InvalidCastException>();
	
		
		if (C1<System.OverflowException>.x == 1 && C1<System.InvalidCastException>.x == 1)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL: Excpected ThreadStatic field of both objects to be 1");
			return 101;
		}

	}
}

public class C1<T>
{
	public static int x;

	public C1()
	{
		x +=1;
		Console.WriteLine(x);
	}
}
