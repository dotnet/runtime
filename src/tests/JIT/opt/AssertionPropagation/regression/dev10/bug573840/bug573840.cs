// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;


internal struct Position
{
    public byte X;
    public byte Y;
}


public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Main called");

        int[,] b = new int[5, 5];


        List<Position> g = new List<Position>();

        for (int i = 0; i < g.Count; i++)
        {
            int h = b[g[0].X, g[0].Y];
            Console.WriteLine(h);
        }

        return 100;
    }
}
