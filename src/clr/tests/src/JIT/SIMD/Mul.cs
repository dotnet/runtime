using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector2;

namespace VectorMathTests
{
    class Program
    {

      

        static int Main(string[] args)
        {
			Point a = new Point(10, 50);
			Point b = new Point(10, 10);
            Point c = a * b;
			return (int)c.X;
        }
    }
}
