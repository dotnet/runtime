// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Test case showing that we can have "more complex"
// IR after a tail call if we do early flow opts.

interface IX
{
}

public class X : IX
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    bool P1(object o) => false;

    bool P0(object o)
    {
        bool b = false;
        if (b)
        {
            return false;
        }
        return P1(o);
    }

    // F3 needs to be too big to inline without being marked noinline,
    // so that it ends up being tail called.
    bool F3(object o)
    {
        int result = 0;
        for (int i = 0; i < 100; i++)
        {
            result += i;
        }
        return result == 4950;
    }

    //  This method mainly serves to introduce are return spill temp
    bool F2(object o)
    {
        bool b = false;
        if (b)
        {
            return false;
        }
        return F3(o);
    }

    //  This method mainly serves to introduce are return spill temp
    bool F1(object o)
    {
        if (P0(o))
        {
            return false;
        }

        return F2(o);
    }

    // F0 is the method of interest. It will end up tail calling F3,
    // and will initially have a chain of moves and casts after the
    // call site which may trip up post tail call validation.
    //
    // We want F0 to be jitted, not inlined, but can't mark it as
    // noinline or we'll also suppress tail calls.
    bool F0(object o)
    {
        if (o == null)
        {
            return false;
        }

        object ix = o as IX;

        if (ix == null)
        {
            return false;
        }

        return F1(ix);
    }

    // This stops F0 from being inlined
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        X x = new X();
        bool b = x.F0(x);
        return b ? 100 : -1;
    }
}
