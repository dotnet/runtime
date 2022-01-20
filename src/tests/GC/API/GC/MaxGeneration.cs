// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GC.MaxGeneration

using System;

public class Test_MaxGeneration {
	public static int Main() {
				
		for(int i=0;i<1000;i++) {
		Object[] array = new Object[i];
		}
	   	
		Console.WriteLine("Max Generations: " + GC.MaxGeneration);
		if(GC.MaxGeneration == 2) {
			Console.WriteLine("Test for GC.MaxGeneration passed!");
            return 100;
		}
		else {
			Console.WriteLine("Test for GC.MaxGeneration failed!");
            return 1;
		}
		
		}

	}

