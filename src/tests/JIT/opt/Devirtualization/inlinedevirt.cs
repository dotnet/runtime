// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class ImprovedType
{
    // Jit should inline this method and then devirtualize ToString()
    static void Print(object o)
    {
        Console.WriteLine(o.ToString());
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Print("hello, world!");
        return 100;
    }
}
