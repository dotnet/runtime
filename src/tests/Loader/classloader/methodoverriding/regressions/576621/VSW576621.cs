// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSWhidbey 576621
// test was asserting.

using System;

public class Test

{
   public static int Main()
   {
  
      	C2 obj2 = new C2();

      	if ( obj2.M3() == 5)
      	{
	      	// correct method was invoked
	      	Console.WriteLine("PASS");
		return 100;
      	}
	else
	{
	      	Console.WriteLine("FAIL");
		return 101;
	}
	
      
   }
}

