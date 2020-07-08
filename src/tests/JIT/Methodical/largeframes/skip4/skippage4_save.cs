// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Passing a very large struct by value on the stack, on arm32 and x86,
// can cause it to be copied from a temp to the outgoing space without
// probing the stack.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace BigFrames
{

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct65500ref
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(65496)]
        public Object o1;
    }

    public class Test
    {
        public static int iret = 1;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(int i1, int i2, int i3, int i4, Struct65500ref s)
        {
            Console.Write("Enter TestWrite: ");
            Console.WriteLine(i1 + i2 + i3 + i4 + s.o1.GetHashCode());
            iret = 100;
            // Test1(); // recurse
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            Console.WriteLine("Enter Test1");
            Struct65500ref s = new Struct65500ref();
            s.o1 = new Object();
            TestWrite(1, 2, 3, 4, s); // 4 int reg args, then struct stack arg
        }

        public static int Main()
        {
            Test1();

            if (iret == 100)
            {
                Console.WriteLine("TEST PASSED");
            }
            else
            {
                Console.WriteLine("TEST FAILED");
            }
            return iret;
        }
    }
}
