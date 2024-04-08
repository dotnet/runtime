// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is regression test for VSW 123712.
// Loading the type resulted in TypeLoadException.


using System;
using Xunit;

public class Base<T>{}

public class Foo : Base<Bar>{}

public class Bar : Foo{}

public class CMain
{
	public static void Indirect()
  	{
  		Bar b = new Bar();
	}

  	[Fact]
  	public static int TestEntryPoint()
	{
		try
		{
			Indirect();
			Console.WriteLine("PASS");
			return 100;
    		}
		catch(Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			return 101;
    		}
  	}
}
