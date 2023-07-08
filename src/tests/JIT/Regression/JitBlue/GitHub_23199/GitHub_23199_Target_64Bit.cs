using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// The test revealed some problems of GCStress infrastructure on platforms with multi reg returns (arm64, amd64 Unix).
// It required GCStress=0xc and GcStressOnDirectCalls=1 to hit issues. The issues were with saving GC pointers in the return registers.
// The GC infra has to correctly mark registers with pointers as alive and must not report registers without pointers.

using nint = System.Int64;
using Xunit;

namespace GitHub_23199_64Bit
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestCrossgenedReturnWith2PointersStruct()
        {
            ProcessStartInfo pi = new ProcessStartInfo();
            // pi.Environment calls crossgened HashtableEnumerator::get_Entry returning struct that we need.
            Console.WriteLine(pi.Environment.Count);
            return pi;
        }

        struct TwoPointers
        {
            public Object a;
            public Object b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static TwoPointers GetTwoPointersStruct()
        {
            var a = new TwoPointers();
            a.a = new String("TestTwoPointers");
            a.b = new string("Passed");
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestTwoPointers()
        {
            var a = GetTwoPointersStruct(); // Report both.
            Console.WriteLine(a.a + " " + a.b);
            return a;
        }

        struct OnePointer
        {
            public Object a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static TwoPointers GetOnePointer()
        {
            var a = new TwoPointers();
            a.a = new String("TestOnePointer Passed");
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestOnePointer()
        {
            var a = GetOnePointer();  // Report one.
            Console.WriteLine(a.a);
            return a;
        }

        struct FirstPointer
        {
            public Object a;
            public nint b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static FirstPointer GetFirstPointer()
        {
            var a = new FirstPointer();
            a.a = new String("TestFirstPointer Passed");
            a.b = 100;
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestFirstPointer()
        {
            var a = GetFirstPointer();  // Report the first field, do not report the second.
            Console.WriteLine(a.a);
            return a;
        }

        struct SecondPointer
        {
            public nint a;
            public Object b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static SecondPointer GetSecondPointer()
        {
            var a = new SecondPointer();
            a.a = 100;
            a.b = new String("TestSecondPointer Passed");
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestSecondPointer()
        {
            var a = GetSecondPointer(); // Report the second field, do not report the first.
            Console.WriteLine(a.b);
            return a;
        }

        struct NoPointer1
        {
            public nint a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static NoPointer1 GetNoPointer1()
        {
            var a = new NoPointer1();
            a.a = 100;
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestNoPointer1()
        {
            var a = GetNoPointer1(); // Do not report anything.
            Console.WriteLine("TestNoPointer1 Passed");
            return a;
        }

        struct NoPointer2
        {
            public nint a;
            public nint b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static NoPointer2 GetNoPointer2()
        {
            var a = new NoPointer2();
            a.a = 100;
            a.b = 100;
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestNoPointer2()
        {
            NoPointer2 a = GetNoPointer2();  // Do not report anything.
            Console.WriteLine("TestNoPointer2 Passed");
            return a;
        }

        struct ThirdPointer
        {
            public nint a;
            public nint b;
            public Object c;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ThirdPointer GetThirdPointer()
        {
            var a = new ThirdPointer();
            a.a = 100;
            a.b = 100;
            a.c = new String("TestThirdPointer Passed");
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Object TestThirdPointer()
        {
            ThirdPointer a = GetThirdPointer();  // Do not return in registers.
            Console.WriteLine(a.c);
            return a;
        }


        [Fact]
        public static int TestEntryPoint()
        {
            TestCrossgenedReturnWith2PointersStruct();
            TestTwoPointers();
            TestOnePointer();
            TestFirstPointer();
            TestSecondPointer();
            TestNoPointer1();
            TestNoPointer2();
            TestThirdPointer();
            return 100;
        }
    }
}
