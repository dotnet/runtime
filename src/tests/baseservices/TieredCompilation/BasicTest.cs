// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public static class BasicTest
{
    private static int Main()
    {
        const int Pass = 100;

        PromoteToTier1(Foo);
        Foo();

        return Pass;
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
    private static void PromoteToTier1(Action action)
    {
        // Call the method once to register a call for call counting
        action();

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the method enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; i++)
        {
            action();
        }

        // Allow time for the method to be jitted at tier 1
        Thread.Sleep(500);
    }
}
