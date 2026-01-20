// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace JIT.opt.ValueNumbering
{
    public class DoubleNot
    {
        // Test case from the issue: Index.IsFromEnd optimization
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Foo(Index i) => i.IsFromEnd ? ~i.Value : i.Value;

        // Test case: explicit double NOT
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Bar(int i) => i < 0 ? ~~i : i;

        // Test case: chained double NOT
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int DoubleNotInt(int value) => ~~value;

        // Test case: nested double NOT
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int QuadNotInt(int value) => ~~(~~value);

        [Fact]
        public static int TestEntryPoint()
        {
            // Test Foo with IsFromEnd
            Index idx1 = new Index(5, fromEnd: true);
            if (Foo(idx1) != 5)
            {
                Console.WriteLine("FAIL: Foo(^5) returned " + Foo(idx1) + ", expected 5");
                return 101;
            }

            Index idx2 = new Index(5, fromEnd: false);
            if (Foo(idx2) != 5)
            {
                Console.WriteLine("FAIL: Foo(5) returned " + Foo(idx2) + ", expected 5");
                return 102;
            }

            // Test Bar
            if (Bar(-10) != -10)
            {
                Console.WriteLine("FAIL: Bar(-10) returned " + Bar(-10) + ", expected -10");
                return 103;
            }

            if (Bar(10) != 10)
            {
                Console.WriteLine("FAIL: Bar(10) returned " + Bar(10) + ", expected 10");
                return 104;
            }

            // Test DoubleNotInt
            if (DoubleNotInt(42) != 42)
            {
                Console.WriteLine("FAIL: DoubleNotInt(42) returned " + DoubleNotInt(42) + ", expected 42");
                return 105;
            }

            if (DoubleNotInt(-42) != -42)
            {
                Console.WriteLine("FAIL: DoubleNotInt(-42) returned " + DoubleNotInt(-42) + ", expected -42");
                return 106;
            }

            // Test QuadNotInt
            if (QuadNotInt(99) != 99)
            {
                Console.WriteLine("FAIL: QuadNotInt(99) returned " + QuadNotInt(99) + ", expected 99");
                return 107;
            }

            Console.WriteLine("PASS: All double NOT optimizations work correctly");
            return 100;
        }
    }
}
