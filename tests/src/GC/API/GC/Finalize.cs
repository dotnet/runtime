// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Finalize() and WaitForPendingFinalizers()

using System;

public class Test
{
    public static bool visited = false;
    public class Dummy
    {
        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            Test.visited = true;
        }
    }

    public class CreateObj
    {
        public Dummy obj;


        public CreateObj()
        {
            obj = new Dummy();
        }

        public void RunTest()
        {
            obj = null;
            GC.Collect();

            GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();


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
