// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPFillArray
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPArray(float []x) 
    { 
       float sum = 0;
       for (int i=0; i < x.Length; ++i)
           sum += x[i];

       return sum / x.Length; 
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int FPFillArray(float []x, int start, int end, float y) 
    { 
       for (int i=start; i< end; ++i)
          x[i] = y++;

       return end-start;
    }    

    [Fact]
    public static int TestEntryPoint()
    {
        float []arr = new float[5];
        if (FPFillArray(arr, 0, arr.Length, 1f) != arr.Length) return Fail;
        float y = FPArray(arr);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
