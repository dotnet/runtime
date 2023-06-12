// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public struct A
{
    public short a;
    public short b;
}

public class TailCallStructPassing
{
    public static int bar(int count, A temp)
    {
        if (count < 100)
        {
            return count;
        }

        else
        {
            count -= 100;
            return bar(count, temp);
        }
    }

    public static int foo(A temp, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p, int q, int r, int s, int t, int u, int v, int w, int decision, int count)
    {
        if (decision < 100)
        {
            return foo(temp, w, v, u, t, s, r, q, p, o, n, m, l, k, j, i, h, g, f, e, d, c, b, 500, 15);
        }

        else
        {
            return bar(count, temp);
        }
    }

    public static int foo(int decision, int count, int a, int b, int c, int d, int e, int f, int g, int h, int i, int j, int k, int l, int m, int n, int o, int p, int q, int r, int s, int t, int u, int v, int w, int x, int y, int z, A temp)
    {
        if (decision < 100)
        {
            return foo(temp, w, v, u, t, s, r, q, p, o, n, m, l, k, j, i, h, g, f, e, d, c, b, 500, 15);
        }

        else
        {
            return bar(count, temp);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        A temp = new A();
        temp.a = 50;
        temp.b = 100;

        int ret = foo(50, 19000, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 24, 25, 26, temp);

        temp.a = (short)ret;

        if (temp.a == 15)
        {
            return 100;
        }

        else
        {
            return -1;
        }
    } 
}
