// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SYSLIB5004

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_136303
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (!X86Base.IsSupported)
        {
            return;
        }

        Assert.Throws<DivideByZeroException>(() => Discarded(0));
        Assert.Throws<DivideByZeroException>(() => Used(0));
        Assert.Equal(1, Discarded(1));
    }

    // The exception must not depend on whether the result is used.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Discarded(uint divisor)
    {
        X86Base.DivRem(1, 0, divisor);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Used(uint divisor) => (int)X86Base.DivRem(1, 0, divisor).Quotient;
}
