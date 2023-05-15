// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

class C0
{
    public sbyte F;
}

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        C0 var0 = new C0 { F = -1 };
        // The JIT was giving (byte)var0.F the same value number as the -1 assigned
        // above, which was causing the OR below to be discarded.
        ulong var1 = (ulong)(1000 | (byte)var0.F);
        return var1 == 1023 ? 100 : 0;
    }
}
