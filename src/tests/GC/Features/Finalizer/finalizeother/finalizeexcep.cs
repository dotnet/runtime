// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Exception handling in Finalize() 

using System;
using System.Runtime.CompilerServices;

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

        // No inline to ensure no stray refs to the Dummy object
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public CreateObj() {
            obj = new Dummy();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] 
        public void RunTest() {
            obj=null;
        }
    }

    public static int Main() {

        CreateObj temp= new CreateObj();
        temp.RunTest();

        GC.Collect();
        GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        GC.Collect();

        if (Dummy.visited)
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
