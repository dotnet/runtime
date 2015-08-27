using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<int>;

namespace VectorTests
{
    class Program
    {
        static int Main(string[] args)
        {
            Point p = new Point(1);
            Point[] arr = new Point[10];
            arr[0] = p; // Loadelem bytecode.
            arr[2] = p;
            if (arr[0] == arr[1] || arr[2] != p)
            {
                return 0;
            }
            return 100;
        }
    }
}
