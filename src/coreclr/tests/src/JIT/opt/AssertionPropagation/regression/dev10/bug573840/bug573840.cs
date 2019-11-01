// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;


internal struct Position
{
    public byte X;
    public byte Y;
}


internal class Program
{
    private static int Main(string[] args)
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
