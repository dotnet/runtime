// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Passing a very large struct by value on the stack, on arm32 and x86,
// can cause it to be copied from a temp to the outgoing space without
// probing the stack.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace BigFrames_skippage3
{

    [StructLayout(LayoutKind.Explicit)]
    public struct LargeStruct
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(65512)]
        public int i2;
    }

    public class Test
    {
        public static int iret = 1;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(int i1, int i2, int i3, int i4, LargeStruct s)
        {
            Console.Write("Enter TestWrite: ");
            Console.WriteLine(i1 + i2 + i3 + i4 + s.i2);
            iret = 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            Console.WriteLine("Enter Test1");
            LargeStruct s = new LargeStruct();
            s.i2 = 5;
            TestWrite(1, 2, 3, 4, s); // 4 int reg args, then struct stack arg
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Escape(ref LargeStruct s)
        {
        }

        // A lot of time the stack when we are called has a bunch of committed pages
        // before the guard page. So eat up a bunch of stack before doing our test,
        // where we want to be near the guard page.
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void EatStackThenTest1(int level = 0)
        {
            LargeStruct s = new LargeStruct();
            s.i2 = level;
            Escape(ref s);

            if (level < 10)
            {
                EatStackThenTest1(level + 1);
            }
            else
            {
                Test1();
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test1(); // force JIT of this

            EatStackThenTest1(); // If that didn't fail, eat stack then try again.

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
