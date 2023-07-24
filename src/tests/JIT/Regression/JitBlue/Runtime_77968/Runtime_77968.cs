// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_77968
{
    private static readonly object o = new ();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo()
    {
        // o is not an array
        // make sure VM doesn't assert in getArrayOrStringLength
        return ((int[])o).Length;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            try
            {
                Foo();
            }
            catch
            {
                // ignored
            }
            Thread.Sleep(15);
        }
        return 100;
    }
}
