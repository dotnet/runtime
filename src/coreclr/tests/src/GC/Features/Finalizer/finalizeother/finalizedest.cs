// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Finalize() and WaitForPendingFinalizers()

using System;
using System.Runtime.CompilerServices;

public class Test
{

    public class Dummy
    {

        public static bool visited=false;

        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            visited=true;
        }
    }

    public class CreateObj
    {
// disabling unused variable warning
#pragma warning disable 0414
        Dummy obj;
#pragma warning restore 0414

        // No inline to ensure no stray refs to the Dummy object
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public CreateObj()
        {
            obj = new Dummy();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RunTest()
        {
            obj=null;
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();

        GC.Collect(); 
        GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        GC.Collect(); 

        if (Dummy.visited)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        Console.WriteLine("Test Failed");
        return 1;

    }
}
