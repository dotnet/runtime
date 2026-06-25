// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestIntMoveNoSignExtend
{
    public class Program
    {
        static int s_value = 7;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int GetInt() => s_value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Sink(int x) => s_value ^= x;

        // A reg-to-reg move of a TYP_INT value (here the call result kept in a callee-saved register
        // across the following call) must not be sign extended to 64 bits: the upper bits are never
        // observed for an int, so a plain 'mov Wd, Wn' suffices instead of 'sxtw Wd, Wn'.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int IntMove()
        {
            //ARM64-NOT: sxtw {{w[0-9]+}}, {{w[0-9]+}}
            int a = GetInt();
            Sink(0);
            return a + GetInt();
        }

        // A genuine int -> long widening must still sign extend with 'sxtw Xd, Wn'.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Widen(int x)
        {
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = 100;

            s_value = 7;
            if (IntMove() != 14)
            {
                result = -1;
            }

            // Negative values must round-trip correctly through the move.
            s_value = -123456;
            if (IntMove() != -246912)
            {
                result = -1;
            }

            s_value = int.MinValue;
            // int.MinValue + int.MinValue overflows to 0 in unchecked int arithmetic.
            if (IntMove() != 0)
            {
                result = -1;
            }

            // Genuine widening must preserve the sign.
            if (Widen(-7) != -7L)
            {
                result = -1;
            }
            if (Widen(int.MinValue) != int.MinValue)
            {
                result = -1;
            }

            return result;
        }
    }
}
