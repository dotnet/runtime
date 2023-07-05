// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Test_skippage7
{
// Exercise stack probing after localloc, on architectures with fixed outgoing argument
// space. Some implementations did not probe after re-establishing the outgoing argument
// space after a localloc.
//
// We need to create a large enough outgoing argument space to skip a guard page. To actually
// see a problem on Windows, we need to skip 3 guard pages. Since structs are passed by
// reference on arm64/x64, we have to have a huge number of small arguments: over 1536
// "long" arguments. For arm32, structs are passed by value, so we can just pass a very large
// struct. This test case is for the arm32 case.

namespace BigFrames
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
        public static void BigArgSpace(LargeStruct s)
        {
            long result = s.i1 + s.i2;
            Console.Write("BigArgSpace: ");
            Console.WriteLine(result);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void SmallArgSpace(long i1, long i2, long i3, long i4, long i5, long i6, long i7, long i8, long i9, long i10)
        {
            long result = i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8 + i9 + i10;
            Console.Write("SmallArgSpace: ");
            Console.WriteLine(result);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public unsafe static void Test1(int n)
        {
            Console.WriteLine("Enter Test1");
            LargeStruct s = new LargeStruct();
            s.i1 = 7;
            s.i2 = 9;
            BigArgSpace(s);

            // Localloc some space; this moves the outgoing argument space.

            if (n < 1) n = 1;
            int* a = stackalloc int[n * 4096];
            a[0] = 7;
            int i;

            for (i=1; i < 5; ++i)
            {
                a[i] = i + a[i - 1];
            }

            // Now call a function that touches the potentially un-probed
            // outgoing argument space.

            SmallArgSpace(1, 2, 3, 4, 5, 6, 7, 8, 9, a[4]);

            iret = 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Escape(ref LargeStruct s)
        {
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test1(1); // force JIT of this
            Test1(80);

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
}
