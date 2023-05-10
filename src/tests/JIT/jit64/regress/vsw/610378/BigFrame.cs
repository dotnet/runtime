// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Bug involves register use with the following circumstances:
//  stack frame over 1 page (4K)
//  non-optimized jit code (compile with no optimizations or run with managed debugger attached
//  static method with first or second arg as float, or instance method with first arg as float
//Test code has methods that have just over and just under 1 page frame size.  Repro hits with 
//larger frame methods (LargeFrameSize(float,float)).
//
//Big frame sizes are accomplished with local structs that have very large explicit layout field offset.
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace BigFrame
{
    [StructLayout(LayoutKind.Explicit)]
    public struct BigStruct
    {
        [FieldOffset(0)]
        public float f1;
        [FieldOffset(4000)]
        public float fx; //Always fails in method LargeFrameSize
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct BigStructOK
    {
        [FieldOffset(0)]
        public float f1;
        [FieldOffset(3972)]
        public float fx; //largest offset that won't fail in method SmallFrameSize
    }
    public class TestClass
    {
        public int LargeFrameSize(float farg1, float farg2)
        {
            int iret = 100;
            BigStruct bs;
            bs.f1 = farg1;
            bs.fx = farg2;
            Console.WriteLine("LARGE FRAME SIZE - INSTANCE");
            Console.WriteLine("   farg1={0} farg2={1} expecting 1.1 and 2.2", farg1, farg2);
            Console.WriteLine("   bs.f1={0} bs.fx={1} expecting 1.1 and 2.2", bs.f1, bs.fx);
            if (farg1 != 1.1F || farg2 != 2.2F)
                iret = 666;

            return iret;
        }
        public int SmallFrameSize(float farg1, float farg2)
        {
            int iret = 100;
            BigStructOK bs;
            bs.f1 = farg1;
            bs.fx = farg2;

            Console.WriteLine("SMALL FRAME SIZE - INSTANCE");
            Console.WriteLine("   farg1={0} farg2={1} expecting 1.1 and 2.2", farg1, farg2);
            Console.WriteLine("   bs.f1={0} bs.fx={1} expecting 1.1 and 2.2", bs.f1, bs.fx);
            if (farg1 != 1.1F || farg2 != 2.2F)
                iret = 666;
            return iret;
        }
    }
    public class BigFrame
    {
        public static int LargeFrameSize(float farg1, float farg2)
        {
            int iret = 100;
            BigStruct bs;
            bs.f1 = farg1;
            bs.fx = farg2;
            Console.WriteLine("LARGE FRAME SIZE - STATIC");
            Console.WriteLine("   farg1={0} farg2={1} expecting 1.1 and 2.2", farg1, farg2);
            Console.WriteLine("   bs.f1={0} bs.fx={1} expecting 1.1 and 2.2", bs.f1, bs.fx);
            if (farg1 != 1.1F || farg2 != 2.2F)
                iret = 666;

            return iret;
        }
        public static int SmallFrameSize(float farg1, float farg2)
        {
            int iret = 100;
            BigStructOK bs;
            bs.f1 = farg1;
            bs.fx = farg2;

            Console.WriteLine("SMALL FRAME SIZE - STATIC");
            Console.WriteLine("   farg1={0} farg2={1} expecting 1.1 and 2.2", farg1, farg2);
            Console.WriteLine("   bs.f1={0} bs.fx={1} expecting 1.1 and 2.2", bs.f1, bs.fx);
            if (farg1 != 1.1F || farg2 != 2.2F)
                iret = 666;
            return iret;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            int iret = 100;
            float f1 = 1.1F;
            float f2 = 2.2F;
            TestClass testclass = new TestClass();
            if (SmallFrameSize(f1, f2) != 100)
            {
                Console.WriteLine("FAILED:  static SmallFrameSize");
                iret = 666;
            }
            if (LargeFrameSize(f1, f2) != 100)
            {
                Console.WriteLine("FAILED:  static LargeFrameSize");
                iret = 666;
            }

            if (testclass.SmallFrameSize(f1, f2) != 100)
            {
                Console.WriteLine("FAILED:  instance SmallFrameSize");
                iret = 666;
            }
            if (testclass.LargeFrameSize(f1, f2) != 100)
            {
                Console.WriteLine("FAILED:  instance LargeFrameSize");
                iret = 666;
            }
            if (iret == 100)
            {
                Console.WriteLine("TEST PASSED!!!");
            }
            else
            {
                Console.WriteLine("TEST FAILED!!!");
            }
            return iret;
        }
    }
}
