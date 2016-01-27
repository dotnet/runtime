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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void FPRoots(float a, float b, float c, ref float r1, ref float r2) 
    { 
       r1 = (-b + (float)Math.Sqrt((double)(b*b - 4*a*c)))/(2*a);
       r2  = (-b - (float)Math.Sqrt((double)(b*b - 4*a*c)))/(2*a);
       Console.WriteLine(r1);
       Console.WriteLine(r2);
       return ; 
    }

    public static int Main()
    {
        float x1 = 0;
        float x2 = 0;
        FPRoots(1f, -5f, 6f, ref x1, ref x2);
        Console.WriteLine(x1 + "," + x2);
        if (System.Math.Abs(x1-3f) > Single.Epsilon) return Fail;
        if (System.Math.Abs(x2-2f) > Single.Epsilon) return Fail;
        return Pass;
    }
}
