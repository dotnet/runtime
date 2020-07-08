// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Reflection;

namespace Precise
{
    public class measure
    {
        public static int a = 0xCC;
    }
    public class test
    {
        public static byte b = 0xC;
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
            Console.WriteLine("Current thread is {0}", Thread.CurrentThread.Name);
            // Following two lines commented because not available in .NET Core
            // Console.WriteLine("Calling assembly is {0}", Assembly.GetCallingAssembly().FullName);
            // Console.WriteLine("This assembly is {0}", Assembly.GetExecutingAssembly().FullName);
            measure.a += 8;
            if (measure.a != 212)
            {
                Console.WriteLine("in .cctor() after measure.a+=8, measure.a is {0}", measure.a);
                Console.WriteLine("FAILED");
                throw new Exception();
            }
            Console.WriteLine("in .cctor() after measure.a+=8, measure.a is {0}", measure.a);
        }
    }
}

