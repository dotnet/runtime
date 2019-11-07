// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// static method
using System;
namespace Precise
{
    class Driver
    {
        public static int Main()
        {
            try
            {
                Console.WriteLine("Testing .cctor() invocation by calling static method across assembly");
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
}
