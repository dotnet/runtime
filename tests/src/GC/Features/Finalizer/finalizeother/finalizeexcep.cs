// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Exception handling in Finalize() 

using System;

public class Test {

	public class List {
		public int val;
		public List next;
	}
	public class Dummy {

		public static bool visited;
	
		~Dummy() {
			List lst = new List();
			Console.WriteLine("In Finalize() of Dummy");
			try {
				Console.WriteLine(lst.next.val);    // should throw nullreference exception
			} catch(NullReferenceException) {
                Console.WriteLine("Caught NullReferenceException in Finalize()");				
                visited=true;
			}
			
			
		}
	}

	public class CreateObj {
		public Dummy obj;

		public CreateObj() {
			obj = new Dummy();
		}

		public bool RunTest() {
			obj=null;
			GC.Collect();
		
			GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.

			if(Dummy.visited == true) {
                return true;
			}
			else {
                return false;
			}
		}
	}

	public static int Main() {

		CreateObj temp= new CreateObj();

        if (temp.RunTest())
        {
            Console.WriteLine("Test for Exception handling in Finalize() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for Exception handling in Finalize() failed!");
            return 1;
        }
		
		
	}
}
