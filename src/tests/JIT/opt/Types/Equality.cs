// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Optimization of type equality tests

using System;
using Xunit;

struct Wrap1<T> {}

struct Wrap2<T> {}

public class EqualityTests
{
    static bool IsInt<T>()
    {
        return (typeof(T) == typeof(int));
    }

    static bool IsInt<T>(T t)
    {
        return (t.GetType() == typeof(int));
    }

    static bool IsString<T>()
    {
        return (typeof(T) == typeof(string));
    }

    static bool IsString<T>(T t)
    {
        return (t.GetType() == typeof(string));
    }

    static bool IsIntArray<T>()
    {
        return (typeof(T) == typeof(int[]));
    }

    static bool IsStringArray<T>()
    {
        return (typeof(T) == typeof(string[]));
    }

    static bool IsWrap1<T,U>()
    {
        return (typeof(U) == typeof(Wrap1<T>));
    }

    static bool IsWrap1<T,U>(U u)
    {
        return (u.GetType() == typeof(Wrap1<T>));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Fully optimized
        bool c1 = IsInt<int>();
        bool c2 = IsInt<string>();
        bool c3 = IsString<int>();

        // Partially optimized (method table check)
        bool c4 = IsString<string>();

        // Fully optimized
        bool d1 = IsInt<int>(3);
        bool d3 = IsString<int>(3);

        // Partially optimized (method table check)
        bool d2 = IsInt<string>("three");
        bool d4 = IsString<string>("three");

        // Partially optimized (runtime lookup)
        bool e1 = IsIntArray<int[]>();
        bool e2 = IsIntArray<string[]>();
        bool e3 = IsStringArray<int[]>();
        bool e4 = IsStringArray<string[]>();

        // Fully optimized
        bool f1 = IsWrap1<int, Wrap1<int>>();
        bool f2 = IsWrap1<int, Wrap2<int>>();
        bool f3 = IsWrap1<int, Wrap2<int>>(new Wrap2<int>());
        bool f4 = IsWrap1<int, Wrap1<int>>(new Wrap1<int>());

        bool pos = c1 & c4 & d1 & d4 & e1 & e4 & f1 & f4;
        bool neg = c2 & c3 & d2 & d3 & e2 & e3 & f2 & f3;

        return pos & !neg ? 100 : 0;
    }
}
