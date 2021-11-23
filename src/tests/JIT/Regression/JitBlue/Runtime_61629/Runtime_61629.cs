// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Runtime_61629
{
    public static int Main() => 
        Test(100, 200.0) + Test(Math.PI, Math.PI) - 72;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(double a, double b)
    {
        return (int)a ^ (int)b >> 32;
    }
}
