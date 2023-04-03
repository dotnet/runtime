// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Repro case for a bug involving hoisting of static field loads out of
// loops and (illegally) above the corresponding type initializer calls.

using System.Runtime.CompilerServices;
using Xunit;

namespace N
{
    public struct Pair
    {
        public int Left;
        public int Right;

        public static Pair TenFour = new Pair() { Left = 10, Right = 4 };
    }

    public static class C
    {
        static int Sum;
        static int Two;

        // Bug repro requires a use of a Pair value; this is a small fn that takes
        // a Pair by value to serve as that use.  Inline it aggressively so that
        // we won't think the call might kill the static field.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Accumulate(Pair pair)
        {
            Sum += pair.Left + pair.Right;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SumNFourteens(int n)
        {
            for (int i = 0; i < n; ++i)
            {
                Two = 2; // Store to C.Two here is a global side-effect above which we won't hoist the static initializer (since it may throw).
                Accumulate(Pair.TenFour);  // Hoisting the load of Pair.TenFour above the static init call is incorrect.
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Sum = 0;
            SumNFourteens(7);  // Now Sum = 14 * 7 = 98 (and Two = 2)
            return Sum + Two;  // 98 + 2 = 100 on success
        }
    }
}
