// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.X86
{

    public static class Avx512Verify
    {
        public static float GetExponent(float x)
        {
            int biasedExponent = GetBiasedExponent(x);
            return biasedExponent - 127;
        }

        public static double GetExponent(double x)
        {
            long biasedExponent = GetBiasedExponent(x);
            return biasedExponent - 1023;
        }

        public static float GetMantissa(float x)
        {
            float trailingSignificand = GetTrailingSignificand(x);
            return 1.0f + (trailingSignificand / (1 << 23));
        }

        public static double GetMantissa(double x)
        {
            double trailingSignificand = GetTrailingSignificand(x);
            return 1.0 + (trailingSignificand / (1L << 52));
        }

        public static bool ValidateReciprocal14(float actual, float value)
        {
            // Tests expect true on error

            float expected = 1.0f / value;
            float relativeError = RelativeError(expected, actual);

            return relativeError >= (1.0f / 16384); // 2^-14
        }

        public static bool ValidateReciprocal14(double actual, double value)
        {
            // Tests expect true on error

            double expected = 1.0 / value;
            double relativeError = RelativeError(expected, actual);

            return relativeError >= (1.0 / 16384); // 2^-14
        }

        public static bool ValidateReciprocalSqrt14(float actual, float value)
        {
            // Tests expect true on error

            float expected = 1.0f / float.Sqrt(value);
            float relativeError = RelativeError(expected, actual);

            return relativeError >= (1.0f / 16384); // 2^-14
        }

        public static bool ValidateReciprocalSqrt14(double actual, double value)
        {
            // Tests expect true on error

            double expected = 1.0 / double.Sqrt(value);
            double relativeError = RelativeError(expected, actual);

            return relativeError >= (1.0 / 16384); // 2^-14
        }

        private static int GetBiasedExponent(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);
            return (bits >>> 23) & 0x00FF;
        }

        private static long GetBiasedExponent(double x)
        {
            long bits = BitConverter.DoubleToInt64Bits(x);
            return (bits >>> 52) & 0x07FF;
        }

        private static int GetTrailingSignificand(float x)
        {
            int bits = BitConverter.SingleToInt32Bits(x);
            return bits & 0x007F_FFFF;
        }

        private static long GetTrailingSignificand(double x)
        {
            long bits = BitConverter.DoubleToInt64Bits(x);
            return bits & 0x000F_FFFF_FFFF_FFFF;
        }

        private static float RelativeError(float expected, float actual)
        {
            float absoluteError = float.Abs(expected - actual);
            return absoluteError / expected;
        }

        private static double RelativeError(double expected, double actual)
        {
            double absoluteError = double.Abs(expected - actual);
            return absoluteError / expected;
        }
    }
}
