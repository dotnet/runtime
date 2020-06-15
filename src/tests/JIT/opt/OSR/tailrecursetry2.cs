// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Tail recursion candidate with OSR entry in a try region

class TailRecursionCandidateOSREntryInTry
{
    public unsafe static int F(int from, int to, int n, int result, int *x)
    {
        try 
        {
            for (int i = from; i < to; i++)
            {
                result += i;
            }
        }
        catch(Exception)
        {
        }

        if (n <= 0) return result;

        int delta = to - from;

        // Tail recursive site, but can't tail call
        return F(to, to + delta, n-1, result, &result);
    }

    public static unsafe int Main()
    {
        int x = 0;
        int result = F(0, 100_000, 9, 0, &x);
        bool ok = (result == 1783293664);
        string msg = ok ? "Pass" : "Fail";
        Console.WriteLine($"done, sum is {result}, {msg}");
        return ok ? 100 : -1;
    }  
}
