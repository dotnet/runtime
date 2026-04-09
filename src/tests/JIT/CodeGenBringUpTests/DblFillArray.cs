// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblFillArray
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblArray(double []x) 
    { 
       double sum = 0;
       for (int i=0; i < x.Length; ++i)
           sum += x[i];

       return sum / x.Length; 
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int DblFillArray(double []x, int start, int end, double y) 
    { 
       for (int i=start; i< end; ++i)
          x[i] = y++;

       return end-start;
    }    

    [Fact]
    public static int TestEntryPoint()
    {
        double []arr = new double[5];
        if (DblFillArray(arr, 0, arr.Length, 1f) != arr.Length) return Fail;
        double y = DblArray(arr);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
