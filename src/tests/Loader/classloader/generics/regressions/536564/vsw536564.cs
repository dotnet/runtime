// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this is regression test for VSW 536564 (and the duplicate VSW 537413)
// We were getting a TypeLoadException when trying to load Class1

using System;

public interface I1 
{
	void meth1<T>(T x) where T : Class1;	

}

public class Class1 : I1
{
	void I1.meth1<T>(T x)	
	{}
}

public class Test
{
	public static int Main()
	{
		try
		{
			Class1 obj = new Class1();		
			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			return 101;
		}
		
	}
}
