using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector2;

namespace VectorMathTests
{
    class Program
    {

      

        static int Main(string[] args)
        {
			Point a = new Point(3, 60);
			Point b = new Point(10, 40);
            return (int)(a + b).Y;
        }
    }
}
