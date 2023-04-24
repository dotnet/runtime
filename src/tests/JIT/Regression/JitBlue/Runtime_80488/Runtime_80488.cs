// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_80488
{
    [Fact]
    public static int TestEntryPoint()
    {
        int code = Foo(new S16 { F1 = 100 });
        if (code != 100)
        {
            Console.WriteLine("FAIL: Returned {0}", code);
        }

        return code;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(S16 s)
    {
        Bar(s);
        return (int)s.F1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Bar(S16 s)
    {
        s.F1 = 12345;
        Consume(s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(S16 s) { }

    private struct S16
    {
        public long F0, F1;
    }
}