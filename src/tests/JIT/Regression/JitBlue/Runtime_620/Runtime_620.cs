// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace NormalizeTest
{
    class Program
    {
        static int testResult = 100;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int Test1(short a, int b)
        {
            short c = (short)(a * 2);
            int d = *((ushort*)&c) / b;
            Console.WriteLine(d);
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe short Test2(short a, int b)
        {
            short c = (short)(b * 2);
            *((ushort*)&c) = (ushort)(a * b);
            Console.WriteLine(c);
            return c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int Test3(ushort a, int b)
        {
            ushort c = (ushort)(a * 2);
            int d = *((short*)&c) / b;
            Console.WriteLine(d);
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ushort Test4(ushort a, int b)
        {
            ushort c = (ushort)(b * 2);
            *((short*)&c) = (short)(a * b);
            Console.WriteLine(c);
            return c;
        }

        static void Check(String id, int result, int expected)
        {
            if (result != expected)
            {
                Console.WriteLine("FAILED: {0} -- result {1}, expected {2}", id, result, expected);
                testResult = -1;
            }
        }

        static int Main()
        {
            int    result1 = Test1(-1,1);
            Check("Test1", result1, 65534);

            short  result2 = Test2(-1,1);
            Check("Test2", (int) result2, -1);

            int    result3 = Test3(32767,-1);
            Check("Test3", (int) result3, 2);
            
            ushort result4 = Test4(32767,-1);
            Check("Test4", (int) result4, 32769);
            
            if (testResult == 100)
            {
                Console.WriteLine("Test Passed");
            }
            return testResult;
        }
    }
}
