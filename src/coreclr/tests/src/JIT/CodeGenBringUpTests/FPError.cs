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
    public static float FPError(float x, float y) { 
         return x - (x/y)*y;
    }

    public static int Main()
    {
        float y = FPError(81f, 16f);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
