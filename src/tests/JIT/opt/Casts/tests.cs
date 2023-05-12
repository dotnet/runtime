// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

interface I<T>
{
    int E(T t);
}

sealed class J : I<string>
{
    public int E(string s)
    {
        return s.Length;
    }
}

class K : I<string>
{
    public int E(string s)
    {
        return s.GetHashCode();
    }
}

sealed class L : K, I<object>
{
    public int E(object o)
    {
        return o.GetHashCode();
    }
}

public class F
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIString<T>(I<T> i)
    {
        return i is I<string>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsI<T,U>(I<U> i)
    {
        return i is I<T>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsJI<T>(J j)
    {
        return j is I<T>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsKI<T>(K k)
    {
        return k is I<T>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsLI<T>(L l)
    {
        return l is I<T>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsJIString(J j)
    {
        return j is I<string>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsKIString(K k)
    {
        return k is I<string>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsLIString(L l)
    {
        return l is I<string>;
    }

    #pragma warning disable CS0184
    // warning CS0184: The given expression is never of the provided ('I<object>') type
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsJIObject(J j)
    {
        return j is I<object>;
    }
    #pragma warning restore CS0184

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsKIObject(K k)
    {
        return k is I<object>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsLIObject(L l)
    {
        return l is I<object>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIStringJ(I<string> i)
    {
        return i is J;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIStringK(I<string> i)
    {
        return i is K;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIStringL(I<string> i)
    {
        return i is L;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIJ<T>(I<T> i)
    {
        return i is J;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIK<T>(I<T> i)
    {
        return i is K;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool IsIL<T>(I<T> i)
    {
        return i is K;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var j = new J();
        var k = new K();
        var l = new L();
        
        bool b0 = IsIString(j);
        bool b1 = IsIString(k);
        bool b2 = IsIString<string>(l);
        bool b3 = IsIString<object>(l);

        bool c0 = IsI<string,string>(j);
        bool c1 = IsI<string,string>(k);
        bool c2 = IsI<string,string>(l);

        bool d0 = IsI<object,string>(j);
        bool d1 = IsI<object,string>(k);
        bool d2 = IsI<object,string>(l);

        bool e0 = IsJI<string>(j);
        bool e1 = IsKI<string>(k);
        bool e2 = IsKI<string>(l);
        bool e3 = IsLI<string>(l);

        bool f0 = IsJIString(j);
        bool f1 = IsKIString(k);
        bool f2 = IsKIString(l);
        bool f3 = IsLIString(l);

        bool g0 = IsIStringJ(j);
        bool g1 = IsIStringJ(k);
        bool g2 = IsIStringJ(l);
        bool g3 = IsIStringK(j);
        bool g4 = IsIStringK(k);
        bool g5 = IsIStringK(l);
        bool g6 = IsIStringL(j);
        bool g7 = IsIStringL(k);
        bool g8 = IsIStringL(l);

        bool h0 = IsIJ<string>(j);
        bool h1 = IsIJ<string>(k);
        bool h2 = IsIJ<string>(l);
        bool h3 = IsIK<string>(j);
        bool h4 = IsIK<string>(k);
        bool h5 = IsIK<string>(l);
        bool h6 = IsIL<string>(j);
        bool h7 = IsIL<string>(k);
        bool h8 = IsIL<string>(l);

        bool j0 = IsJIObject(j);
        bool j1 = IsKIObject(k);
        bool j2 = IsKIObject(l);
        bool j3 = IsLIObject(l);

        bool pos = 
        b0 & b1 & b2 & b3 
        & c0 & c1 & c2
        & d2
        & e0 & e1 & e2 & e3 
        & f0 & f1 & f2 & f3
        & g0 & g4 & g5 & g8
        & h0 & h4 & h5 & h8
        & j2 & j3;

        bool neg = 
        d0 & d1
        & g1 & g2 & g6 & g7
        & h1 & h2 & h6 & h7
        & j0 & j1;

        return pos & !neg ? 100 : 0;
    }
}
