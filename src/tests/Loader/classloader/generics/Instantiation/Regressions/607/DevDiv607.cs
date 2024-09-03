// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
	This is regression test for DevDiv #607
	Runtime was throwing a TypeLoadException
	Unhandled Exception: System.TypeLoadException: 
	The type 'I6' in assembly 'check2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' 
	has a contracting interface set for some instantiations.
*/
using System;
using Xunit;

public class Test_DevDiv607
{	
	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			C7 obj = new C7();
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
