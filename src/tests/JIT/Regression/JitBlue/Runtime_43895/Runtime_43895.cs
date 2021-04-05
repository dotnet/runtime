// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Runtime_43895
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test1(double d, ulong ul)
    {
        return (d == (double) ul);
    }

    public static int Main()
    {
        bool b1 = Test1(10648738977740919977d,  10648738977740919977ul);

        if (!b1)
        {
            Console.WriteLine("FAILED");
            return 101;
        }

        Console.WriteLine("Passed");
        return 100;
    }
}
