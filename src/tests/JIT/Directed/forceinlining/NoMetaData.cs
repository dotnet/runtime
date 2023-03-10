// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class P
{
    [Fact]
    public static int TestEntryPoint()
    {
        int[,] x = new int[5, 5];
        for (int i = 0; i < 5; ++i)
            for (int j = 0; j < 5; ++j)
                x[i, j] = 7;

        Console.WriteLine("PASS");
        return 100;
    }
}
