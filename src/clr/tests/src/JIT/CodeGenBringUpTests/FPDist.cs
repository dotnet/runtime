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
    public static float FPDist(float x1, float y1, float x2, float y2) 
    { 
       float z = (float) Math.Sqrt((double)((x2-x1)*(x2-x1) + (y2-y1)*(y2-y1)));
       return z; 
    }

    public static int Main()
    {
        float y = FPDist(5f, 7f, 2f, 3f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-5f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
