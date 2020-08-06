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

        struct S {
            public short  s16;
            public ushort u16;
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int TestF1(short a, int b)
        {
            S s;
            s.s16 = (short)(a * 2);
            int d = *((ushort*)&s.s16) / b;
            Console.WriteLine(d);
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe short TestF2(short a, int b)
        {
            S s;
            s.s16 = (short)(b * 2);
            *((ushort*)&s.s16) = (ushort)(a * b);
            Console.WriteLine(s.s16);
            return s.s16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int TestF3(ushort a, int b)
        {
            S s;
            s.u16 = (ushort)(a * 2);
            int d = *((short*)&s.u16) / b;
            Console.WriteLine(d);
            return d;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ushort TestF4(ushort a, int b)
        {
            S s;
            s.u16 = (ushort)(b * 2);
            *((short*)&s.u16) = (short)(a * b);
            Console.WriteLine(s.u16);
            return s.u16;
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
            int    result1a = Test1(-1,1);
            Check("Test1a", result1a, 65534);

            int    result1b = Test1(-1,-1);
            Check("Test1b", result1b, -65534);

            short  result2a = Test2(-1,1);
            Check("Test2a", (int) result2a, -1);

            short  result2b = Test2(-1,-1);
            Check("Test2b", (int) result2b, 1);

            int    result3 = Test3(32767,-1);
            Check("Test3", (int) result3, 2);
            
            ushort result4 = Test4(32767,-1);
            Check("Test4", (int) result4, 32769);


            
            int    resultF1a = TestF1(-1,1);
            Check("TestF1a", resultF1a, 65534);

            int    resultF1b = TestF1(-1,-1);
            Check("TestF1b", resultF1b, -65534);

            short  resultF2a = TestF2(-1,1);
            Check("TestF2a", (int) resultF2a, -1);

            short  resultF2b = TestF2(-1,-1);
            Check("TestF2b", (int) resultF2b, 1);

            int    resultF3 = TestF3(32767,-1);
            Check("TestF3", (int) resultF3, 2);
            
            ushort resultF4 = TestF4(32767,-1);
            Check("TestF4", (int) resultF4, 32769);


            
            if (testResult == 100)
            {
                Console.WriteLine("Test Passed");
            }
            return testResult;
        }
    }
}
