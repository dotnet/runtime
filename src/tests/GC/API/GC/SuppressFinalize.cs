// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests SuppressFinalize()

using System;
using System.Runtime.CompilerServices;

public class Test_SuppressFinalize {

	public class Dummy {

		public static bool visited;
		~Dummy() {
			Console.WriteLine("In Finalize() of Dummy");	
			visited=true;
		}
	}

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void RunTest()
    {
		Dummy obj1 = new Dummy();
		GC.SuppressFinalize(obj1);	// should not call the Finalizer() for obj1
		obj1=null;
    }

	public static int Main()
    {
        RunTest();

		GC.Collect();
		GC.WaitForPendingFinalizers();   // call all Finalizers.
		GC.Collect();

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
