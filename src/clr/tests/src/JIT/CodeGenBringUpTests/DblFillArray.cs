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

    public static int Main()
    {
        double []arr = new double[5];
        if (DblFillArray(arr, 0, arr.Length, 1f) != arr.Length) return Fail;
        double y = DblArray(arr);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
