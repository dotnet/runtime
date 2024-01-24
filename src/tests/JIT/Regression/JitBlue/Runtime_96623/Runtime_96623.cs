// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_96623
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Foo(new int[15]));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(int[] arr)
    {
        int limit = GetLimit();
        int sum = 0;
        int i = 0;
        int j = 0;

        int x;

        if (Environment.TickCount < 0)
            x = 42;
        else
            x = 42;

        if (x == 42)
        {
            i = int.MaxValue;
            goto TestInner;
        }

        Console.WriteLine("Unreachable");
        goto TestOuter;

OuterStart:;
        Console.WriteLine("Outer");
        goto TestInner;

InnerStart:;
        sum += arr[i];

TestInner:;
        j++;
        if (j < 30)
            goto InnerStart;

TestOuter:;
        i++;
        if (i < limit)
            goto OuterStart;

        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetLimit() => 10;
}