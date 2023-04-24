// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = Foo(new S32 { A = 100 });
        if (result != 100)
        {
            Console.WriteLine("FAIL: Returned {0}", result);
        }

        return result;
    }

    static int Foo(S32 s)
    {
        return Baz(s, Bar(s));
    }

    static int Bar(S32 s)
    {
        s.A = 1234;
        Consume(s);
        return 42;
    }

    static int Baz(S32 s, int arg)
    {
        return s.A;
    }

    static void Consume(S32 s) { }

    struct S32
    {
        public int A, B, C, D, E, F, G, H;
    }
}