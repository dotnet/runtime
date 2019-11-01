// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class Gen<U>
{
	public void Meth<T>(T t) where T : struct, U {}
}

public class Test
{
	public static int Main()
	{
		try
		{
			Gen<System.ValueType> g1 = new Gen<ValueType>();
	    g1.Meth(1);
	    Console.WriteLine("PASS");
	    return 100;
	  }
	  catch(Exception e)
	  {
	  	Console.WriteLine("cuaght unexpected exception \n {0}", e);
	  	return 99;
	  }
	}
}

