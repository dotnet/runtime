// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct S<K>
{
    public int x;
    public int y;
    public K val;
}

public class X<K,V> 
{
    public X(K k)
    {
        a = new S<K>[2];
        a[1].val = k;
        a[1].x = 3;
        a[1].y = 4;
    }

    public void Assert(bool b)
    {
        if (!b) throw new Exception("bad!");
    }

    public int Test()
    {
        int r = 0;
        for (int i = 0; i < a.Length; i++)
        {
            Assert(a[i].val != null);
            r += a[i].val.GetHashCode();
        }
        return r;
    }

    S<K>[] a;
}

public class B
{
    [Fact]
    public static int TestEntryPoint()
    {
        var a = new X<int, string>(11);
        int z = a.Test();
        return (z == 11 ? 100 : 0);
    }
}
