// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Repro
{
    private struct foo
    {
        public int x, y;
    }

    [Fact]
    public static int TestEntryPoint()
    {

        foo f = new foo();
        f.x = f.y = 1;
        Console.WriteLine(f.x + f.y);

        Console.WriteLine(BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL)));

        Console.WriteLine("PASS!");
        return 100;
    }
}
