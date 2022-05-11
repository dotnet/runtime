// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// static field
using System;
using Xunit;
namespace Precise
{
    public class Driver_xprecise4
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Console.WriteLine("Testing .cctor() invocation by accessing static field across assembly");
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
                test.b1 = 0xF;
                Console.WriteLine("After calling static field");
                if (measure.a != 8)
                {
                    Console.WriteLine("in Main(), measure.a is {0}", measure.a);
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
