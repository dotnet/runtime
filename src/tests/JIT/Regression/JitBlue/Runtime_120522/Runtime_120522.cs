// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_120522
{
    static System.Collections.ArrayList array = [];

    static void Problem(ref object[]? a)
    {
        a = new object[1];
        array.Add(a.Clone());
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int Test()
    {
        int i = 0;
        while (i < 200_000)
        {
            object[]? a = null;
            Problem(ref a);
            i++;

            if (i % 10_000 == 0)
            {
                Thread.Sleep(200);
            }
        }
        
        return i / 2000;
    }
}
