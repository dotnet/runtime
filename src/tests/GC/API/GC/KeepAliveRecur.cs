// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() in Recursive method

using System;

public class Test
{
    public class Dummy
    {
        public static bool visited;
        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            visited = true;
        }
    }

    public static int count;

    public static void foo(Object o)
    {
        if (count == 10) return;
        Console.WriteLine("Count: {0}", count);
        count++;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foo(o);     //Recursive call

        GC.KeepAlive(o);    // Keeping object alive 
    }

    public static int Main()
    {
        Dummy obj = new Dummy();

        foo(obj);
        Console.WriteLine("After call to foo()");

        if (Dummy.visited == false)
        {  // has not visited the Finalize()
            Console.WriteLine("Test for KeepAlive() recursively passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for KeepAlive() recursively failed!");
            return 1;
        }
    }
}
