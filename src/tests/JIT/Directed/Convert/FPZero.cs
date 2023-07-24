// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * Assertion prop was not taking into account the fact that +0.0 and -0.0 compare equal but are not the same value.
 * Just need to turn of assertion prop for all floating point because equality doesn't mean the same...
 * Notes from initial investigations:
 * Calling IsNegativeZero the first time returns true, the second time false.
 * The first call to IsNegativeZero is getting inlined and the second call is not.
 * It appears the code for the non-inlined method is wrong which is how we end up with two different results for the same call with the same arg.
 * It seems that if you compile with /debug we get correct code so I’m thinking the problem is with inlining DoubleToInt64Bits into IsNegativeZero.
 * 
 */

using System;
using Xunit;

public class MyClass
{
    [Fact]
    public static int TestEntryPoint()
    {
        double d1 = -0e0;
        if (!IsNegativeZero(d1)) return 101;
        double d2 = -0e0;
        if (!IsNegativeZero(d2)) return 101;
        return 100;
    }

    private static unsafe long DoubleToInt64Bits(double value)
    {
        return *((long*)&value);
    }

    private static bool IsNegativeZero(double value)
    {
        if (value == 0 && DoubleToInt64Bits(value) == DoubleToInt64Bits(-0e0))
        {
            return true;
        }
        return false;
    }
}

