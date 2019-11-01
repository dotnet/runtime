// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this test is regression test for VSW 188892
// we couldn't load C3

using System;

class C2<T> { }
class C1<T> : C2<C3> { }
class C3 : C1<C3> { }

                                       

class Test
{

	public static void LoadTypes()
	{	
		C2<int> c2 = new C2<int>();
		C2<C3> c1 = new C1<double>();
		C1<C3> c3 = new C3();
	}
	
    	public static int Main()
    	{	
    		try
    		{
    			LoadTypes();
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
