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
    public static float FPSqrt(float x)
    {
       return (float)System.Math.Sqrt(x);
    }

    // Computes area of a triangle given its three sides
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPArea(float a, float b, float c) 
    {
        float s = (a+b+c)/2f;
        return FPSqrt(s*(s-a)*(s-b)*(s-c));
    }


    public static int Main()
    {
        float y = FPArea(3f, 4f, 5f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
