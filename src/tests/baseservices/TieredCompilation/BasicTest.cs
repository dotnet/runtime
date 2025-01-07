// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class BasicTest
{
    [Fact]
    public static void TestEntryPoint()
    {
        PromoteToTier1(Foo, () => FooWithLoop(2));
        Foo();
        FooWithLoop(2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo()
    {
        Foo2();
    }

    private static void Foo2()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FooWithLoop(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; ++i)
        {
            sum += i;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PromoteToTier1(params Action[] actions)
    {
        // Call the methods once to register a call each for call counting
        foreach (Action action in actions)
        {
            action();
        }

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the methods enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; ++i)
        {
            foreach (Action action in actions)
            {
                action();
            }
        }

        // Allow time for the methods to be jitted at tier 1
        Thread.Sleep(500);
    }
}
