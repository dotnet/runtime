// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 395780
// explicit overriding with a generic type was working.

// as opposed to testExcplicitOverride2.il, in this case the overridden method is also generic.

using System;
using Xunit;

public interface I<T>
{
	int M<N>(T t);
}

public class C : I<String>
{
	int I<String>.M<N>(String t)
	{
		return 3;
	}
}



public class Test_testExplicitOverride
{
	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			I<String> cGen = new C();

			int ret = cGen.M<String>("Hello");

			if (ret == 3)
			{
				Console.WriteLine("PASS");
				return 100;
			}
			else
			{
				Console.WriteLine("FAIL: Incorrect method was invoked. Ret =" + ret);
				return 99;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caugh unexpected exception: " + e);
			return 101;
		}

	}
}
