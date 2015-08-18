using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector3;

namespace VectorMathTests
{
    class Program
    {

      

        static int Main(string[] args)
        {
			Point a = new Point(1,2,3);
			Point b = new Point(2,2, 5);
			float c = 33;
			Point d = (b + a)*c;
			Point q = d + a;
            return (int)(q).X;
        }
    }
}
