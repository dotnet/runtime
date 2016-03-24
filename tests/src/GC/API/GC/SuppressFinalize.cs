// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests SuppressFinalize()

using System;

public class Test {

	public class Dummy {

		public static bool visited;
		~Dummy() {
			Console.WriteLine("In Finalize() of Dummy");	
			visited=true;
		}
	}

	public static int Main() {

		Dummy obj1 = new Dummy();
	
		GC.SuppressFinalize(obj1);	// should not call the Finalizer() for obj1
		obj1=null;
			
		GC.Collect();
		
		GC.WaitForPendingFinalizers();   // call all Finalizers.

		if(Dummy.visited == false) {
			Console.WriteLine("Test for SuppressFinalize() passed!");
            return 100;
		}
		else {
			Console.WriteLine("Test for SuppressFinalize() failed!");
            return 1;
		}
	}
}
