// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public interface IGet 
{
    int Get();
}

public struct R : IGet
{
    public double d;
    public int a;

    public int Get() { return a; }
}

public class X<K> where K: IGet
{
    public X(K r)
    {
        a = new K[2];
        a[0] = r;
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
            Assert(a[i] != null);
            r += a[i].Get();
        }
        return r;
    }

    K[] a;
}

public class B
{
    [Fact]
    public static int TestEntryPoint()
    {
        var r = new R();
        r.a = 3;
        var a = new X<R>(r);
        int result = a.Test();
        bool passed = result == 3;
        Console.WriteLine("Passed: {0}", passed);
        return passed ? 100 : 0;
    }
}
