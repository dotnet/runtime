// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Numerics;
using Point = System.Numerics.Vector4;

namespace VectorMathTests
{
    class Program
    {
        static int Main(string[] args)
        {
            Point a = new Point(11, 13, 8, 4);
            Point b = new Point(11, 13, 2, 1);
            Point d = a * b;
            Point s = Point.SquareRoot(d);
            s *= -1;
            s = Point.Abs(s);
            if ((int)(s.X) == 11 && (int)(s.Y) == 13 && (int)(s.Z) == 4 && (int)(s.W) == 2)
            {
                return 100;
            }
            return 0;
        }
    }
}
