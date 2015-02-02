// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class NullableTests
{
    [Fact]
    public static void TestBasics()
    {
        // Nullable and Nullable<T> are mostly verbatim ports so we don't test much here.
        Nullable<int> n = default(Nullable<int>);
        bool b;
        int v;
        int h;
        String s;
        int i;

        b = n.HasValue;
        Assert.False(b);

        try
        {
            v = n.Value;
            Assert.True(false, "Nullable<>.Value should have thrown.");
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            i = (int)n;
            Assert.True(false, "Nullable<>.Value should have thrown.");
        }
        catch (InvalidOperationException)
        {
        }

        b = n.Equals(null);
        Assert.True(b);

        b = n.Equals(7);
        Assert.False(b);

        h = n.GetHashCode();
        Assert.Equal(h, 0);

        s = n.ToString();
        Assert.Equal(s, "");

        v = n.GetValueOrDefault();
        Assert.Equal(v, default(int));

        v = n.GetValueOrDefault(999);
        Assert.Equal(v, 999);

        n = new Nullable<int>(42);
        b = n.HasValue;
        Assert.True(b);

        v = n.Value;
        Assert.Equal(v, 42);

        i = (int)n;
        Assert.Equal(i, 42);

        b = n.Equals(null);
        Assert.False(b);

        b = n.Equals(7);
        Assert.False(b);

        b = n.Equals(42);
        Assert.True(b);

        h = n.GetHashCode();
        Assert.Equal(h, 42.GetHashCode());

        s = n.ToString();
        Assert.Equal(s, 42.ToString());

        v = n.GetValueOrDefault();
        Assert.Equal(v, 42);

        v = n.GetValueOrDefault(999);
        Assert.Equal(v, 42);

        n = 88;
        b = n.HasValue;
        Assert.True(b);

        v = n.Value;
        Assert.Equal(v, 88);
    }

    [Fact]
    public static void TestBoxing()
    {
        Nullable<int> n = new Nullable<int>(42);
        Foo(n);
    }

    private static void Foo(Object o)
    {
        Type t = o.GetType();

        if (t == typeof(Nullable<int>))
        {
            Assert.True(false, "Compiler did not implement the special boxing/unboxing rules for Nullable<T>");
        }

        Assert.Equal(t, typeof(int));
    }

    [Fact]
    public static void TestGetUnderlyingType()
    {
        Type t;
        Type u;

        t = typeof(int);
        u = Nullable.GetUnderlyingType(t);
        Assert.Null(u);

        t = typeof(Nullable<int>);
        u = Nullable.GetUnderlyingType(t);
        Assert.Equal(u, typeof(int));

        t = typeof(G<int>);
        u = Nullable.GetUnderlyingType(t);
        Assert.Null(u);

        return;
    }

    [Fact]
    public static void TestCompareAndEquals()
    {
        Nullable<int> n1;
        Nullable<int> n2;
        bool b;
        int i;

        n1 = default(Nullable<int>);
        n2 = default(Nullable<int>);
        b = Nullable.Equals<int>(n1, n2);
        Assert.True(b);
        i = Nullable.Compare<int>(n1, n2);
        Assert.Equal(i, 0);

        n1 = new Nullable<int>(7);
        n2 = default(Nullable<int>);
        b = Nullable.Equals<int>(n1, n2);
        Assert.False(b);
        i = Nullable.Compare<int>(n1, n2);
        Assert.Equal(i, 1);

        n1 = default(Nullable<int>);
        n2 = new Nullable<int>(7);
        b = Nullable.Equals<int>(n1, n2);
        Assert.False(b);
        i = Nullable.Compare<int>(n1, n2);
        Assert.Equal(i, -1);

        n1 = new Nullable<int>(5);
        n2 = new Nullable<int>(7);
        b = Nullable.Equals<int>(n1, n2);
        Assert.False(b);
        i = Nullable.Compare<int>(n1, n2);
        Assert.Equal(i, -1);

        n1 = new Nullable<int>(7);
        n2 = new Nullable<int>(7);
        b = Nullable.Equals<int>(n1, n2);
        Assert.True(b);
        i = Nullable.Compare<int>(n1, n2);
        Assert.Equal(i, 0);
    }

    private class G<T>
    {
    }
}

