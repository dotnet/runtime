using System;
using System.Collections.Generic;
using System.Numerics;

namespace VectorMathTests
{
    class Program
    {



        static int Main(string[] args)
        {
			var a = new System.Numerics.Vector<short>(51);
			for(int i = 0; i < System.Numerics.Vector<short>.Count; ++i)
			{
				if (a[i] != 51)
				{
					return 0;
				}
			}
			var b = System.Numerics.Vector<int>.One;
			for(int i = 0; i < System.Numerics.Vector<int>.Count; ++i)
			{
				if (b[i] != 1)
				{
					return 0;
				}
			}
			var c = System.Numerics.Vector<short>.One;
			for(int i = 0; i < System.Numerics.Vector<short>.Count; ++i)
			{
				if (c[i] != 1)
				{
					return 0;
				}
			}
			var d = new System.Numerics.Vector<double>(100.0);
			for(int i = 0; i < System.Numerics.Vector<double>.Count; ++i)
			{
				if (d[i] != 100.0)
				{
					return 0;
				}
			}
			
			var e = new System.Numerics.Vector<float>(100);
			for(int i = 0; i < System.Numerics.Vector<float>.Count; ++i)
			{
				if (e[i] != 100.0)
				{
					return 0;
				}
			}
			var f = c * 49;
			var g = f + a;
			
			short[] array1 = new short[] { 1, 3, 5, 7, 9, 2, 1, 1, 1,5,4,3,1,2,3,5,6,7,1,1,1,1 };
            var w = new System.Numerics.Vector<short>(array1);
			return g[0];
			//return (int)c[1] + (int)b[3] - 100 + a[2] - 2 * d[1];
          
        }
    }
}
