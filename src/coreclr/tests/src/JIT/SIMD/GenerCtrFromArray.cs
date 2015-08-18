using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<int>;

namespace VectorMathTests
{
    class Program
    {
        static int test(int[] arr)
        {
            int a = arr[0];
            return a;
        }

        static int Main(string[] args)
        {
			int[] arr = new int[] {1,2,3,4,5,6,7,8};
			Point p = new Point(arr, 0);
            for (int i = 0; i < Point.Count; ++i)
            {
                if (p[i] != arr[i])
                {
                    return 0;
                }
            }
            return 100;


            
        }
    }
}
