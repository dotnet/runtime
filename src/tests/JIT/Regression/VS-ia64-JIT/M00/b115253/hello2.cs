// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace hello
{
    public class Class1
    {
        [Fact]
        static public int TestEntryPoint()
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

