using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector2;

namespace VectorMathTests
{
    class Program
    {

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
            if (a.X != 10 || a.Y != 20)
            {
                return 0;
            }
            return 100;

        }


        static int Main(string[] args)
        {


            Point a = new Point(0, 0), b = new Point(1, 0);
            Point c = a + b;
            Point d = c - b;
            Point e = d - a;
            if (e.X != 0 || e.Y != 0)
            {
                return 0;
            }

            e += e;
            if (e.X != 0 || e.Y != 0)
            {
                return 0;
            }

            a += new Point(5, 2);
            e += a + b;
            if (e.X != 6 || e.Y != 2)
            {
                return 0;
            }
            e *= 10;
            if (e.X != 60 || e.Y != 20)
            {
                return 0;
            }
            // Debug.Assert(e.X == 0 && e.Y == 0);      
            if (Adds() != 100)
            {
                return 0;
            }
            return 100;
        }
    }
}
