// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public unsafe class testout1
{
    public struct VT
    {
        public long a1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        VT vt = new VT();
        vt.a1 = 500L;

        long* a0 = stackalloc long[1];
        *a0 = -6L;
        Console.WriteLine("Should be 500");
        Console.WriteLine((long)(vt.a1));
        return 100;
    }
}
