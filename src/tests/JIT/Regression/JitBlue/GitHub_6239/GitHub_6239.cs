// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test tests our signed uncontained compare logic
// We should generate a signed jump for the high compare, and an unsigned
// jump for the low compare.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    uint i;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(long a, long b)
    {
        if (a < b)
        {
            return 5;
        }
        else
        {
            return 0;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (Test(-2147483649L, -2147483648L) == 5)
        {
            Console.WriteLine("Passed");
            return Pass;
        }
        else
        {
            Console.WriteLine("Failed");
            return Fail;
        }
    }
}
