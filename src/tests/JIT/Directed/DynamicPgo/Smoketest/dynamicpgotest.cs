// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;

class DynamicPgoSmokeTest
{
    static int t = 0;
    static int s = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Bar(int i, bool b = false) 
    {
        if (b)
        {
            s++;
        }


        if ((i % 3) == 0) t++;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int Main()
    {
        for (int i = 0; i < 3_000; i++)
        {
            Bar(i);
        }

        Thread.Sleep(1000);

        for (int i = 0; i < 3_000; i++)
        {
            Bar(i);
        }

        Thread.Sleep(1000);

        for (int i = 0; i < 15_000; i++)
        {
            Bar(i);
        }

        return 100;
    }
}
 