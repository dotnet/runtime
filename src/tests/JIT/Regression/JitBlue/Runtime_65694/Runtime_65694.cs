// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public struct Key
{
    public int a;
    public string s;
}

public struct Problem
{
    public int x;
    public double d;
    public string s0;
    public int y;
    public double e;
    public string s1;
}

public class Runtime_65694
{
    public Dictionary<Key, Problem> _d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void D()
    {
        Problem p = new Problem { s0 = "hello", s1 = "world", x = 33 };
        Key k = new Key() { a = 0, s = "a" };
        Dictionary<Key, Problem> d = new Dictionary<Key, Problem>();
        d[k] = p;

        _d = d;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void F() 
    {
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public int G(Key k, bool b)
    {
        Problem p = default;

        F();

        if (b)
        {
            if (_d?.TryGetValue(k, out p) == true && (p.x == 33))
            {
                return 22;
            }
        }

        return 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var r = new Runtime_65694();
        r.D();
        int result = 0;
        Key k = new Key() { a = 0, s = "a" };
        result += r.G(k, true);
        return result + 78;
    }
}

