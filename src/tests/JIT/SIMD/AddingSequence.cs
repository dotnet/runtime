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
		const float EPS = Single.Epsilon * 5;
		
		static bool CheckEQ(float a, float b)
        {
            return Math.Abs(a - b) < 5 * EPS;
        }

        static bool CheckNEQ(float a, float b)
        {
            return !CheckEQ(a, b);
        }

        static int Adds()
        {
            Point a = new Point(0, 0);
            Point b = new Point(1, 2);
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            a += b;
            if (CheckNEQ(a.X, 10) || CheckNEQ(a.Y, 20))
            {
                return 0;
            }
            return 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Point a = new Point(0, 0), b = new Point(1, 0);
            Point c = a + b;
            Point d = c - b;
            Point e = d - a;
            if (CheckNEQ(e.X, 0) || CheckNEQ(e.Y, 0))
            {
                return 0;
            }
            e += e;
            if (CheckNEQ(e.X, 0) || CheckNEQ(e.Y, 0))
            {
                return 0;
            }
            a += new Point(5, 2);
            e += a + b;
            if (CheckNEQ(e.X, 6) || CheckNEQ(e.Y, 2))
            {
                return 0;
            }
            e *= 10;
            if (CheckNEQ(e.X, 60) || CheckNEQ(e.Y, 20))
            {
                return 0;
            }
            if (Adds() != 100)
            {
                return 0;
            }
            return 100;
        }
    }
}
