// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tests
{
    public static class Util
    {
        static Random random = new Random();
        public static void SetRandomSeed(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// Generates random floats between 0 and 100.
        /// </summary>
        /// <param name="numValues">The number of values to generate</param>
        /// <returns>An array containing the random floats</returns>
        public static float[] GenerateRandomFloats(int numValues)
        {
            float[] values = new float[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = (float)(random.NextDouble() * 99 + 1);
            }
            return values;
        }

        /// <summary>
        /// Generates random ints between 0 and 99, inclusive.
        /// </summary>
        /// <param name="numValues">The number of values to generate</param>
        /// <returns>An array containing the random ints</returns>
        public static int[] GenerateRandomInts(int numValues)
        {
            int[] values = new int[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = random.Next(1, 100);
            }
            return values;
        }

        /// <summary>
        /// Generates random doubles between 0 and 100.
        /// </summary>
        /// <param name="numValues">The number of values to generate</param>
        /// <returns>An array containing the random doubles</returns>
        public static double[] GenerateRandomDoubles(int numValues)
        {
            double[] values = new double[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = random.NextDouble() * 99 + 1;
            }
            return values;
        }

        /// <summary>
        /// Generates random doubles between 1 and 100.
        /// </summary>
        /// <param name="numValues">The number of values to generate</param>
        /// <returns>An array containing the random doubles</returns>
        public static long[] GenerateRandomLongs(int numValues)
        {
            long[] values = new long[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = random.Next(1, 100) * (long.MaxValue / int.MaxValue);
            }
            return values;
        }

        public static T[] GenerateRandomValues<T>(int numValues, int min = 1, int max = 100) where T : struct
        {
            T[] values = new T[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = GenerateSingleValue<T>(min, max);
            }

            return values;
        }

        public static T GenerateSingleValue<T>(int min = 1, int max = 100) where T : struct
        {
            var randomRange = random.Next(min, max);
            T value = (T)(dynamic)randomRange;
            return value;
        }

        public static T Abs<T>(T value) where T : struct
        {
            Type[] unsignedTypes = new[] { typeof(Byte), typeof(UInt16), typeof(UInt32), typeof(UInt64) };
            if (unsignedTypes.Contains(typeof(T)))
            {
                return value;
            }

            dynamic dyn = (dynamic)value;
            var abs = Math.Abs(dyn);
            T ret = (T)abs;
            return ret;
        }

        public static T Sqrt<T>(T value) where T : struct
        {
            return (T)(dynamic)(Math.Sqrt((dynamic)value));
        }

        public static T Multiply<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left * right);
        }

        public static T Divide<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left / right);
        }

        public static T Add<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left + right);
        }

        public static T Subtract<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left - right);
        }

        public static T Xor<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left ^ right);
        }

        public static T AndNot<T>(T left, T right) where T : struct
        {
            return (T)((dynamic)left & ~(dynamic)right);
        }

        public static T OnesComplement<T>(T left) where T : struct
        {
            return (T)(~(dynamic)left);
        }
        public static float Clamp(float value, float min, float max)
        {
            return value > max ? max : value < min ? min : value;
        }

        public static T Zero<T>() where T : struct
        {
            return (T)(dynamic)0;
        }

        public static T One<T>() where T : struct
        {
            return (T)(dynamic)1;
        }

        public static bool GreaterThan<T>(T left, T right) where T : struct
        {
            var result = (dynamic)left > right;
            return (bool)result;
        }

        public static bool GreaterThanOrEqual<T>(T left, T right) where T : struct
        {
            var result = (dynamic)left >= right;
            return (bool)result;
        }

        public static bool LessThan<T>(T left, T right) where T : struct
        {
            var result = (dynamic)left < right;
            return (bool)result;
        }

        public static bool LessThanOrEqual<T>(T left, T right) where T : struct
        {
            var result = (dynamic)left <= right;
            return (bool)result;
        }

        public static bool AnyEqual<T>(T[] left, T[] right) where T : struct
        {
            for (int g = 0; g < left.Length; g++)
            {
                if (((IEquatable<T>)left[g]).Equals(right[g]))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AllEqual<T>(T[] left, T[] right) where T : struct
        {
            for (int g = 0; g < left.Length; g++)
            {
                if (!((IEquatable<T>)left[g]).Equals(right[g]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}