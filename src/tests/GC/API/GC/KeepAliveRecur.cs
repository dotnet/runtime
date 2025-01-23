// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests KeepAlive() in Recursive method

using System;
using Xunit;

public class Test_KeepAliveRecur
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

    [Fact]
    public static int TestEntryPoint()
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
