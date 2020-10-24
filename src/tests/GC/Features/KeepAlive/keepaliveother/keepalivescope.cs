// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() scopes

using System;

public class Test {

    public static int returnValue = 0;
	public class Dummy {

		public static bool visited;
		~Dummy() {
			//Console.WriteLine("In Finalize() of Dummy");	
			visited=true;
		}
	}

	public class CreateObj {
		public Dummy obj;
		public bool result;

		public CreateObj() {
			obj = new Dummy();
			result=false;
		}
		
		public void RunTest() {
			GC.Collect();
			GC.WaitForPendingFinalizers();
		
			
			if((Dummy.visited == false)) {  // has not visited the Finalize() yet
				result=true;
			}
		
			GC.KeepAlive(obj);	// will keep alive 'obj' till this point
		
			obj=null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
		
			if(result==true && Dummy.visited==true)
                returnValue = 100;
			else
                returnValue = 1;
		}

	}

	public static int Main() {

		CreateObj temp = new CreateObj();
		temp.RunTest();

        if (returnValue == 100) 
			Console.WriteLine("Test passed!");		
		else
			Console.WriteLine("Test failed!");

        return returnValue;
	}
}



