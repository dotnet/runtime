// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Xunit;

public static unsafe class NullableTests
{
    [Fact]
    public static void TestBasics()
    {
        // Nullable and Nullable<T> are mostly verbatim ports so we don't test much here.
        Nullable<int> n = default(Nullable<int>);
        Assert.False(n.HasValue);
        Assert.Throws<InvalidOperationException>(() => n.Value);
        Assert.Throws<InvalidOperationException>(() => (int)n);
        Assert.Equal(null, n);
        Assert.NotEqual(7, n);
        Assert.Equal(0, n.GetHashCode());
        Assert.Equal("", n.ToString());
        Assert.Equal(default(int), n.GetValueOrDefault());
        Assert.Equal(999, n.GetValueOrDefault(999));

        n = new Nullable<int>(42);
        Assert.True(n.HasValue);
        Assert.Equal(42, n.Value);
        Assert.Equal(42, (int)n);
        Assert.NotEqual(null, n);
        Assert.NotEqual(7, n);
        Assert.Equal(42, n);
        Assert.Equal(42.GetHashCode(), n.GetHashCode());
        Assert.Equal(42.ToString(), n.ToString());
        Assert.Equal(42, n.GetValueOrDefault());
        Assert.Equal(42, n.GetValueOrDefault(999));

        n = 88;
        Assert.True(n.HasValue);
        Assert.Equal(88, n.Value);
    }

    [Fact]
    public static void TestBoxing()
    {
        Nullable<int> n = new Nullable<int>(42);
        Foo(n);
    }

    private static void Foo(object o)
    {
        Type t = o.GetType();
        Assert.IsNotType<Nullable<int>>(t);

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
