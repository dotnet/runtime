// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests WeakReference.Finalize()  


using System;
using System.Runtime.InteropServices;

public class Test {

    public class Dummy {

        public static bool visited=false;
        ~Dummy() {
            Console.WriteLine("In Finalize() of Dummy");    
            if(visited==false) visited=true;
            else visited=false;
        }
    }

    public class CreateObj {
        Dummy dummy1;
        Dummy dummy2;

        public CreateObj() {
            dummy1 = new Dummy();
            dummy2 = new Dummy();
        }

        public bool RunTest() {
            
            WeakReference weak1 = new WeakReference(dummy1);
            GCHandle handle = GCHandle.Alloc(dummy1,GCHandleType.Normal); // Strong Reference

            WeakReference weak2 = new WeakReference(dummy2); // only a weak reference..so should run finalizer
        
            // ensuring that GC happens even with /debug mode
            dummy1=null;
            dummy2=null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            if(Dummy.visited == true) 
                return true;
            else 
                return false;
        }    
    }

    public static int Main() {
        
        CreateObj temp = new CreateObj();
        bool passed = temp.RunTest();

        if(passed) {
            Console.WriteLine("Test for WeakReference.Finalize() passed!");
            return 100;
        }
        else {
            Console.WriteLine("Test for WeakReference.Finalize() failed!");
            return 1;
        }
    }
}
