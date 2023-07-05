// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector3;
using Xunit;

namespace VectorTests
{
    public class Program
    {
        const float EPS = Single.Epsilon * 5;
		
        static bool CheckEQ(float a, float b)
        {
            return Math.Abs(a - b) < EPS;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Point a = new Point(1, 2, 3);
            Point b = new Point(2, 2, 5);
            float c = 33;
            Point d = (b + a) * c;
            Point q = d + a;
            if (CheckEQ(q.X, 100))
            {
                return 100;
            }
            return 0;
        }
    }
}
