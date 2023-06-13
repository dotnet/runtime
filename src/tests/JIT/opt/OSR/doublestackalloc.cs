// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// OSR test case with two stackallocs.
//
// Original method frame is variable sized when we reach patchpoint
// OSR method frame is also variable sized.

using System;
using Xunit;

public class DoubleStackAlloc
{
    static int outerSize = 1000;
    static int innerSize = 1;
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        long* result = stackalloc long[outerSize];
        *result = 0;
        for (int i = 0; i < 1_000_000; i++)
        {
            if ((i % 8192) == 0)
            {
                long *nresult = stackalloc long[innerSize];
                *nresult = *result;
                result = nresult;
            }
            *result += i;
        }
        return *result == 499999500000 ? 100 : -1;
    }  
}
