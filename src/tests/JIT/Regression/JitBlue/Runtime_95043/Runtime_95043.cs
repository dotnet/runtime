// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_95043
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Sweep(int ecx, int* edx, Vector3 stack12)
    {
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(ecx);
            if (ecx == -1)
            {
                ecx = edx[0];
            }
            else if (!float.IsNaN(stack12.X))
            {
                edx[0] = -1;
                ecx = -1;
            }
        }
    }

    [Fact]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        int x = 42;
        Sweep(1, &x, Vector3.Zero);
        Assert.Equal(-1, x);
    }
}
