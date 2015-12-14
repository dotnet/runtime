// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//Unit test for Null check assertion propagation.

using System;

internal class Point
{
    public int x;
    public int y;

    public Point(int _x, int _y) { x = _x; y = _y; }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public int Distance() { return x * x + y * y; }
}

internal class Sample5
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(Point p1, Point p2, Point p3)
    {
        int h, t;

        h = p1.Distance();

        t = p2.x + h;
        h += p2.Distance();

        if (p3.y == t)
        {
            h += p3.Distance();
        }

        return h;
    }

    private static int Main(string[] args)
    {
        try
        {
            int h = func(new Point(0, 1), new Point(2, 1), new Point(0, 3));
            if (h == 15)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
