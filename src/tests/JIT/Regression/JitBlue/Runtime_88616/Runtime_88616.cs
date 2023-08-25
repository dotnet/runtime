// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_88616
{
    [Fact]
    public static int TestEntryPoint()
    {
        S foo;
        foo.A = 10;
        foo.B = 20;
        foo.C = 30;
        foo.D = 40;
        foo.E = 50;
        foo.A += foo.A += foo.A += foo.A += foo.A;
        foo.B += foo.B += foo.B += foo.B += foo.B;
        foo.C += foo.C += foo.C += foo.C += foo.C;
        foo.D += foo.D += foo.D += foo.D += foo.D;
        foo.E += foo.E += foo.E += foo.E += foo.E;
        // 'foo' is a last use here (it is fully promoted so all of its state
        // is stored in separate field locals), so physical promotions marks
        // this occurence as GTF_VAR_DEATH.
        Mutate(foo);
        // However, we cannot use the fact that we wrote the fields back into
        // the struct local above to skip those same write backs here; if we
        // skip those write backs then we effectively introduce new uses of the
        // struct local.
        return Check(foo);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Mutate(S s)
    {
        s.A = -42;
        Consume(ref s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(ref S s)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Check(S s)
    {
        if (s.A != 50)
        {
            Console.WriteLine("FAIL: s.A == {0}", s.A);
            return -1;
        }

        return 100;
    }

    private struct S
    {
        public long A, B, C, D, E;
    }
}
