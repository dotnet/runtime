// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135819
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, Test(false));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(bool b)
    {
        if (!b)
            return 1;

        try
        {
            throw new Exception();
        }
        finally
        {
            while (true)
            {
            }
        }
    }
}
