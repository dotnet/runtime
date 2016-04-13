// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
	This is regression test for DevDiv #607
	Runtime was throwing a TypeLoadException
	Unhandled Exception: System.TypeLoadException: 
	The type 'I6' in assembly 'check2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' 
	has a contracting interface set for some instantiations.
*/
using System;

public class Test
{	
	public static int Main()
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
