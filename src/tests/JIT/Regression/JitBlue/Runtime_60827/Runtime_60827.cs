// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public struct S0
{
    public ushort F1;
    public ushort F2;
    public byte F3;

    public void M21()
    {
        M25(0, 1, 2, 3, this);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M25(int r0, int r1, int r2, int r3, S0 stkArg)
    {
    }
}

public class Runtime_60827
{
    [Fact]
    public static int TestEntryPoint()
    {
        new S0().M21();

        return 100;
    }
}
