// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_65942
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test1(double* a, int i)
    {
        double unused1 = a[i];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test2(float* a, int i)
    {
        float unused1 = a[i];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        double d = 0;
        Test1(&d, 0);

        float f = 0;
        Test2(&f, 0);
        return 100;
    }
}
