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
    }
}
