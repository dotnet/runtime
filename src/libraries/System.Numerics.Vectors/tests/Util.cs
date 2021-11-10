// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Tests
{
    public static class Util
    {
        private static Random s_random = new Random();
        public static void SetRandomSeed(int seed)
        {
            s_random = new Random(seed);
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
                values[g] = (float)(s_random.NextDouble() * 99 + 1);
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
                values[g] = s_random.Next(1, 100);
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
                values[g] = s_random.NextDouble() * 99 + 1;
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
                values[g] = s_random.Next(1, 100) * (long.MaxValue / int.MaxValue);
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
            var randomRange = s_random.Next(min, max);
            T value = unchecked((T)(dynamic)randomRange);
            return value;
        }

        public static T Abs<T>(T value) where T : struct
        {
            // unsigned types
            if      (value is byte)   return value;
            else if (value is ushort) return value;
            else if (value is uint)   return value;
            else if (value is ulong)  return value;
            // signed types
            else if (value is short)  return (T)(ValueType)(short)  ( Math.Abs((short) (ValueType)value) );
            else if (value is int)    return (T)(ValueType)(int)    ( Math.Abs((int)   (ValueType)value) );
            else if (value is long)   return (T)(ValueType)(long)   ( Math.Abs((long)  (ValueType)value) );
            else if (value is sbyte)  return (T)(ValueType)(sbyte)  ( Math.Abs((sbyte) (ValueType)value) );
            else if (value is float)  return (T)(ValueType)(float)  ( Math.Abs((float) (ValueType)value) );
            else if (value is double) return (T)(ValueType)(double) ( Math.Abs((double)(ValueType)value) );
            else throw new NotImplementedException();
        }

        public static T Sqrt<T>(T value) where T : struct
        {
            unchecked
            {
                if      (value is short)  return (T)(ValueType)(short)  ( Math.Sqrt((short) (ValueType)value) );
                else if (value is int)    return (T)(ValueType)(int)    ( Math.Sqrt((int)   (ValueType)value) );
                else if (value is long)   return (T)(ValueType)(long)   ( Math.Sqrt((long)  (ValueType)value) );
                else if (value is ushort) return (T)(ValueType)(ushort) ( Math.Sqrt((ushort)(ValueType)value) );
                else if (value is uint)   return (T)(ValueType)(uint)   ( Math.Sqrt((uint)  (ValueType)value) );
                else if (value is ulong)  return (T)(ValueType)(ulong)  ( Math.Sqrt((ulong) (ValueType)value) );
                else if (value is byte)   return (T)(ValueType)(byte)   ( Math.Sqrt((byte)  (ValueType)value) );
                else if (value is sbyte)  return (T)(ValueType)(sbyte)  ( Math.Sqrt((sbyte) (ValueType)value) );
                else if (value is float)  return (T)(ValueType)(float)  ( Math.Sqrt((float) (ValueType)value) );
                else if (value is double) return (T)(ValueType)(double) ( Math.Sqrt((double)(ValueType)value) );
                else throw new NotImplementedException();
            }
        }

        public static T Multiply<T>(T left, T right) where T : struct
        {
            unchecked
            {
                if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left * (short) (ValueType)right );
                else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left * (int)   (ValueType)right );
                else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left * (long)  (ValueType)right );
                else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left * (ushort)(ValueType)right );
                else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left * (uint)  (ValueType)right );
                else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left * (ulong) (ValueType)right );
                else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left * (byte)  (ValueType)right );
                else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left * (sbyte) (ValueType)right );
                else if (left is float)  return (T)(ValueType)(float)  ( (float) (ValueType)left * (float) (ValueType)right );
                else if (left is double) return (T)(ValueType)(double) ( (double)(ValueType)left * (double)(ValueType)right );
                else throw new NotImplementedException();
            }
        }

        public static T Divide<T>(T left, T right) where T : struct
        {
            if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left / (short) (ValueType)right );
            else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left / (int)   (ValueType)right );
            else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left / (long)  (ValueType)right );
            else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left / (ushort)(ValueType)right );
            else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left / (uint)  (ValueType)right );
            else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left / (ulong) (ValueType)right );
            else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left / (byte)  (ValueType)right );
            else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left / (sbyte) (ValueType)right );
            else if (left is float)  return (T)(ValueType)(float)  ( (float) (ValueType)left / (float) (ValueType)right );
            else if (left is double) return (T)(ValueType)(double) ( (double)(ValueType)left / (double)(ValueType)right );
            else throw new NotImplementedException();
        }

        public static T Add<T>(T left, T right) where T : struct
        {
            unchecked
            {
                if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left + (short) (ValueType)right );
                else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left + (int)   (ValueType)right );
                else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left + (long)  (ValueType)right );
                else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left + (ushort)(ValueType)right );
                else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left + (uint)  (ValueType)right );
                else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left + (ulong) (ValueType)right );
                else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left + (byte)  (ValueType)right );
                else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left + (sbyte) (ValueType)right );
                else if (left is float)  return (T)(ValueType)(float)  ( (float) (ValueType)left + (float) (ValueType)right );
                else if (left is double) return (T)(ValueType)(double) ( (double)(ValueType)left + (double)(ValueType)right );
                else throw new NotImplementedException();
            }
        }

        public static T Subtract<T>(T left, T right) where T : struct
        {
            unchecked
            {
                if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left - (short) (ValueType)right );
                else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left - (int)   (ValueType)right );
                else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left - (long)  (ValueType)right );
                else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left - (ushort)(ValueType)right );
                else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left - (uint)  (ValueType)right );
                else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left - (ulong) (ValueType)right );
                else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left - (byte)  (ValueType)right );
                else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left - (sbyte) (ValueType)right );
                else if (left is float)  return (T)(ValueType)(float)  ( (float) (ValueType)left - (float) (ValueType)right );
                else if (left is double) return (T)(ValueType)(double) ( (double)(ValueType)left - (double)(ValueType)right );
                else throw new NotImplementedException();
            }
        }

        public static T Xor<T>(T left, T right) where T : struct
        {
            if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left ^ (short) (ValueType)right );
            else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left ^ (int)   (ValueType)right );
            else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left ^ (long)  (ValueType)right );
            else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left ^ (ushort)(ValueType)right );
            else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left ^ (uint)  (ValueType)right );
            else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left ^ (ulong) (ValueType)right );
            else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left ^ (byte)  (ValueType)right );
            else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left ^ (sbyte) (ValueType)right );
            else throw new NotImplementedException();
        }

        public static T AndNot<T>(T left, T right) where T : struct
        {
            if      (left is short)  return (T)(ValueType)(short)  ( (short) (ValueType)left & ~(short) (ValueType)right );
            else if (left is int)    return (T)(ValueType)(int)    ( (int)   (ValueType)left & ~(int)   (ValueType)right );
            else if (left is long)   return (T)(ValueType)(long)   ( (long)  (ValueType)left & ~(long)  (ValueType)right );
            else if (left is ushort) return (T)(ValueType)(ushort) ( (ushort)(ValueType)left & ~(ushort)(ValueType)right );
            else if (left is uint)   return (T)(ValueType)(uint)   ( (uint)  (ValueType)left & ~(uint)  (ValueType)right );
            else if (left is ulong)  return (T)(ValueType)(ulong)  ( (ulong) (ValueType)left & ~(ulong) (ValueType)right );
            else if (left is byte)   return (T)(ValueType)(byte)   ( (byte)  (ValueType)left & ~(byte)  (ValueType)right );
            else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( (sbyte) (ValueType)left & ~(sbyte) (ValueType)right );
            else throw new NotImplementedException();
        }

        public static T OnesComplement<T>(T left) where T : struct
        {
            unchecked
            {
                if      (left is short)  return (T)(ValueType)(short)  ( ~(short) (ValueType)left );
                else if (left is int)    return (T)(ValueType)(int)    ( ~(int)   (ValueType)left );
                else if (left is long)   return (T)(ValueType)(long)   ( ~(long)  (ValueType)left );
                else if (left is ushort) return (T)(ValueType)(ushort) ( ~(ushort)(ValueType)left );
                else if (left is uint)   return (T)(ValueType)(uint)   ( ~(uint)  (ValueType)left );
                else if (left is ulong)  return (T)(ValueType)(ulong)  ( ~(ulong) (ValueType)left );
                else if (left is byte)   return (T)(ValueType)(byte)   ( ~(byte)  (ValueType)left );
                else if (left is sbyte)  return (T)(ValueType)(sbyte)  ( ~(sbyte) (ValueType)left );
                else throw new NotImplementedException();
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            return value > max ? max : value < min ? min : value;
        }

        public static T Zero<T>() where T : struct
        {
            if      (typeof(T) == typeof(short))  return  (T)(ValueType)(short)  0;
            else if (typeof(T) == typeof(int))    return  (T)(ValueType)(int)    0;
            else if (typeof(T) == typeof(long))   return  (T)(ValueType)(long)   0;
            else if (typeof(T) == typeof(ushort)) return  (T)(ValueType)(ushort) 0;
            else if (typeof(T) == typeof(uint))   return  (T)(ValueType)(uint)   0;
            else if (typeof(T) == typeof(ulong))  return  (T)(ValueType)(ulong)  0;
            else if (typeof(T) == typeof(byte))   return  (T)(ValueType)(byte)   0;
            else if (typeof(T) == typeof(sbyte))  return  (T)(ValueType)(sbyte)  0;
            else if (typeof(T) == typeof(float))  return  (T)(ValueType)(float)  0;
            else if (typeof(T) == typeof(double)) return  (T)(ValueType)(double) 0;
            else throw new NotImplementedException();
        }

        public static T One<T>() where T : struct
        {
            if      (typeof(T) == typeof(short))  return  (T)(ValueType)(short)  1;
            else if (typeof(T) == typeof(int))    return  (T)(ValueType)(int)    1;
            else if (typeof(T) == typeof(long))   return  (T)(ValueType)(long)   1;
            else if (typeof(T) == typeof(ushort)) return  (T)(ValueType)(ushort) 1;
            else if (typeof(T) == typeof(uint))   return  (T)(ValueType)(uint)   1;
            else if (typeof(T) == typeof(ulong))  return  (T)(ValueType)(ulong)  1;
            else if (typeof(T) == typeof(byte))   return  (T)(ValueType)(byte)   1;
            else if (typeof(T) == typeof(sbyte))  return  (T)(ValueType)(sbyte)  1;
            else if (typeof(T) == typeof(float))  return  (T)(ValueType)(float)  1;
            else if (typeof(T) == typeof(double)) return  (T)(ValueType)(double) 1;
            else throw new NotImplementedException();
        }

        public static bool GreaterThan<T>(T left, T right) where T : struct
        {
            if      (left is short)  return  (short)(ValueType)  left > (short)(ValueType)  right;
            else if (left is int)    return  (int)(ValueType)    left > (int)(ValueType)    right;
            else if (left is long)   return  (long)(ValueType)   left > (long)(ValueType)   right;
            else if (left is ushort) return  (ushort)(ValueType) left > (ushort)(ValueType) right;
            else if (left is uint)   return  (uint)(ValueType)   left > (uint)(ValueType)   right;
            else if (left is ulong)  return  (ulong)(ValueType)  left > (ulong)(ValueType)  right;
            else if (left is byte)   return  (byte)(ValueType)   left > (byte)(ValueType)   right;
            else if (left is sbyte)  return  (sbyte)(ValueType)  left > (sbyte)(ValueType)  right;
            else if (left is float)  return  (float)(ValueType)  left > (float)(ValueType)  right;
            else if (left is double) return  (double)(ValueType) left > (double)(ValueType) right;
            else throw new NotImplementedException();
        }

        public static bool GreaterThanOrEqual<T>(T left, T right) where T : struct
        {
            if      (left is short)  return  (short)(ValueType)  left >= (short)(ValueType)  right;
            else if (left is int)    return  (int)(ValueType)    left >= (int)(ValueType)    right;
            else if (left is long)   return  (long)(ValueType)   left >= (long)(ValueType)   right;
            else if (left is ushort) return  (ushort)(ValueType) left >= (ushort)(ValueType) right;
            else if (left is uint)   return  (uint)(ValueType)   left >= (uint)(ValueType)   right;
            else if (left is ulong)  return  (ulong)(ValueType)  left >= (ulong)(ValueType)  right;
            else if (left is byte)   return  (byte)(ValueType)   left >= (byte)(ValueType)   right;
            else if (left is sbyte)  return  (sbyte)(ValueType)  left >= (sbyte)(ValueType)  right;
            else if (left is float)  return  (float)(ValueType)  left >= (float)(ValueType)  right;
            else if (left is double) return  (double)(ValueType) left >= (double)(ValueType) right;
            else throw new NotImplementedException();
        }

        public static bool LessThan<T>(T left, T right) where T : struct
        {
            if      (left is short)  return  (short)(ValueType)  left < (short)(ValueType)  right;
            else if (left is int)    return  (int)(ValueType)    left < (int)(ValueType)    right;
            else if (left is long)   return  (long)(ValueType)   left < (long)(ValueType)   right;
            else if (left is ushort) return  (ushort)(ValueType) left < (ushort)(ValueType) right;
            else if (left is uint)   return  (uint)(ValueType)   left < (uint)(ValueType)   right;
            else if (left is ulong)  return  (ulong)(ValueType)  left < (ulong)(ValueType)  right;
            else if (left is byte)   return  (byte)(ValueType)   left < (byte)(ValueType)   right;
            else if (left is sbyte)  return  (sbyte)(ValueType)  left < (sbyte)(ValueType)  right;
            else if (left is float)  return  (float)(ValueType)  left < (float)(ValueType)  right;
            else if (left is double) return  (double)(ValueType) left < (double)(ValueType) right;
            else throw new NotImplementedException();
        }

        public static bool LessThanOrEqual<T>(T left, T right) where T : struct
        {
            if      (left is short)  return  (short)(ValueType)  left <= (short)(ValueType)  right;
            else if (left is int)    return  (int)(ValueType)    left <= (int)(ValueType)    right;
            else if (left is long)   return  (long)(ValueType)   left <= (long)(ValueType)   right;
            else if (left is ushort) return  (ushort)(ValueType) left <= (ushort)(ValueType) right;
            else if (left is uint)   return  (uint)(ValueType)   left <= (uint)(ValueType)   right;
            else if (left is ulong)  return  (ulong)(ValueType)  left <= (ulong)(ValueType)  right;
            else if (left is byte)   return  (byte)(ValueType)   left <= (byte)(ValueType)   right;
            else if (left is sbyte)  return  (sbyte)(ValueType)  left <= (sbyte)(ValueType)  right;
            else if (left is float)  return  (float)(ValueType)  left <= (float)(ValueType)  right;
            else if (left is double) return  (double)(ValueType) left <= (double)(ValueType) right;
            else throw new NotImplementedException();
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
