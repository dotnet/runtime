using System;
using System.Collections.Generic;
using System.Numerics;
using Point = System.Numerics.Vector4;

namespace VectorMathTests
{
    class Program
    {



        static int Main(string[] args)
        {
            Point a = new Point(1, 2, 3, 4);
            Point b = new Point(2, 2, 1, 1);
            float c = 33;
            Point d = (b + a) * c;
            Point q = d + a;
            Point s = Point.SquareRoot(q);
            s *= -1;
            s = Point.Abs(s);
            return (int)(s).X * 10;
        }
    }
}
