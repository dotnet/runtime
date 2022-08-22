// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// PGO enables an invariant GDV type test in a loop.
// We then clone the loop based on this test.
//
// COMPlus_TieredPGO=1

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

class CloningForIEnumerable
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum(IEnumerable<int> e)
    {
        int r = 0;
        foreach(int i in e)
        {
            r += i;
        }
        return r;
    }

    public static int Main()
    {
        List<int> list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        int r = 0;

        for (int i = 0; i < 30; i++)
        {
            r += Sum(list);
            Thread.Sleep(15);
        }

        Thread.Sleep(50);

        for (int i = 0; i < 70; i++)
        {
            r += Sum(list);
        }
        
        return r - 5400;
    }
}
