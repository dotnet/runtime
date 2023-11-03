// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector2;
using Xunit;

namespace VectorMathTests
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
			Point a = new Point(10, 50);
			Point b = new Point(10, 10);
            Point c = a * b;
			if (((int)c.X) == 100)
			{
				return 100;
			}
			return 0;
        }
    }
}
