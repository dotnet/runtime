// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

ref struct MyStruct<A, B>
{
    static Random r = new Random();

    Span<int> s1, s2, s3, s4, s5, s6, s7, s8, s9, s10;

    public static void Test(int depth)
    {
        MyStruct<A, B> u;
        u.s1 = My.g[r.Next(My.g.Length)];
        u.s2 = My.g[r.Next(My.g.Length)];
        u.s3 = My.g[r.Next(My.g.Length)];
        u.s4 = My.g[r.Next(My.g.Length)];
        u.s5 = My.g[r.Next(My.g.Length)];
        u.s6 = My.g[r.Next(My.g.Length)];
        u.s6 = My.g[r.Next(My.g.Length)];
        u.s7 = My.g[r.Next(My.g.Length)];
        u.s8 = My.g[r.Next(My.g.Length)];
        u.s9 = My.g[r.Next(My.g.Length)];
        u.s10 = My.g[r.Next(My.g.Length)];
        Test(depth, u);
    }

    public static void Test(int depth, MyStruct<A, B> u)
    {
        int x1 = u.s1.Length + u.s2.Length + u.s3.Length + u.s4.Length + u.s5.Length +
                 u.s6.Length + u.s7.Length + u.s8.Length + u.s9.Length + u.s10.Length;
        int x2 = u.s1[0] + u.s2[0] + u.s3[0] + u.s4[0] + u.s5[0] +
                 u.s6[0] + u.s7[0] + u.s8[0] + u.s9[0] + u.s10[0];
        if (x1 != x2)
            throw new InvalidOperationException();

        if (depth-- == 0)
            return;
        MyStruct<KeyValuePair<A, B>, B>.Test(depth);
        MyStruct<A, KeyValuePair<B, A>>.Test(depth);
    }
}

public class My
{
    static void Stress()
    {
        for (; ; )
        {
            GC.Collect();
            Thread.Sleep(1);
        }
    }

    static void Churn()
    {
        Random r = new Random();
        for (; ; )
        {
            var a = new int[1 + r.Next(100)];
            a[0] = a.Length;
            g[r.Next(g.Length)] = a;
        }
    }

    public static int[][] g = new int[10000][];

    [Fact]
    public static int TestEntryPoint()
    {
        int[] empty = new int[] { 1 };
        for (int i = 0; i < g.Length; i++)
            g[i] = empty;

        var t = new Thread(Stress);
        t.IsBackground = true;
        t.Start();
        t = new Thread(Churn);
        t.IsBackground = true;
        t.Start();

        int result = 100; // pass
        try
        {
            MyStruct<int, uint>.Test(3);
        }
        catch (InvalidOperationException)
        {
            result = 1; // fail
        }
        return result;
    }
}
