// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public unsafe class Hoisting
{
    [Fact]
    public static int TestEntryPoint()
    {
        var p = stackalloc int[4];
        p[0] = 1;
        try
        {
            ProblemWithHwiStore(p, -1);
            return 101;
        }
        catch (OverflowException)
        {
            if (p[0] != 0)
            {
                return 102;
            }
        }

        try
        {
            ProblemWithNormalStore(p, -1);
            return 103;
        }
        catch (OverflowException)
        {
            if (p[0] != -1)
            {
                return 104;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithHwiStore(int* p, int b)
    {
        // Make sure we don't hoist the checked cast.
        for (int i = 0; i < 10; i++)
        {
            Vector128.Store(Vector128<int>.Zero, p);
            *p = (int)checked((uint)b);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithNormalStore(int* p, int b)
    {
        // Make sure we don't hoist the checked cast.
        for (int i = 0; i < 10; i++)
        {
            *p = b;
            *p = (int)checked((uint)b);
        }
    }
}
