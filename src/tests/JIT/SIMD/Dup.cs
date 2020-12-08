// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector4;

namespace VectorMathTests
{
    class Program
    {
        static Point a;
        static int Main(string[] args)
        {
            Point p = new Point(1, 2, 3, 4);
            Point c = p;
            p.X = 2;
            if (p.X == c.X)
            {
                return 0;
            }
            return 100;
        }
    }
}
