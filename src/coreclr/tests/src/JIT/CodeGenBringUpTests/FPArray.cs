// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    // JBTodo - remove 2nd param after implementing conv from float to int
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPArray(float []x, float len) 
    { 
       float sum = 0;
       for (int i=0; i < x.Length; ++i)
           sum += x[i];

       return sum / len; 
    }

    public static int Main()
    {
        float []arr = new float[] {1f,2f,3f,4f,5f};
        float y = FPArray(arr, arr.Length);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
