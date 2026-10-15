// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135811
{
    private class C
    {
        public int Value;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(3, Test(null));
        Assert.Equal(1, Test(new C { Value = 1 }));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(C obj)
    {
        bool notNull = obj != null;
        if (obj != null)
        {
            if (notNull & (obj.Value > 0))
                return 1;
        }
        return 3;
    }
}
