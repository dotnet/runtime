// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal class measure
{
    public static int a = 0xCC;
}
internal class test
{
    static test()
    {
        if (measure.a != 0xCC)
        {
            Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            return;
        }
        Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
        measure.a = 8;
        if (measure.a != 8)
        {
            Console.WriteLine("in .cctor() after measure.a=8, measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            return;
        }
        Console.WriteLine("in .cctor() after measure.a=8, measure.a is {0}", measure.a);

        throw new Exception();
    }
}

internal class Driver
{
    public static int Main()
    {
        try
        {
            Console.WriteLine("Testing .cctor() invocation by calling instance method");
            Console.WriteLine();
            Console.WriteLine("Before calling instance method");
            if (measure.a != 0xCC)
            {
                Console.WriteLine("in Main(), measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                return 1;
            }
            test t = new test();
            Console.WriteLine("After calling instance method");
            if (measure.a != 8)
            {
                Console.WriteLine("in Main() after new test(), measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                return -1;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
            Console.WriteLine();
            Console.WriteLine("PASSED");
            return 100;
        }
        return -1;
    }
}
