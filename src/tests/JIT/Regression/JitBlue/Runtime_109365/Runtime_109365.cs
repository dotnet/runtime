// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_109365
{
    [Fact]
    public static void TestEntryPoint()
    {
        var c = new Runtime_109365();
        for (var i = 0; i < 1000; i++) // triggers tier-1
        {
            c.Hash(i);
            Thread.Sleep(1);
        }
    }

    private readonly static int[] _perm = [1,2,3,4];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Hash(int value)
    {
        return _perm[value & (_perm.Length - 1)];
    }
}
