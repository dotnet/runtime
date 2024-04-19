// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_96591_2
{
    [Fact]
    public static int TestEntryPoint()
    {
        return 99 + Foo(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(int y)
    {
        int x = 0;
        if (y != 0)
        {
            do
            {
                x++;
            }
            while (x < 0);
        }
        return x;
    }
}
