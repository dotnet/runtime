// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() with GCHandles

using System;
using System.Runtime.InteropServices;

public class Test {
	public class Dummy {

		public static bool visited;
		~Dummy() {
			//Console.WriteLine("In Finalize() of Dummy");
			visited=true;
		}
	}

	public static int Main() {

        int returnValue = 0;
		Dummy obj = new Dummy();

		Console.WriteLine("Allocating a Weak handle to object..");
		GCHandle handle = GCHandle.Alloc(obj,GCHandleType.Weak);

		GC.Collect();
		GC.WaitForPendingFinalizers();
		
		if(Dummy.visited == false) {  // has not visited the Finalize()
            returnValue = 100;
			Console.WriteLine("Test for KeepAlive() passed!");
		}
		else {
            returnValue = 1;
			Console.WriteLine("Test for KeepAlive() failed!");
		}

		GC.KeepAlive(obj);	// will keep alive 'obj' till this point
		GC.Collect();

        return returnValue;
	}
}
