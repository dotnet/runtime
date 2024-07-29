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
        [Fact]
        public static int TestEntryPoint()
        {
            Point a = new Point(10, 50,0,-100);
            Point b = new Point(10);
            Point c = Point.Max(a, b);
			if (((int)c.Y) != 50 || ((int)c.W) != 10)
			{
				return 0;
			}		
			Point d = Point.Min(a, b);
			Point q = Point.Min(d, d);
			if (q != d)
			{
				return 0;
			}
			if (((int)d.W) != -100)
			{
				return 0;
			}			
            return 100;
        }
    }
}
