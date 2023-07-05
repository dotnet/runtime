// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class B 
{
    public virtual int V() => 33;
}

public class D : B
{
    public override int V() => 44;
}

public class Runtime_70802
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void G() {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int F(B b, int n = 10, int m = 10)
    {
        int i = 0;
        int j = 0;
        int r = 0;
        goto mid;
        top:
        G();
        mid:
        r += b.V();
        i++;
        if (i < n) goto top;
        j++;
        if (i < m) goto mid;
        return r;     
    }

    [Fact]
    public static int TestEntryPoint()
    {
        D d = new D();
        
        for (int i = 0; i < 100; i++)
        {
            _ = F(d);
            Thread.Sleep(15);
        }

        Thread.Sleep(50);

        int r = 0;

        for (int i = 0; i < 100; i++)
        {
            r += F(d);
        }

        Console.WriteLine($"result is {r} (expected 44000)");

        return r / 440;
    }
}
