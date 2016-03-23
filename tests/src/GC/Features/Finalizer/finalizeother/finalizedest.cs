// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Tests Finalize() and WaitForPendingFinalizers()

using System;

public class Test
{

    public class Dummy
    {

        public static bool visited;

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

        public CreateObj()
        {
            obj = new Dummy();
        }

        public bool RunTest()
        {
            obj=null;
            GC.Collect();

            GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.

            return Dummy.visited;
        }
    }

    public static int Main()
    {
        CreateObj temp = new CreateObj();

        if (temp.RunTest())
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        Console.WriteLine("Test Failed");
        return 1;

    }
}
