// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test has two effectively identical methods, one of which copies
// its input parameter to a local, allowing it to be promoted.
// The JIT should be able to generate identical code for these.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_5556
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanAsParam(Span<long> span)
    {
        long value = 0;
        for (int i = 0; i < span.Length; i++)
        {
            value = span[i];
        }
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static long SpanWithLocalCopy(Span<long> span)
    {
        var spanLocal = span;
        long value = 0;
        for (int i = 0; i < spanLocal.Length; i++)
        {
            value = spanLocal[i];
        }
        return value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long[] a = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Span<long> mySpan = new Span<long>(a);
        int returnVal = 100;
        if (SpanAsParam(mySpan) != 9)
        {
            returnVal = -1;
        }
        if (SpanWithLocalCopy(mySpan) != 9)
        {
            returnVal = -1;
        }
        return returnVal;
    }
}
