// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test for double bitwise NOT optimization

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DoubleNotTest
{
    // Test case from the issue: Index.IsFromEnd optimization
    // This pattern generates double NOT across statements/blocks
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(Index i) => i.IsFromEnd ? ~i.Value : i.Value;

    [Fact]
    public static int TestEntryPoint()
    {
        // Test Foo with IsFromEnd
        Index idx1 = new Index(5, fromEnd: true);
        if (Foo(idx1) != ~5)
        {
            Console.WriteLine("FAIL: Foo(^5) returned " + Foo(idx1) + ", expected " + ~5);
            return 101;
        }

        Index idx2 = new Index(5, fromEnd: false);
        if (Foo(idx2) != 5)
        {
            Console.WriteLine("FAIL: Foo(5) returned " + Foo(idx2) + ", expected 5");
            return 101;
        }

        Console.WriteLine("PASS");
        return 100;
    }
}
