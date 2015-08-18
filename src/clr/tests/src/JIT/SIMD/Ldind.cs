using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Point = System.Numerics.Vector4;


namespace Test
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
   
    static class Program
    {
        static int Main()
        {
            Point x = new Point(1);
			Point y, z;
            unsafe
            {
                Do1(&x, &y);
                Do2(&y, &z);
            }
			if (y.X != 1)
			{
				return 0;
			}
			if (z.X != 1)
			{
				return 0;
			}
			return 100;
        }

        // Disable inlining to permit easier identification of the code
        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe static void Do1(Point* src, Point* dst)
        {
            *((Point*)dst) = *((Point*)src);
        }

        // Disable inlining to permit easier identification of the code
        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe static void Do2(Point* src, Point* dst)
        {
            *((long*)dst) = *((long*)src);
        }
    }
}