// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// OSR method must access memory argument

class MemoryArgument
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int a, int b, int c, int d, int from, int to)
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            result += i;
        }
        return result;
    }

    public static int Main(string[] args)
    {
        int final = 1_000_000;
        int result = F(0, 0, 0, 0, 0, final);
        return result == 1783293664 ? 100 : -1;
    }  
}
