// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Bug
{
    [Fact]
    public static int TestEntryPoint()
    {
        double d1 = 0;
        d1 = -d1;
        Console.WriteLine(1 / d1);
        Object d2 = d1;
        double d3 = (double)d2;
        Console.WriteLine(1 / d3);
        return 100;
    }
}
