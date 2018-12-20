// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Point = System.Numerics.Vector2;

namespace GitHub_21546
{
    public class test
    {
        static Point checkA;
        static Point checkB;
        static Point checkC;
        static int   returnVal;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void check(Point a, Point b, Point c)
        {
            if (a != checkA)
            {
                Console.WriteLine($"A doesn't match. Should be {checkA} but is {a}");
                returnVal = -1;
            }
            if (b != checkB)
            {
                Console.WriteLine($"B doesn't match. Should be {checkB} but is {b}");
                returnVal = -1;
            }
            if (c != checkC)
            {
                Console.WriteLine($"C doesn't match. Should be {checkC} but is {c}");
                returnVal = -1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void FailureCase(List<Point> p)
        {
            Point p1 = p[0];
            Point p2 = p.Last();

            check(p1, p[1], p2);
            check(p1, p[1], p2);
            check(p1, p[1], p2);
        }

        static Point NextPoint(Random random)
        {
            return new Point(
                (float)random.NextDouble(),
                (float)random.NextDouble()
            );
        }

        static int Main()
        {
            returnVal     = 100;
            Random random = new Random(13);
            List<Point> p = new List<Point>();

            checkA = NextPoint(random);
            p.Add(checkA);
            checkB = NextPoint(random);
            p.Add(checkB);
            checkC = NextPoint(random);
            p.Add(checkC);

            FailureCase(p);

            return returnVal;
        }
    }
}
