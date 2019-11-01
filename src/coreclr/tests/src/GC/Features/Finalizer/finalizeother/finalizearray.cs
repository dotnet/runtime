// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Finalize() on array of objects

using System;
using System.Runtime.CompilerServices;

public class Test 
{
    public class Dummy 
    {
        public static int count=0;
        ~Dummy() 
        {
            count++;
        }
    }

    public class CreateObj 
    {
        public Dummy[] obj;

        // No inline to ensure no stray refs to the new array
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public CreateObj() {
            obj = new Dummy[10000];
            for(int i=0;i<10000;i++) {
                obj[i] = new Dummy();
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void RunTest() 
        {
            obj=null;     // making sure collect is called even with /debug
        }
    }

    public static int Main() {
        CreateObj temp = new CreateObj();
        temp.RunTest();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (Dummy.count == 10000)  // all objects in array finalized!
        {
            Console.WriteLine("Test for Finalize() for array of objects passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for Finalize() for array of objects failed!");
            return 1;
        }

    }
}
