// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

interface I<T>
{
    int E(T t);
}

sealed class J<T> : I<T>
{
    public int E(T t) { return 3; }
}

class Z
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool F0<T>(I<T> i)
    {
        return i is I<object>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool F1<T>(J<T> j)
    {
        return j is I<string>;
    }

    public static int Main()
    {
        var j0 = new J<object>();
        var j1 = new J<string>();
        bool b00 = F0(j0);
        bool b01 = F0(j1);
        bool b10 = F1(j0);
        bool b11 = F1(j1);

        int a = 0;
        if (b00) a += 1;
        if (b01) a += 2;
        if (b10) a += 4;
        if (b11) a += 8;

        Console.WriteLine($"a = {a}");

        return a == 9 ? 100 : 0;
    }
}
