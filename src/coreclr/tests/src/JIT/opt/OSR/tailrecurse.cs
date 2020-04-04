// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// OSR and tail recursion

class OSRTailRecursion
{
    public static int F(int from, int to, int n, int a)
    {
        int result = a;
        for (int i = from; i < to; i++)
        {
            result += i;
        }

        if (n <= 0) return result;

        int delta = to - from;

        return F(to, to + to - from, n-1, result);
    }

    public static int Main()
    {
        Console.WriteLine($"starting sum");
        int result = F(0, 100_000, 9, 0);
        bool ok = (result == 1783293664);
        string msg = ok ? "Pass" : "Fail";
        Console.WriteLine($"done, sum is {result}, {msg}");
        return  ok ? 100 : -1;
    }  
}
