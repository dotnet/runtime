// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_116657
{
    [Fact]
    public static void TestEntryPoint()
    {
        ulong x = 0x7ffc000000000000;
        double result = Problem(ref x);
        Assert.Equal(0, result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe double Problem(ref ulong x)
    {
        if ((x & 0x7ffc000000000000) != 0x7ffc000000000000)
        {
            fixed (void* p = &x)
                return *(double*)p;
        }
        return 0;
    }
}
