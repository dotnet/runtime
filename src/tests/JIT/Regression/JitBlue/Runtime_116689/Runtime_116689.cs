// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_116689
{
    [Fact]
    public static int TestEntryPoint()
    {
        s_value = -1;
        return Test(&GetVal);
    }

    private static int s_value;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int Test(delegate*<int> foo)
    {
        return s_value + foo();
    }

    private static int GetVal()
    {
        s_value = 0;
        return 101;
    }
}
