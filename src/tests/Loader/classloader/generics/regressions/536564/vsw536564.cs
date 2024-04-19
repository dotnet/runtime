// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 536564 (and the duplicate VSW 537413)
// We were getting a TypeLoadException when trying to load Class1

using System;
using Xunit;

public interface I1 
{
	void meth1<T>(T x) where T : Class1;	

}

public class Class1 : I1
{
	void I1.meth1<T>(T x)	
	{}
}

public class Test_vsw536564
{
	[Fact]
	public static int TestEntryPoint()
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
