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
    public static int FPMath() 
    {
        double r = Math.Cos(0);
        if (Math.Abs(r - 1d) <= Double.Epsilon) return Pass;      
        return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPMath(double x) 
    {
        return Math.Round(x);

    }

    public static int Main()
    {
        int result = FPMath();

        if (result != Pass) return result;

        double r = FPMath(3.999d);
        Console.WriteLine(r);
        if (Math.Abs(r - 4d) <= Double.Epsilon) return Pass;      
        return Fail;
    }
}
