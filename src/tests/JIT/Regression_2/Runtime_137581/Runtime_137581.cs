// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137581
{
    private struct Box
    {
        public int Value;
    }

    private static int Throw() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run(int n, bool fail)
    {
        // X64-NOT: dword ptr [{{r[bs]p}}
        // X64: ret
        Box box;
        box.Value = n;
        if (fail)
        {
            box.Value ^= Throw();
        }
        return box.Value + n;
    }

    [Fact]
    public static void Test()
    {
        Assert.Equal(14, Run(7, false));
        Assert.Throws<InvalidOperationException>(() => Run(7, true));
    }
}
