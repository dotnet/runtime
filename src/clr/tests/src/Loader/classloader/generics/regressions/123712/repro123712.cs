// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is regression test for VSW 123712.
// Loading the type resulted in TypeLoadException.


using System;

public class Base<T>{}

public class Foo : Base<Bar>{}

public class Bar : Foo{}

public class CMain
{
	public static void Indirect()
  	{
  		Bar b = new Bar();
	}

  	public static int Main()
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
