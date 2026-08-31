// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Precise
{
    public class measure
    {
        public static int a = 0xCC;
    }
    public class test
    {
        public static byte b1 = 0xC;
        public byte b2 = 0xC;
        public static void f()
        {
        }
        public static void f(ref byte b)
        {
            return;
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

}

