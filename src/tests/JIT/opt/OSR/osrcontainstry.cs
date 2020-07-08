// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// OSR method contains try

class OSRContainsTry
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe int I(ref int p) => p;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe int F(int from, int to)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            try 
            {
                result = I(ref result) + i;
            }
            catch (Exception e)
            {
            }
        }
        return result;
    }

    public static int Main()
    {
        Console.WriteLine($"starting sum");
        int result = F(0, 1_000_000);
        Console.WriteLine($"done, sum is {result}");
        return result == 1783293664 ? 100 : -1;
    }  
}
