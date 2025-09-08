// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Runtime.CompilerServices;

using Point = System.Numerics.Vector2;
using Xunit;

namespace Runtime_52864
{
    public class test
    {
        static Point checkA;
        static Point checkB;
        static Point checkC;
        static int   returnVal;

        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

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

        [Fact]
        public static int TestEntryPoint()
        {
            returnVal     = 100;
            Random random = new Random(Seed);

            for (int i = 0; i < 50; i++)
            {
                List<Point> p = new List<Point>();

                checkA = NextPoint(random);
                p.Add(checkA);
                checkB = NextPoint(random);
                p.Add(checkB);
                checkC = NextPoint(random);
                p.Add(checkC);

                FailureCase(p);

                Thread.Sleep(15);
            }

            for (int i = 0; i < 50; i++)
            {
                List<Point> p = new List<Point>();

                checkA = NextPoint(random);
                p.Add(checkA);
                checkB = NextPoint(random);
                p.Add(checkB);
                checkC = NextPoint(random);
                p.Add(checkC);

                FailureCase(p);
            }

            return returnVal;
        }
    }
}