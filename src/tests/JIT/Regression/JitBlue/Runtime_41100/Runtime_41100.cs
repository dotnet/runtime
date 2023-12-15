// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The test was showing a wrong copy propagation when a struct field was rewritten by
// a call assignment to the parent struct but that assignment was not supported by copyprop.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit;

public class X
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void E(ImmutableArray<string> a) {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ImmutableArray<string> G() => ImmutableArray<string>.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ImmutableArray<string> H()
    {
        string[] a = new string[100];

        for (int i = 0; i < a.Length; i++)
        {
            a[i] = "hello";
        }

        return ImmutableArray.Create<string>(a);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F()
    {
        var a = H();
        int r = 0;

        foreach (var s in a)
        {
            if (s.Equals("hello")) r++;
        }

        var aa = a;

        if (r > 0)

        {
            foreach (var s in a)
            {
                if (s.Equals("hello")) r--;
            }

            aa = G();

            foreach (var s in a)
            {
                if (s.Equals("hello")) r++;
            }
        }

        E(aa);

        return r;
    }

    [Fact]
    public static int TestEntryPoint() => F();
}
