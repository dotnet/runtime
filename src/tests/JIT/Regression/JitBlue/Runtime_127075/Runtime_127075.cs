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

    private static readonly IReadOnlyCollection<I> s_tail = [new A(), new A()];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static I[] Combine(IReadOnlyCollection<I> head)
    {
        return [.. head, .. s_tail];
    }

    [Fact]
    public static void TestEntryPoint()
    {
        I[] input = [new A(), new A(), new A(), new A()];
        for (int i = 0; i < 300; i++)
        {
            I[] result = Combine(input);
            Assert.Equal(input.Length + s_tail.Count, result.Length);
            foreach (I item in result)
            {
                Assert.NotNull(item);
            }
            Thread.Sleep(1);
        }
    }
}
