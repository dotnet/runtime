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

    private static readonly I[] s_tailItems = [new B(), new B()];
    private static readonly IReadOnlyCollection<I> s_tail = s_tailItems;

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
            Assert.Equal(head.Length + s_tailItems.Length, result.Length);
            for (int j = 0; j < head.Length; j++)
            {
                Assert.Same(head[j], result[j]);
            }
            for (int j = 0; j < s_tailItems.Length; j++)
            {
                Assert.Same(s_tailItems[j], result[head.Length + j]);
            }
            Thread.Sleep(1);
        }
    }
}
