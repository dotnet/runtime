// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// static method - inlineable

using System;
internal class measure
{
    public static int a = 0xCC;
}
internal class test
{
    public static void f()
    {
    }

    static test()
    {
        if (measure.a != 0xCC)
        {
            Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            throw new Exception();
        }
        Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
        measure.a = 8;
        if (measure.a != 8)
        {
            Console.WriteLine("in .cctor() after measure.a=8, measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            throw new Exception();
        }
        Console.WriteLine("in .cctor() after measure.a=8, measure.a is {0}", measure.a);
    }
}

internal class Driver
{
    public static int Main()
    {
        try
        {
            Console.WriteLine("Testing .cctor() invocation by calling static method");
            Console.WriteLine();
            Console.WriteLine("Before calling static method");
            // .cctor should not run yet
            if (measure.a != 0xCC)
            {
                Console.WriteLine("in Main(), measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                return 1;
            }
            // the next line should trigger .cctor
            test.f();
            Console.WriteLine("After calling static method");
            if (measure.a != 8)
            {
                Console.WriteLine("in Main() after f(ref b), measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                return -1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            return -1;
        }
        Console.WriteLine();
        Console.WriteLine("PASSED");
        return 100;
    }
}
