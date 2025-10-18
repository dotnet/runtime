// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace TestCompareZero
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCompareZero()
        {
            bool fail = false;

            if (!CompareLtZeroLong(-4))
            {
                fail = true;
            }

            if (!CompareLeZeroLong(0))
            {
                fail = true;
            }

            if (!CompareGtZeroLong(2))
            {
                fail = true;
            }

            if (!CompareGeZeroLong(0))
            {
                fail = true;
            }

            if (!CompareLtZeroInt(-4))
            {
                fail = true;
            }

            if (!CompareLeZeroInt(0))
            {
                fail = true;
            }

            if (!CompareGtZeroInt(2))
            {
                fail = true;
            }

            if (!CompareGeZeroInt(0))
            {
                fail = true;
            }

            if (!CompareLtZeroShort(-4))
            {
                fail = true;
            }

            if (!CompareLeZeroShort(-1))
            {
                fail = true;
            }

            if (!CompareGtZeroShort(2))
            {
                fail = true;
            }

            if (!CompareGeZeroShort(10))
            {
                fail = true;
            }

            if (!CompareLtZeroSByte(-4))
            {
                fail = true;
            }

            if (!CompareLeZeroSByte(-1))
            {
                fail = true;
            }

            if (!CompareGtZeroSByte(2))
            {
                fail = true;
            }

            if (!CompareGeZeroSByte(5))
            {
                fail = true;
            }

            CompareLtZeroJumpLong(-4);
            CompareLeZeroJumpLong(0);
            CompareGtZeroJumpLong(3);
            CompareGeZeroJumpLong(10);

            CompareLtZeroJumpInt(-4);
            CompareLeZeroJumpInt(-4);
            CompareGtZeroJumpInt(1);
            CompareGeZeroJumpInt(1);

            CompareLtZeroJumpShort(-1);
            CompareLeZeroJumpShort(-2);
            CompareGtZeroJumpShort(2);
            CompareGeZeroJumpShort(12);

            CompareLtZeroJumpSByte(-1);
            CompareLeZeroJumpSByte(-1);
            CompareGtZeroJumpSByte(3);
            CompareGeZeroJumpSByte(0);

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLtZeroLong(long a)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #63
            return a < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLeZeroLong(long a)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGtZeroLong(long a)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return a > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGeZeroLong(long a)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return a >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLtZeroInt(int a)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return a < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLeZeroInt(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGtZeroInt(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return a > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGeZeroInt(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return a >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLtZeroShort(short a)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return a < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLeZeroShort(short a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGtZeroShort(short a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return a > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGeZeroShort(short a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return a >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLtZeroSByte(sbyte a)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return a < 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareLeZeroSByte(sbyte a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, le
            return a <= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGtZeroSByte(sbyte a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, gt
            return a > 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool CompareGeZeroSByte(sbyte a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: cset {{x[0-9]+}}, ge
            return a >= 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLtZeroJumpLong(long a)
        {
            //ARM64-FULL-LINE: tbz {{x[0-9]+}}, #63, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a < 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLeZeroJumpLong(long a)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            //ARM64-FULL-LINE: bgt G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a <= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGtZeroJumpLong(long a)
        {
            //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #0
            //ARM64-FULL-LINE: ble G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a > 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGeZeroJumpLong(long a)
        {
            //ARM64-FULL-LINE: tbnz {{x[0-9]+}}, #63, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a >= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLtZeroJumpInt(int a)
        {
            //ARM64-FULL-LINE: tbz {{w[0-9]+}}, #31, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a < 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLeZeroJumpInt(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: bgt G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a <= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGtZeroJumpInt(int a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: ble G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a > 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGeZeroJumpInt(int a)
        {
            //ARM64-FULL-LINE: tbnz {{w[0-9]+}}, #31, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a >= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLtZeroJumpShort(short a)
        {
            //ARM64-FULL-LINE: tbz {{w[0-9]+}}, #15, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a < 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLeZeroJumpShort(short a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: bgt G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a <= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGtZeroJumpShort(short a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: ble G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a > 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGeZeroJumpShort(short a)
        {
            //ARM64-FULL-LINE: tbnz {{w[0-9]+}}, #15, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a >= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLtZeroJumpSByte(sbyte a)
        {
            //ARM64-FULL-LINE: tbz {{w[0-9]+}}, #7, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a < 0)
                foo();
        }

                [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareLeZeroJumpSByte(sbyte a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: bgt G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a <= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGtZeroJumpSByte(sbyte a)
        {
            //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #0
            //ARM64-FULL-LINE: ble G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a > 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareGeZeroJumpSByte(sbyte a)
        {
            //ARM64-FULL-LINE: tbnz {{w[0-9]+}}, #7, G_M{{[0-9]+}}_IG{{[0-9]+}}
            if (a >= 0)
                foo();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void foo() {}
    }
}
