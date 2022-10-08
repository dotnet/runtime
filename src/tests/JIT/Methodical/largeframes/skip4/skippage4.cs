// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Passing a very large struct by value on the stack, on arm32 and x86,
// can cause it to be copied from a temp to the outgoing space without
// probing the stack.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace BigFrames_skippage4
{

    [StructLayout(LayoutKind.Explicit)]
    public struct LargeStructWithRef
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(65496)] // Must be 8-byte aligned for test to work on 64-bit platforms.
        public Object o1;
    }

    public class Test
    {
        public static int iret = 1;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(LargeStructWithRef s)
        {
            Console.Write("Enter TestWrite: ");
            Console.WriteLine(s.o1.GetHashCode());
            iret = 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            Console.WriteLine("Enter Test1");
            LargeStructWithRef s = new LargeStructWithRef();
            s.o1 = new Object();
            TestWrite(s);
        }

        [Fact]
        public static int TestEntryPoint()
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
