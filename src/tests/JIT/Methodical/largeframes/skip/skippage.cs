// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Consider a case of potentially skipping probing a page on ARM32.
// 1. Have a function with a "very large frame" that requires a probing loop,
//    of exactly a page size increment. The last page is not probed.
// 2. Call a function that doesn't force touching the pages.
//
// The probing must be required, and the space not otherwise probed, e.g.,
// because of the need for an outgoing argument space for a separate function
// that is not called.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace BigFrames_skippage
{

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct12288 // Three pages
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(12284)]
        public int i2;
    }

    public class Test
    {
        public static int iret = 100;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(int i1, int i2, int i3, int i4, Struct12288 s)
        {
            Console.WriteLine(i1 + i2 + i3 + i4 + s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite2()
        {
            Console.WriteLine(7);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1(bool call_struct_function)
        {
            if (call_struct_function)
            {
                Struct12288 s = new Struct12288();
                s.i2 = 5;
                TestWrite(1, 2, 3, 4, s); // 4 int reg args, then struct stack arg
            }
            else
            {
                TestWrite2();
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TestWrite2(); // Make sure this is JITted first, so the call from Test1() is not to the prestub.

            Test1(false);

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
