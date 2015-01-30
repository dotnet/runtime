// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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

    public static int Main()
    {
        float []arr = new float[5];
        if (FPFillArray(arr, 0, arr.Length, 1f) != arr.Length) return Fail;
        float y = FPArray(arr);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
