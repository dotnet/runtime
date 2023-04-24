// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// static field

using System;
using Xunit;
namespace Test_precise4_simple_cs
{
internal class measure
{
    public static int a = 0xCC;
}
internal class test
{
    public static byte b = 0xC;

    static test()
    {
        if (measure.a != 0xCC)
        {
            Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            throw new Exception();
        }
        Console.WriteLine("in .cctor(), measure.a is {0}", measure.a);
        measure.a += b;
        if (measure.a != 216)
        {
            Console.WriteLine("in .cctor() after measure.a+=b, measure.a is {0}", measure.a);
            Console.WriteLine("FAILED");
            throw new Exception();
        }
        Console.WriteLine("in .cctor() after measure.a=8, measure.a is {0}", measure.a);
    }
}

public class Driver
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing .cctor() invocation by accessing static field");
            Console.WriteLine();
            Console.WriteLine("Before calling static field");
            // .cctor should not run yet
            if (measure.a != 0xCC)
            {
                Console.WriteLine("in Main(), measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                return 1;
            }
            // the next line should trigger .cctor
            test.b = 0xF;
            Console.WriteLine("After calling static field");
            if (measure.a != 216)
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
}
