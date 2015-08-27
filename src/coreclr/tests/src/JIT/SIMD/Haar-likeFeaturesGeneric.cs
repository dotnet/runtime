using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<double>;

namespace VectorMathTests
{
    class Program
    {
		const float EPS = Single.Epsilon * 5;
		
        static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-32, 32));
            return (float)(mantissa * exponent);
        }

        static byte NextByte(Random random)
        {
            return (byte)random.Next(0, 2);
        }

        static double[] VectorFilter(double[] color, double[] mask)
        {
            int count = Point.Count;
            int N = color.Length;
            double[] res = new double[N];
            for (int i = 0; i < N; i += count)
            {
                Point p = new Point(color, i);
                Point m = new Point(mask, i);
                p = System.Numerics.Vector.Abs(p);
                Point r = p * m;
                for (int j = 0; j < count; ++j)
                {
                    res[i + j] = r[j];
                }
            }

            return res;
        }

        static double[] VectorAndFilter(double[] color, double[] mask)
        {
            int count = Point.Count;
            int N = color.Length;
            double[] res = new double[N];
            for (int i = 0; i < N; i += count)
            {
                Point p = new Point(color, i);
                Point m = new Point(mask, i);
                p = System.Numerics.Vector.Abs(p);
                Point r = p & m;
                for (int j = 0; j < count; ++j)
                {
                    res[i + j] = r[j];
                }
            }
            return res;
        }

        static double[] SimpleFilter(double[] color, double[] mask)
        {
            int N = color.Length;
            double[] res = new double[N];
            for (int i = 0; i < N; i += 1)
            {
                double c = Math.Abs(color[i]);
                res[i] = c * mask[i];
            }
            return res;
        }

        static double[] generateColor(int N, Random random)
        {
            double[] res = new double[N];
            for (int i = 0; i < N; ++i)
            {
                res[i] = NextFloat(random);
            }
            return res;
        }

        static double[] generateMask(int N, Random random)
        {
            double[] res = new double[N];
            for (int i = 0; i < N; ++i)
            {
                byte b = NextByte(random);
                if (b == 0)
                {
                    res[i] = 0;
                }
                else
                {
                    res[i] = 1;
                }
            }
            return res;
        }

        static double[] generateAndMask(double[] mask)
        {
            int N = mask.Length;
            double[] res = new double[N];
            for (int i = 0; i < N; ++i)
            {

                if (mask[i] == 0)
                {
                    res[i] = 0;
                }
                else
                {
                    res[i] = BitConverter.Int64BitsToDouble(-1);
                }
            }
            return res;
        }

        static bool checkEQ(double[] a, double[] b)
        {
            int N = a.Length;
            for (int i = 0; i < N; ++i)
            {
                if (Math.Abs(a[i] - b[i]) > EPS)
                {
                    return false;
                }
            }
            return true;
        }

        static int Main(string[] args)
        {
            Random random = new Random(13);
            int count = Point.Count;
            int N = count * 1000;
            double[] color = generateColor(N, random);
            double[] mask = generateMask(N, random);
            double[] andMask = generateAndMask(mask);

            double[] res = VectorFilter(color, mask);
            double[] check = SimpleFilter(color, mask);
            double[] andRes = VectorAndFilter(color, andMask);

            if (checkEQ(res, check) == false)
            {
                return 0;
            }
            if (checkEQ(res, andRes) == false)
            {
                return 0;
            }
            return 100;
        }
    }
}
