// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests GCHandleType.Normal .. the object with GCHandleType Normal 
// should not be collected.

using System;
using System.Runtime.InteropServices;

public class Test {

	public class Dummy {

		public static int flag=0;
		~Dummy() {
			Console.WriteLine("In Finalize() of Dummy");	
			flag=99;
		}
	}

	public static int Main() {

		Dummy obj = new Dummy();
		
		Console.WriteLine("Allocating a normal handle to object..");
		GCHandle handle = GCHandle.Alloc(obj,GCHandleType.Normal); // Normal handle
		
		// ensuring that GC happens even with /debug mode
		obj=null;

		GC.Collect();
		GC.WaitForPendingFinalizers();
		
		if(Dummy.flag == 0) {
			
			Console.WriteLine("Test for GCHandleType.Normal passed!");
            return 100;
		}
		else {
			
			Console.WriteLine("Test for GCHandleType.Normal failed!");
            return 1;
		}


	}
}
