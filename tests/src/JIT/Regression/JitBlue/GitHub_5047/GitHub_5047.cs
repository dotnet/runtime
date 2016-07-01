// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Get42()
    {
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Get43()
    {
        return 43;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float Test()
    {
        float x = Get42();
        x *= Get43();
        return x;
    }

    static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (Test() == 1806)
        {
            Console.WriteLine("Passed");
            return Pass;
        }
        else
        {
            Console.WriteLine("Failed");
            return Fail;
        }
    }
}
