// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// static field
using System;
namespace Precise
{
    class Driver
    {
        public static int Main()
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
