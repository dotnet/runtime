// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 515341
// we used to get System.Security.VerificationException when invoking Meth2<GenS<C>>()

using System;
using Xunit;

public interface GenI<T> { }
public struct GenS<T> : GenI<T> { }

public class C
{
	public void Meth2<T>() where T : GenI<C> { }
}	 

public class Test_vsw515341
{
	public static void RunTest()
	{
		C c = new C();
		c.Meth2<GenS<C>>();	
	}
	
	[Fact]
	public static int TestEntryPoint()
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

