// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class ConstStringConstIndexOptimizations
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int AssertPropCase()
    {
        char x = GetChar()[1];
        if (x > 100)
        {
            Consume(x);
        }
        return x;
    }

    static ReadOnlySpan<char> GetChar()
    {
        return "\u924412\u044B";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FoldCase1()
    {
        ReadOnlySpan<char> span = "\u9244";
        return span[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FoldCase2()
    {
        ReadOnlySpan<char> span = "\u9244";
        return span[0] + span[1];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FoldCase3()
    {
        ReadOnlySpan<char> span = "abc";
        return span[2] + span[1];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int FoldCase4()
    {
        ReadOnlySpan<char> span = "";
        return span[0];
    }

    static readonly string Str1 = "12345";

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadonlyFieldCase1()
    {
        var f = Str1;

        if (Str1.Length > 1)
        {
            Consume(f);
        }
        return f[1] + f[2];
    }

    static string Str2 { get; } = "";

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadonlyFieldCase2()
    {
        var f = Str2;

        if (Str2.Length > 1)
        {
            Consume(f);
        }
        return f[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(object _) {}

    static void ThrowIOORE(Action action)
    {
        try
        {
            action();
        }
        catch (IndexOutOfRangeException)
        {
            return;
        }
        throw new InvalidOperationException();
    }

    static void AssertEquals(int expected, int actual)
    {
        if (expected != actual)
            throw new InvalidOperationException();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            AssertEquals(49, AssertPropCase());
            AssertEquals(37444, FoldCase1());
            ThrowIOORE(() => FoldCase2());
            AssertEquals(197, FoldCase3());
            ThrowIOORE(() => FoldCase4());
            AssertEquals(101, ReadonlyFieldCase1());
            ThrowIOORE(() => ReadonlyFieldCase2());
            Thread.Sleep(15);
        }
        return 100;
    }
}
