// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.Collect(0)

using System;

public class Test {
	public static int Main() {

		int[] array = new int[25];
		int agen1 = GC.GetGeneration(array);

		Console.WriteLine("Array is in generation: " + agen1);
		
		if(agen1 != 0) {
			Console.WriteLine("Running under stress..");
            return 100;
		}
		
		//GC.Collect();

		Object obj = new Object();
		int ogen1 = GC.GetGeneration(obj);

		Console.WriteLine("Object is in generation: " + ogen1);
		Console.WriteLine("Collect(0)");
		GC.Collect(0);
		GC.Collect(0);

		int agen2 = GC.GetGeneration(array);
		int ogen2 = GC.GetGeneration(obj);
			
		if(agen2 > 1) {
			Console.WriteLine("Running under stress..");
            return 100;
		}

		Console.WriteLine("Array is in generation: {0}",agen2);
		Console.WriteLine("Object is in generation: {0}",ogen2);
		
		if(agen2 == ogen2) {	 // only gen 0 was collected
			Console.WriteLine("Test for GC.Collect(0) passed!");
            return 100;
		}

		else {
			Console.WriteLine("Test for GC.Collect(0) failed!");
            return 1;
		}
	}
}
