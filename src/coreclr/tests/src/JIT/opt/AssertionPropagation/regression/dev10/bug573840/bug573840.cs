// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
