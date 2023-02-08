// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

struct S0
{
    public byte F0;
}

public class Program
{
    static S0 s_2;
    static long s_5;

    [Fact]
    public static int TestEntryPoint()
    {
        s_2.F0 = 128;
        M7(s_2);
        return (s_5 == 128) ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M7(S0 arg0)
    {
        s_5 = Volatile.Read(ref arg0.F0);
    }
}
