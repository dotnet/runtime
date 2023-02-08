// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Repro for a failure to hoist an invariant expression due to bad
// interaction between CSE and constant prop.

using System;
using Xunit;

public class Program
{
    public static int ii;
    [Fact]
    public static int TestEntryPoint()
    {
        int res = 0;
        ii = 99;
        int b = ii;
        int a = ii - 1;

        for (int i = 0; i < b; ++i)
        {
            int res1 = (b + a + b * a + a * a + b * b); // large invariant expression with constant liberal VN but non-const conservative VN
            res += res1;
        }

        // At this point, res should be 2901096
        // Since the test needs to return 100 on success,
        // subtract 2900996 from res.
        return res - 2900996;
    }
}
