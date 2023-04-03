// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Block inlining of small localloc callee if call site is in a loop.

using System;
using Xunit;

public class Runtime_43391
{
    [Fact]
    public static int TestEntryPoint()
    {
        int r = 58;
        for (int i = 1; i >= 0; i--)
        {
            r += Test(i);
        }
        return r;
    }
 
    public static unsafe byte Test(int i)
    {
        byte* p = stackalloc byte[8];
        p[i] = 42;
        return p[1];
    }
}
