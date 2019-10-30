// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector4;

namespace VectorTests
{
    class Program
    {
        const float EPS = Single.Epsilon * 5;

        static bool CheckEQ(float a, float b)
        {
            return Math.Abs(a - b) < EPS;
        }

        static int Main(string[] args)
        {
            Point a = new Point(1, 2, 3, 4);
            Point b = new Point(2, 2, 1, 1);
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
