// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_120903;

using System;
using System.Runtime.CompilerServices;
using Xunit;

class B {}
class D1: B {}
class D2: B {}

public class Runtime_120903
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Merge(object[] b)
    {
        D1[] d1 = Unsafe.As<D1[]>(b);
        return d1.Length;
    }

    [Fact]
    public static void Problem()
    {
        D2[] d2 = new D2[10];
        Console.WriteLine(Merge(d2));
    }
}
