// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;

// Assert in F() with OSR+PGO

class Runtime_69032
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int F()
    {
        var cwt = new ConditionalWeakTable<object, object>();
        for (int i = 0; i < 10_000; i++)
        {
            cwt.Add(i.ToString(), i.ToString());
            if (i % 1000 == 0) GC.Collect();
        }
        return cwt.Count();
    }

    public static int Main()
    {
        return F() / 100;
    }
}
