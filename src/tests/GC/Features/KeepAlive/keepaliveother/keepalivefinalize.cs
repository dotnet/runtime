// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() in Finalize

using System;
using System.Collections;

public class Test {
	
	public class Dummy1 {
		public static bool visited;
		~Dummy1() {
			Console.WriteLine("In Finalize() of Dummy1");
			Dummy2 temp = new Dummy2();
			visited=true;
			
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.KeepAlive(temp);
		}
	}

	public class Dummy2 {
		public static bool visited;
		~Dummy2() {
			Console.WriteLine("In Finalize() of Dummy2");
			visited=true;
		}
	}

	public static int Main() {

        int returnValue = 0;
		Dummy1 obj = new Dummy1();
		
		GC.Collect();
		GC.WaitForPendingFinalizers();

		if((Dummy1.visited == false) && (Dummy2.visited == false)) {  // has not visited the Finalize()
            returnValue = 100;
			Console.WriteLine("Test passed!");
		}
		else {
            returnValue = 1;
			Console.WriteLine("Test failed!");
		}
		
		GC.KeepAlive(obj);

        return returnValue;
	}
}
