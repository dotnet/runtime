// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Point = System.Numerics.Vector4;
using Xunit;

namespace Test
{ 
    public static class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Point x = new Point(1);
			Point y, z;
            unsafe
            {
                Do1(&x, &y);
                Do2(&y, &z);
            }
			if (((int)y.X) != 1)
			{
				return 0;
			}
			if (((int)z.X) != 1)
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
