// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace N
{
    public static class C
    {
        // This is a regression test for a failure in loop unrolling when
        // the unrolled loop contains a switch statement.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test()
        {
            int s = 0;

            // Loop to some Vector<T>.Count to trigger unrolling.
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                // Loop contains switch; the bug was that the clones
                // of the switch were all sharing its BBswtDesc instead
                // of getting their own, so updates to their jump targets
                // were incorrectly shared.
                switch (i)
                {
                    case 1: s += 4; break;
                    case 2: s += 2; break;
                    case 3: s += i; break;
                }
            }

            return s;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = Test();

            // Expected result is a function of Vector<int>.Count.
            int expected;
            switch(Vector<int>.Count)
            {
                case 1:
                    expected = 4;
                    break;
                case 2:
                    expected = 6;
                    break;
                default:
                    expected = 9;
                    break;
            }

            // Return 100 on success (result == expected), other
            // values on failure.
            return 100 + result - expected;
        }
   }
}
