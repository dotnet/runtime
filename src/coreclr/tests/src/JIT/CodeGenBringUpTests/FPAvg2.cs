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

    public static int Main()
    {
        float y = FPAvg2(5f, 7f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
