// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test probing of large stack frames.
//
// The JIT generates probes of large frames to ensure that each stack page is touched in sequence, and
// none are skipped. This allows the OS to incrementally commit stack pages.
//
// The JIT generates different code sequences for different cases, typically:
//    <2 pages
//    2-3 pages
//    >= 3 pages
//
// Big frame sizes are accomplished with local structs that have very large explicit layout field offset.
//
// Note that OS page sizes are typically 0x1000 (4096) bytes, but could be bigger.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace BigFrames_bigframes
{

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct4096 // One page
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(4092)]
        public int i2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct5376 // 1.5 pages
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(5372)]
        public int i2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct8192 // Two pages
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(8188)]
        public int i2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct12288 // Three pages
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(12284)]
        public int i2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Struct16384 // Four pages
    {
        [FieldOffset(0)]
        public int i1;
        [FieldOffset(16380)]
        public int i2;
    }

    public class Test
    {
        public static int iret = 100;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(ref Struct4096 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(ref Struct5376 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(ref Struct8192 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(ref Struct12288 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void TestWrite(ref Struct16384 s)
        {
            Console.WriteLine(s.i2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            Struct4096 s = new Struct4096();
            s.i2 = 1;
            TestWrite(ref s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            Struct5376 s = new Struct5376();
            s.i2 = 2;
            TestWrite(ref s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test3()
        {
            Struct8192 s = new Struct8192();
            s.i2 = 3;
            TestWrite(ref s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test4()
        {
            Struct12288 s = new Struct12288();
            s.i2 = 4;
            TestWrite(ref s);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test5()
        {
            Struct16384 s = new Struct16384();
            s.i2 = 5;
            TestWrite(ref s);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Test1();
            Test2();
            Test3();
            Test4();
            Test5();

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
