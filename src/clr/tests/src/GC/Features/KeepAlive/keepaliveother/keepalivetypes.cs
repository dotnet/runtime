// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests KeepAlive() with different types of inputs

using System;

public class Test {

	public class Dummy {

		public static bool visited;
		~Dummy() {
			//Console.WriteLine("In Finalize() of Dummy");	
			visited=true;
		}
	}

	public struct StrDummy {
		public int val;
		public static bool flag;

		public StrDummy(int v) {
			val=v;
			flag=true;
		}
	}

	public enum Color
	{
		Red, Blue, Green
	}

	public static int  Main() {

        int returnValue = 0;
		Dummy obj = new Dummy();
		StrDummy strobj = new StrDummy(999);
		Color enumobj = new Color();
	
		GC.Collect();
		GC.WaitForPendingFinalizers();
		
			
		if((Dummy.visited == false) && (StrDummy.flag==true)) {  // has not visited the Finalize()
            returnValue = 100;
			Console.WriteLine("Test passed!");
		}
		else {
            returnValue = 1;
			Console.WriteLine("Test failed!");
		}

		GC.KeepAlive(obj);	// will keep alive 'obj' till this point
		GC.KeepAlive(1000000);
		GC.KeepAlive("long string for testing");
		GC.KeepAlive(-12345678);
		GC.KeepAlive(3456.8989);
		GC.KeepAlive(true);
		GC.KeepAlive(strobj);
		GC.KeepAlive(enumobj);

        return returnValue;
		
	}
}



