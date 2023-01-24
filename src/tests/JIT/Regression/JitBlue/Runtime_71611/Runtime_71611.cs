// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public class Runtime_71611
{
    interface I
    {
        public int F(int x, int y);
    }

    class Add : I 
    {
        int I.F(int x, int y) => x + y;
    }

    class Mul : I
    {
        int I.F(int x, int y) => x * y;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Cloned(I m, int[] xs, int[] ys, int from, int to)
    {
        int r = 0;
        // This loop was being cloned without a null check on 'ys'
        for (int i = from; i < to; i++)
        {
            int y = ys != null ? ys[i] : 0;
            r += m.F(xs[i], y);
        }
        return r;
    }
    
    public static int Main()
    {
        int[] xs = new int[] { 1, 2, 3, 4 };
        I m = new Add();

        int r = 0;

        for (int i = 0; i < 30; i++)
        {
            r += Cloned(m, xs, null, 0, 3);
            Thread.Sleep(15);
        }

        Thread.Sleep(50);

        for (int i = 0; i < 70; i++)
        {
            r += Cloned(m, xs, null, 0, 3);
        }
        
        return r / 6;
    }
}
