// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

// We're testing whether HWI nodes with > 2 operands propagate exception sets correctly.
//
public class ExceptionSetsPropagation_Hwi
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Problem(-1, false, false);
        }
        catch (OverflowException)
        {
            return 100;
        }

        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(int a, bool c1, bool c2)
    {
        var zero = 0;
        c1 = a == 0;
        c2 = c1;

        if ((Vector128.Create((int)checked((uint)a), a, a, a).GetElement(0) * zero) + a == 0)
        {
            return false;
        }

        return c2;
    }
}
