// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_127075
{
    public interface I;
    public sealed class A : I;
    public sealed class B : I;

    // Roslyn lowers this collection literal stored to IReadOnlyCollection<I>
    // into a `new <>z__ReadOnlyArray<I>(new I[]{...})` wrapper. The wrapper's
    // GetEnumerator() does NOT devirtualize even under PGO (the array element
    // type is inexact), so it stays as a virtual call. That is what triggered
    // the bug: the second loop's enumerator local store was hidden from
    // conditional escape analysis.
    //
    private static readonly IReadOnlyCollection<I> s_tail = [new B(), new B()];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static I[] Combine(IReadOnlyCollection<I> head)
    {
        return [.. head, .. s_tail];
    }

    [Fact]
    public static void TestEntryPoint()
    {
        I[] head = [new A(), new A(), new A(), new A()];
        for (int i = 0; i < 300; i++)
        {
            I[] result = Combine(head);
            Assert.Equal(head.Length + s_tail.Count, result.Length);
            for (int j = 0; j < head.Length; j++)
            {
                Assert.Same(head[j], result[j]);
            }
            int k = head.Length;
            foreach (I t in s_tail)
            {
                Assert.Same(t, result[k++]);
            }
            Thread.Sleep(1);
        }
    }
}
