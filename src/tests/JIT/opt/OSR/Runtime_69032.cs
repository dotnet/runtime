// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

// Assert in F() with OSR+PGO

class Runtime_69032
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int F(int n)
    {
        var cwt = new ConditionalWeakTable<object, object>();
        for (int i = 0; i < n; i++)
        {
            cwt.Add(i.ToString(), i.ToString());
            if (i % 1000 == 0) GC.Collect();
        }
        return n;
    }

    public static int Main()
    {
        return F(10_000) / 100;
    }
}
