// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector4;
using Xunit;

namespace VectorMathTests
{
    public class Program
    {
        static Point a;
        [Fact]
        public static int TestEntryPoint()
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
