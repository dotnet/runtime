// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Sleep in Finalizer for array of objects 

using System;
using System.Threading;

public class Test {

	public class Dummy {
		public static int count=0;
		~Dummy() {
			count++;
			Thread.Sleep(1000);
		}
	}

	public class CreateObj {
		public Dummy[] obj;
                public int ExitCode = 0;		

		public CreateObj() {
		obj = new Dummy[10];

		for(int i=0;i<10;i++) {
			obj[i] = new Dummy();
		}
		}
	
		public void RunTest() {		

		obj=null;     // making sure collect is called even with /debug
		GC.Collect();
		GC.WaitForPendingFinalizers();
		
		if(Dummy.count == 10) {     // all objects in array finalized!
			ExitCode = 100;
			//Console.WriteLine("Test for Finalize() for array of objects passed!");
		}
		else {
			ExitCode = 1;
			//Console.WriteLine("Test for Finalize() for array of objects failed!");
		}
		}
	}

	public static int Main() {
		CreateObj temp = new CreateObj();
		temp.RunTest();

		if(temp.ExitCode==100)
			Console.WriteLine("Test for Finalize() for array of objects passed!");
		else 
			Console.WriteLine("Test for Finalize() for array of objects failed!");
                return temp.ExitCode;				
	}
}
