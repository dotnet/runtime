using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector4;

namespace VectorMathTests
{
    class Program
    {
        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-32, 32));
            return (float)(mantissa * exponent);
        }

        static float sum(Point[] arr)
        {
            int n = arr.Length;
            Point s = new Point(0);
            for (int i = 0; i < n; ++i)
            {
                arr[i] += new Point(1);
                arr[i] *= 2;
                arr[i] -= (i == 0) ? new Point(0) : arr[i - 1];
                arr[i] += (i == n - 1) ? new Point(0) : arr[i + 1];
                s += arr[i];
            }
            return s.X;
        }

        static int Main(string[] args)
        {
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            Random random = new Random(13);
            int N = 10000;
            Point[] arr = new Point[N];
            for (int i = 0; i < N; ++i)
            {
                arr[i].X = NextFloat(random);
                arr[i].Y = NextFloat(random);
                arr[i].Z = NextFloat(random);
                arr[i].W = NextFloat(random);
            }

            for (int i = 0; i < 1000; ++i)
            {
                sum(arr);
            }
            return 100;
        }
    }
}
