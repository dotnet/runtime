// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this is regression test for VSW 515341
// we used to get System.Security.VerificationException when invoking Meth2<GenS<C>>()

using System;

public interface GenI<T> { }
public struct GenS<T> : GenI<T> { }

public class C
{
	public void Meth2<T>() where T : GenI<C> { }
}	 

public class Test
{
	public static void RunTest()
	{
		C c = new C();
		c.Meth2<GenS<C>>();	
	}
	
	public static int Main()
	{
		try
		{
			RunTest();
			
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

