// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Noway assert where an inlinee modified a parameter with index > 16,
// and caller that had few args or locals passed in a constant for
// that parameter.

public class B
{
    int X(
        int a01, int a02, int a03, int a04,
        int a05, int a06, int a07, int a08,
        int a09, int a10, int a11, int a12,
        int a13, int a14, int a15, int a16,
        int a17, int a18, int a19, int a20)
    {
        a20 = a19;
        return a20;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        B b = new B();
        int v = b.X(1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20);
        return v + 81;
    }
}
