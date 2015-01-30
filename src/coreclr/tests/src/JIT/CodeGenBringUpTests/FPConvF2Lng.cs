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
    public static long FPConvF2Lng(float x) { return (long) x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static UInt64 FPConvF2Lng(double x) { return (UInt64) x; }


    public static int Main()
    {
        int result = Fail;
        long x = FPConvF2Lng(3294168832f);
        Console.WriteLine(x);
        if (x == 3294168832L) result = Pass;
        
        int result2 = Fail;
        UInt64 y = FPConvF2Lng(3294168832d);
        Console.WriteLine(y);
        if (y == 3294168832UL) result2 = Pass;

        if (result == Pass && result2 == Pass) return Pass;
        return Fail;

    }
}
