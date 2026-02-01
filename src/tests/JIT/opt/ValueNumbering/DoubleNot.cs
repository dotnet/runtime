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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte DoubleNotByte(byte value) => (byte)(~~value);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long DoubleNotLong(long value) => ~~value;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint DoubleNotUInt(uint value) => ~~value;

        // Test case: nested double NOT
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int QuadNotInt(int value) => ~~(~~value);

        // LONGER CHAINS
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TripleNot(int value) => ~~~value;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SixNot(int value) => ~~~~~~value;

        // LOCAL VARIABLE PATTERN 
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int DoubleNotViaLocal(int value)
        {
            int temp = ~value;
            return ~temp;
        }

        // ACROSS BLOCKS
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AcrossBlocks(int value, bool condition)
        {
            int temp = condition ? ~value : ~value;
            return ~temp;
        }

        // WITH OTHER OPERATIONS
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NotWithAddition(int a, int b) => ~~(a + b);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NotInsideExpression(int a, int b) => (~~a) + (~~b);

        [Fact]
        public static int TestEntryPoint()
        {
            // Test Foo with IsFromEnd
            Index idx1 = new Index(5, fromEnd: true);
            if (Foo(idx1) != ~5)
            {
                Console.WriteLine("FAIL: Foo(^5) returned " + Foo(idx1) + ", expected " + ~5);
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

            // Test DoubleNotByte
            if (DoubleNotByte(200) != 200)
            {
                Console.WriteLine("FAIL: DoubleNotByte(200) returned " + DoubleNotByte(200) + ", expected 200");
                return 108;
            }

            // Test DoubleNotLong
            if (DoubleNotLong(123456789L) != 123456789L)
            {
                Console.WriteLine("FAIL: DoubleNotLong(123456789L) returned " + DoubleNotLong(123456789L) + ", expected 123456789L");
                return 109;
            }

            // Test DoubleNotUInt
            if (DoubleNotUInt(4000000000U) != 4000000000U)
            {
                Console.WriteLine("FAIL: DoubleNotUInt(4000000000U) returned " + DoubleNotUInt(4000000000U) + ", expected 4000000000U");
                return 110;
            }

            // Test TripleNot
            if (TripleNot(42) != ~42)
            {
                Console.WriteLine("FAIL: TripleNot(42) returned " + TripleNot(42) + ", expected " + ~42);
                return 111;
            }

            // Test SixNot
            if (SixNot(42) != 42)
            {
                Console.WriteLine("FAIL: SixNot(42) returned " + SixNot(42) + ", expected 42");
                return 112;
            }

            // Test DoubleNotViaLocal
            if (DoubleNotViaLocal(42) != 42)
            {
                Console.WriteLine("FAIL: DoubleNotViaLocal(42) returned " + DoubleNotViaLocal(42) + ", expected 42");
                return 113;
            }

            // Test AcrossBlocks
            if (AcrossBlocks(42, true) != 42)
            {
                Console.WriteLine("FAIL: AcrossBlocks(42, true) returned " + AcrossBlocks(42, true) + ", expected 42");
                return 114;
            }

            if (AcrossBlocks(42, false) != 42)
            {
                Console.WriteLine("FAIL: AcrossBlocks(42, false) returned " + AcrossBlocks(42, false) + ", expected 42");
                return 115;
            }

            // Test NotWithAddition
            if (NotWithAddition(10, 20) != 30)
            {
                Console.WriteLine("FAIL: NotWithAddition(10, 20) returned " + NotWithAddition(10, 20) + ", expected 30");
                return 116;
            }

            // Test NotInsideExpression
            if (NotInsideExpression(10, 20) != 30)
            {
                Console.WriteLine("FAIL: NotInsideExpression(10, 20) returned " + NotInsideExpression(10, 20) + ", expected 30");
                return 117;
            }

            Console.WriteLine("PASS: All double NOT optimizations work correctly");
            return 100;
        }
    }
}
