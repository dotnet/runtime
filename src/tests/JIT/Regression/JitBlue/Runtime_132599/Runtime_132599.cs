// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// optImpliedByTypeOfAssertions() treated *any* "lcl != <const>" assertion as a non-null
// assertion, because it never checked that the constant was 0. The exact-type assertion
// produced by the guarded devirtualization check on Type.Equals ("t is RuntimeType")
// therefore activated the unrelated "t != typeof(Row)" assertion inside the devirtualized
// block itself, and assertion prop folded the comparison to "not equal". Count() below
// then stopped incrementing once the method reached tier-1/OSR.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_132599;

public class Row
{
}

public abstract class Holder
{
    public abstract Type Get();
}

public sealed class ObjHolder : Holder
{
    private readonly object _o;

    public ObjHolder(object o) => _o = o;

    // Guarded-devirtualized and inlined. The return spill temp loses the "exactly
    // RuntimeType" class info, so the Type.Equals call below needs a guarded
    // devirtualization of its own, which is what produces the exact-type assertion.
    public override Type Get() => _o.GetType();
}

public class Runtime_132599
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count(Holder h, int n)
    {
        int c = 0;
        for (int i = 0; i < n; i++)
        {
            if (h.Get().Equals(typeof(Row)))
            {
                c++;
            }
        }
        return c;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // The loop has to be long enough for the method to tier up via OSR while it runs.
        const int N = 1_000_000;

        Holder h = new ObjHolder(new Row());
        int actual = Count(h, N);

        Assert.Equal(N, actual);
    }
}
