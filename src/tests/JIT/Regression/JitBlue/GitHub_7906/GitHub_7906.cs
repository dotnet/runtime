// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for insufficient guard on inference of initial values
// of induction variables.
using System.Numerics;
using Xunit;

namespace N
{
    public static class C
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int x = 0;

            // When bottom-testing sees this loop, it (questionably for performance,
            // but correctly) copies only the first part of the disjunctive loop condition
            // so we get
            //
            // B1: i = Count                // initialization
            // B2: if (i < Count) goto B6   // duplicated loop condition (note the "zero trip" case goes to the 2nd loop condition disjunct)
            // B3: x += i                   // loop body
            // B4: ++i                      // increment
            // B5: if (i < Count) goto B3   // first disjunct of loop condition
            // B6: if (i < 20) goto B3      // second disjunct of loop condition
            // B7: return x - 84            // post-loop
            //
            // At which point B3..B6 is an irreducible loop, but B3..B5 is a natural loop.
            // This is a regression test for a bug where optRecordLoop would incorrectly
            // identify B1 as the initial value of loop B3..B5 -- this is incorrect because
            // the edge from B6 to B3 enters the loop with different values of i.
            //
            // The testcase is intentionally structured so that loop unrolling will try
            // to unroll loop B3..B5 and generate incorrect code due to the incorrect
            // initial value.
            for (int i = Vector<int>.Count; i < Vector<int>.Count || i < 20; ++i)
            {
                x += i;
            }

            // After running the loop above, the value of x should be (Count + 19) * (20 - Count) / 2.
            // Return 100 + x - (expected x) so the test will return 100 on success.
            return 100 + x - ((Vector<int>.Count + 19) * (20 - Vector<int>.Count) / 2);
        }
    }
}
