// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test exercises expression folding in the place of overflowing operations. The original failure was SBCG due to
// an incorrect application of the same: in the program below, the checked int -> ulong cast on line 24 was folded to a
// long -> ulong cast with an incorrect constant value that fit in a ulong, resulting in no overflow exception being
// thrown.

using System;
using Xunit;

public static class C
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i = -4;
        ulong l = 0;

        int rv = 0;
        try
        {
            checked
            {
                l = (ulong)i;
            }
        }
        catch (OverflowException)
        {
            rv = 100;
        }
        catch (Exception)
        {
            i = 0;
            l = 0;
        }

        return rv;
    }
}
