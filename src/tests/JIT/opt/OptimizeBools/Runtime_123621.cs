// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When a constant-folded operand appears after a non-constant operand in a
// short-circuit && expression, inlining may leave dead local stores in the
// return block. fgFoldCondToReturnBlock failed to optimize this pattern
// because isReturnBool required hasSingleStmt(), which was false due to the
// leftover dead stores.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_123621
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Hoisted(byte v)
    {
        bool isUnix = Environment.NewLine != "\r\n";
        return (v == 2 && isUnix);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Inline_Before(byte v)
    {
        return (Environment.NewLine != "\r\n" && v == 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Inline_After(byte v)
    {
        return (v == 2 && Environment.NewLine != "\r\n");
    }

    [Fact]
    public static void TestEntryPoint()
    {
        bool isUnix = Environment.NewLine != "\r\n";

        Assert.Equal(isUnix, Hoisted(2));
        Assert.False(Hoisted(3));

        Assert.Equal(isUnix, Inline_Before(2));
        Assert.False(Inline_Before(3));

        Assert.Equal(isUnix, Inline_After(2));
        Assert.False(Inline_After(3));

        // All three methods must produce the same result.
        Assert.Equal(Hoisted(2), Inline_After(2));
        Assert.Equal(Inline_Before(2), Inline_After(2));
    }
}
