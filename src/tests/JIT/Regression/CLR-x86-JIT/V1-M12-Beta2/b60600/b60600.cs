// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public unsafe class bug1
{
    public struct VT1
    {
        public double a;
    }
    public static double f(double* a0)
    {
        return *a0;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        VT1 vt = new VT1();
        double* a0 = stackalloc double[1];
        *a0 = 100;
        int[,,] arr3d = new int[5, 20, 4];
        arr3d[4, 6, 3] = 5;
        double val = f(a0);
        return (int)val;
    }
}
