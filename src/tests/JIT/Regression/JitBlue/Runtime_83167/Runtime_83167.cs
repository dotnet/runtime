// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_83167
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int Problem()
    {
        Plane p = new Plane (new Vector3(2.0f, 3.0f, 4.0f), 1.0f);
        int pH = p.GetHashCode();
        EqualityComparer<Plane> c = EqualityComparer<Plane>.Default;
        int cH = c.GetHashCode(p);
        if (pH != cH)
        {
            Console.WriteLine($"Failed: {pH:X8} != {cH:X8}");
            return 101;
        }
        return 100;
    }
}