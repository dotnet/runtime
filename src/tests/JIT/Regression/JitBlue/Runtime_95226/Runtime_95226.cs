// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_95226
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Foo(new int[1]));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(int[] arr)
    {
        int i = 0;
        goto Bottom;
Top:;
        i++;
        while (true)
        {
            if (AlwaysTrue())
                break;
        }
Bottom:;
        Use(arr[i]);
        if (i < arr.Length)
            goto Top;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool AlwaysTrue() => true;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(int x)
    {
    }
}