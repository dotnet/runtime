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
    public static float FPAvg2(float x, float y) 
    { 
       float z = (x+y)/2.0f;
       return z; 
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPCall2(float a, float b, float c, float d)
    {
        //float e = FPAvg2(a, b);
        //float f = FPAvg2(c, d);
        //float g = FPAvg2(e, f);
        //return g;
        return FPAvg2(FPAvg2(a, b), FPAvg2(c, d));
    }

    public static int Main()
    {
        float y = FPCall2(1f, 2f, 3f, 4f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2.5f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
