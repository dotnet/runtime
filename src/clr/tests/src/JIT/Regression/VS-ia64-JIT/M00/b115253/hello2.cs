// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace hello
{
    class Class1
    {
        static public int Main(string[] args)
        {
            int i = 123;
            Console.WriteLine(i);
        begin:
            String s = "test";
        intry:
            try
            {
                Console.WriteLine(s);
                throw new Exception();
            }
            catch
            {
                try
                {
                    if (i == 3) goto incatch;

                }
                catch
                {
                    Console.WriteLine("end inner catch");
                }
                Console.WriteLine("unreached");

            incatch:
                Console.WriteLine("end outer catch " + s);
            }

            return 100;
        }
    }
}

