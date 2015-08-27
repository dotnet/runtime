using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<int>;

namespace VectorMathTests
{
    class Program
    {
        static float Do(Point p)
        {
            return p[0];
        }

        struct S
        {
            public Point p;
        }

        class C
        {
            public Point p;
        }

        static int Main(string[] args)
        {
            Point p = new Point(1);

            S s = new S();
            C c = new C();
            s.p = new Point(1);
            c.p = new Point(2);
            if (((int)Do(s.p) != 1))
            {
                return 0;
            }
            if (((int)c.p[1]) != 2 || ((int)c.p[1]) != 2)
            {
                return 0;
            }
            Point[] fixedArr = new Point[5];
            Point fixedPoint = new Point(1);
            fixedArr[0] = fixedPoint;
            if (fixedArr[0][0] != 1)
            {
                return 0;
            }

            List<Point> points = new List<Point>();
            points.Add(fixedPoint);
            if (points[0][1] != 1)
            {
                return 0;
            }

            return 100;
        }
    }
}
