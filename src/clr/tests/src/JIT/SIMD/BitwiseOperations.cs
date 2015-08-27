using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector2;

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

        static int TestDouble()
        {
            Random random = new Random(11);
            double[] arr1 = new double[] { NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random) };
            double[] arr2 = new double[] { NextFloat(random), NextFloat(random), NextFloat(random), NextFloat(random) };
            var a = new System.Numerics.Vector<double>(arr1);
            var b = new System.Numerics.Vector<double>(arr2);
            var xorR = a ^ b;
            var andR = a & b;
            var orR = a | b;
            int Count = System.Numerics.Vector<double>.Count;
            for (int i = 0; i < Count; ++i)
            {
                Int64 f = BitConverter.DoubleToInt64Bits(a[i]);
                Int64 s = BitConverter.DoubleToInt64Bits(b[i]);
                Int64 r = f ^ s;
                double d = BitConverter.Int64BitsToDouble(r);
                if (xorR[i] != d)
                {
                    return 0;
                }
                r = f & s;
                d = BitConverter.Int64BitsToDouble(r);
                if (andR[i] != d)
                {
                    return 0;
                }
                r = f | s;
                d = BitConverter.Int64BitsToDouble(r);
                if (orR[i] != d && d == d)
                {
                    return 0;
                }
            }
            return 100;
        }

        static byte NextByte(Random random)
        {
            return (byte)random.Next(0, 255);
        }

        static byte[] GenerateByteArray(int size, Random random)
        {
            byte[] arr = new byte[size];
            for (int i = 0; i < size; ++i)
            {
                arr[i] = NextByte(random);
            }
            return arr;
        }

        static int TestBool()
        {
            Random random = new Random(13);
            byte[] arr1 = GenerateByteArray(64, random);
            byte[] arr2 = GenerateByteArray(64, random);
            var a = new System.Numerics.Vector<byte>(arr1);
            var b = new System.Numerics.Vector<byte>(arr2);

            var xorR = a ^ b;
            var andR = a & b;
            var orR = a | b;
            int Count = System.Numerics.Vector<byte>.Count;
            for (int i = 0; i < Count; ++i)
            {
                int d = a[i] ^ b[i];
                if (xorR[i] != d)
                {
                    return 0;
                }
                d = a[i] & b[i];
                if (andR[i] != d)
                {
                    return 0;
                }
                d = a[i] | b[i];
                if (orR[i] != d)
                {
                    return 0;
                }
            }
            return 100;
        }

        static int Main(string[] args)
        {
            if (TestDouble() != 100)
            {
                return 0;
            }
            if (TestBool() != 100)
            {
                return 0;
            }

            return 100;
        }
    }
}
