// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

public class Program
{
    public static unsafe int Main()
    {
        int result = 0;
        try {
            test_up_big(new int[10], 5, 2);
        } catch (IndexOutOfRangeException) {
            result = 100;
        }
        
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int test_up_big(int[] a, int s, int x)
    {
        int r = 0;
        int i;
        for (i = 1; i < s; i += 2147483647)
        {
            r += a[i];
        }
        return r;
    }
}