// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// Patchpoint in generic method

class GenericMethodPatchpoint
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F<T>(T[] data, int from, int to) where T : class
    {
        int result = 0;
        for (int i = from; i < to; i++)
        {
            if (data[i] == null) result++;
        }
        return result;
    }

    public static int Main()
    {
        string[] a = new string[1000];
        a[111] = "hello, world";
        int result = F(a, 0, a.Length);
        Console.WriteLine($"done, result is {result}");
        return result == 999 ? 100 : -1;
    }  
}
