// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Finalize() and WaitForPendingFinalizers()

using System;
using System.Runtime.CompilerServices;

public class Test_Finalize
{
    public static bool visited = false;
    public class Dummy
    {
        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            Test_Finalize.visited = true;
        }
    }

    public class CreateObj
    {
        public Dummy obj;


        public CreateObj()
        {
            obj = new Dummy();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void RunTest()
        {
            obj = null;
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();

        GC.Collect();
        GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        GC.Collect();

        if (visited)
        {
            Console.WriteLine("Test for Finalize() & WaitForPendingFinalizers() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for Finalize() & WaitForPendingFinalizers() failed!");
            return 0;
        }
    }
}
