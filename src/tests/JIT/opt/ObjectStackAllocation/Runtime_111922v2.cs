// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_111922v2
{
    [Fact]
    public static int Test()
    {
        LinkedList<string> e = new LinkedList<string>();
        LinkedList<string> e1 = new LinkedList<string>();
        e1.AddLast("b");
        e1.AddLast("a");

        int sum = -80;

        for (int i = 0; i < 200; i++)
        {
            sum += Problem(i % 10 == 0 ? e : e1) ? 1 : 0;
            Thread.Sleep(5);
        }

        Console.WriteLine(sum);
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Problem(IEnumerable<string> e)
    {
        return e.Contains("a", StringComparer.OrdinalIgnoreCase);
    }
}
