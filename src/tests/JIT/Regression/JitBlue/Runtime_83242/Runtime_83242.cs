// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_83242
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Map(int i)
    {
        if (i == 5) return -1;
        return i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Setup(int[][] a)
    {
        for (int i = 0; i < a.Length; i++)
        {
            a[i] = new int[5];

            for (int j = 0; j < 5; j++)
            {
                a[i][j] = j;
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int[][] a = new int[11][];
        int sum = 0;
        Setup(a);
        
        for (int i = 0; i < a.Length; i++)
        {
            int ii = Map(i);

            // Need to ensure ii >= 0 is in the cloning
            // conditions for the following loop
            
            for (int j = 0; j < 5; j++)
            {
                if (ii >= 0)
                {
                    sum += a[ii][j];
                }
            }
        }

        Console.WriteLine($"sum is {sum}\n");
        return sum;
    }
}
