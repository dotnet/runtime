// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Tests GC.Collect()

using System;

public class Test {
	public static int Main() {

		Object obj1 = new Object();
		int[] array = new int[25];
		
		int gen1 = GC.GetGeneration(array);

		Console.WriteLine("Array is in generation: " + gen1);
		GC.Collect();

		int gen2 = GC.GetGeneration(array);
		Console.WriteLine("Array is in generation: " + gen2);

		if(((gen1==2) && (gen2==2)) || (gen2>gen1)) {	 // was already in gen 2!
			Console.WriteLine("Test for GC.Collect() passed!");
            return 100;
		}

		else {
			Console.WriteLine("Test for GC.Collect() failed!");
            return 1;
		}
	}
}
