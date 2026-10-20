// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135809
{
    public struct S
    {
        public long X, Y;
    }

    public class C
    {
        public static S F;

        static C()
        {
            F.X = 3;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(2, Test(default, new S { X = 2 }));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Test(S a, S b)
    {
        (a, b) = (b, C.F);
        return a.X;
    }
}
