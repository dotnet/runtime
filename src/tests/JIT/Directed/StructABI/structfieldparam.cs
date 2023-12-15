// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a repro for Issue6585. The problem was that the source of
// a GT_OBJ (struct argument) node was a lclFldAddr, and codegen was
// treating it as a lclVarAddr, i.e. not adding in the offset.
// The inner struct must either be larger than CPBLK_UNROLL_LIMIT,
// which is currently 64 bytes, or must contain GC refs.

using System;
using System.Runtime.CompilerServices;
using Xunit;
namespace structfieldparam
{

    struct Inner1
    {
        public long l1;
        public long l2;
        public long l3;
        public long l4;
        public long l5;
        public long l6;
        public long l7;
        public long l8;
        public long[] arr;

        public Inner1(int seed)
        {
            l1 = seed;
            l2 = seed + 1;
            l3 = seed + 2;
            l4 = seed + 3;
            l5 = seed + 4;
            l6 = seed + 5;
            l7 = seed + 6;
            l8 = seed + 7;
            arr = new long[4];
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public long sum()
        {
            return l1 + l2 + l3 + l4 + l5 + l6 + l7 + l8;
        }
    }

    struct Outer1
    {
        public int i1;
        public long l1;
        public Inner1 inner;
    }

    struct Inner2
    {
        public long l1;
        public long l2;
        public long l3;
        public long l4;
        public long l5;
        public long l6;
        public long l7;
        public long l8;
        public long l9;

        public Inner2(int seed)
        {
            l1 = seed;
            l2 = seed + 1;
            l3 = seed + 2;
            l4 = seed + 3;
            l5 = seed + 4;
            l6 = seed + 5;
            l7 = seed + 6;
            l8 = seed + 7;
            l9 = seed + 8;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public long sum()
        {
            return l1 + l2 + l3 + l4 + l5 + l6 + l7 + l8 + l9;
        }
    }

    struct Outer2
    {
        public int i1;
        public long l1;
        public Inner2 inner;
    }

    public class Program
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test1(Inner1 s)
        {
            return s.sum();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static long test2(Inner2 s)
        {
            return s.sum();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = 100;

            Inner1 t1 = new Inner1(10);
            Outer1 o1;
            o1.i1 = 1;
            o1.l1 = 2;
            o1.inner = t1;
            long direct = t1.sum();
            long indirect = test1(o1.inner);
            if (direct != indirect)
            {
                Console.WriteLine("t1.sum() returns " + direct + ", but test(o1.inner) returns " + indirect);
                result = -1;
            }

            Inner2 t2 = new Inner2(10);
            Outer2 o2;
            o2.i1 = 1;
            o2.l1 = 2;
            o2.inner = t2;
            direct = t2.sum();
            indirect = test2(o2.inner);
            if (direct != indirect)
            {
                Console.WriteLine("t2.sum() returns " + direct + ", but test(o2.inner) returns " + indirect);
                result = -1;
            }

            return result;
        }
    }
}
