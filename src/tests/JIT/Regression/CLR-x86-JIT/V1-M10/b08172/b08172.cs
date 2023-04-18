// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Factorial

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    [Fact]
    public static void TestEntryPoint() {
        Test app = new Test();
        app.Run(17);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Run(long i)
    {
        Console.Out.WriteLine("Factorial of " + i.ToString() + " is " + Fact(i).ToString());
        return (0);
    }

    private long Fact(long i)
    {
        if (i <= 1L)
            return (i);
        return (i * Fact(i - 1L));
    }
}
