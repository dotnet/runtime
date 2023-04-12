// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

// Bug where interacting CSEs of N - Old.Length and Old.Length
// were not handled properly in optCSE

public class P
{
    [Fact]
    public static int TestEntryPoint()
    {
        var ar = new double[]
        {
            100
        };
        
        FillTo1(ref ar, 5);
        Console.WriteLine(string.Join(",", ar.Select(a => a.ToString()).ToArray()));
        return (int)ar[4];
    }
    
    internal static void FillTo1(ref double[] dd, int N)
    {
        if (dd.Length >= N)
        return;
        
        double[] Old = dd;
        double d = double.NaN;
        if (Old.Length > 0)
        d = Old[0];
        
        dd = new double[N];
        
        for (int i = 0; i < Old.Length; i++)
        {
            dd[N - Old.Length + i] = Old[i];
        }
        for (int i = 0; i < N - Old.Length; i++)
        dd[i] = d;
    }
}
