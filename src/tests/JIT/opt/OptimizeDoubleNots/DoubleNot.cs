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
        Index idx1 = new Index(5, fromEnd: true);
        Assert.Equal(~5, Foo(idx1));   // integrated failure message from xUnit

        Index idx2 = new Index(5, fromEnd: false);
        Assert.Equal(5, Foo(idx2));

        return 100;
    }
}
