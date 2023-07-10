// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.X86
{

    public static class Avx512Verify
    {
        public static bool ValidateFixup(double actual, double x, double y, long z)
        {
            // Tests expect true on error
            double expected = Fixup<double>(x, y, (int)(z));

            if (BitConverter.DoubleToInt64Bits(actual) == BitConverter.DoubleToInt64Bits(expected))
            {
                return false;
            }

            // The real fixup returns specific NaNs, but we don't need to validate for this test
            return !(double.IsNaN(actual) && double.IsNaN(expected));
        }

        public static bool ValidateFixup(float actual, float x, float y, int z)
        {
            // Tests expect true on error
            float expected = Fixup<float>(x, y, (int)(z));

            if (BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(expected))
            {
                return false;
            }

            // The real fixup returns specific NaNs, but we don't need to validate for this test
            return !(float.IsNaN(actual) && float.IsNaN(expected));
        }

        public static TInteger DetectConflicts<TInteger>(TInteger[] firstOp, int i)
            where TInteger : IBinaryInteger<TInteger>
        {
            TInteger result = TInteger.Zero;

            for (int n = 0; n < i - 1; n++)
            {
                if (firstOp[n] == firstOp[i])
                {
                    result |= (TInteger.One << n);
                }
            }

            return result;
        }

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

        public static TFloat Reduce<TFloat>(TFloat x, int m)
            where TFloat : IFloatingPointIeee754<TFloat>
        {
            return x - TFloat.Round(TFloat.ScaleB(TFloat.One, m) * x) * TFloat.ScaleB(TFloat.One, -m);
        }

        public static T Shuffle2x128<T>(T[] left, T[] right, byte control, int i)
            where T : struct
        {
            int partsPerV128 = left.Length / 2;
            int offset = i % partsPerV128;
            int selected = (control >> (i / partsPerV128)) & 0b1;

            if (i < (left.Length / 2))
            {
                return left[(selected * partsPerV128) + offset];
            }
            else
            {
                return right[(selected * partsPerV128) + offset];
            }
        }

        public static T Shuffle4x128<T>(T[] left, T[] right, byte control, int i)
            where T : struct
        {
            int partsPerV128 = left.Length / 4;
            int offset = i % partsPerV128;
            int selected = (control >> (i / partsPerV128 * 2)) & 0b11;

            if (i < (left.Length / 2))
            {
                return left[(selected * partsPerV128) + offset];
            }
            else
            {
                return right[(selected * partsPerV128) + offset];
            }
        }

        public static ushort SumAbsoluteDifferencesInBlock32(byte[] left, byte[] right, byte control, int i)
        {
            int a = i % 4;
            int b = (a < 2) ? 0 : 4;
            int c = (i / 4) * 8;

            ushort result = 0;

            for (int n = 0; n < 4; n++)
            {
                int tmp = int.Abs(left[c + n + b] - right[c + ((control >> (n * 2)) & 3) + a]);
                result += (ushort)(tmp);
            }

            return result;
        }

        public static bool ValidateReciprocal14<TFloat>(TFloat actual, TFloat value)
            where TFloat : IFloatingPointIeee754<TFloat>
        {
            // Tests expect true on error

            TFloat expected = TFloat.One / value;
            TFloat relativeError = RelativeError(expected, actual);

            return relativeError >= (TFloat.One / TFloat.CreateSaturating(16384)); // 2^-14
        }

        public static bool ValidateReciprocalSqrt14<TFloat>(TFloat actual, TFloat value)
            where TFloat : IFloatingPointIeee754<TFloat>
        {
            // Tests expect true on error

            TFloat expected = TFloat.One/ TFloat.Sqrt(value);
            TFloat relativeError = RelativeError(expected, actual);

            return relativeError >= (TFloat.One / TFloat.CreateSaturating(16384)); // 2^-14
        }

        private static TFloat Fixup<TFloat>(TFloat x, TFloat y, int z)
            where TFloat : IFloatingPointIeee754<TFloat>, IMinMaxValue<TFloat>
        {
            int tokenType = GetTokenType(y);
            int tokenResponse = GetTokenResponse(tokenType, z);

            switch (tokenResponse)
            {
                case 0: return x;
                case 1: return y;
                case 2: return TFloat.NaN;
                case 3: return TFloat.NaN;
                case 4: return TFloat.NegativeInfinity;
                case 5: return TFloat.PositiveInfinity;
                case 6: return TFloat.CopySign(TFloat.PositiveInfinity, y);
                case 7: return TFloat.NegativeZero;
                case 8: return TFloat.Zero;
                case 9: return -TFloat.One;
                case 10: return TFloat.One;
                case 11: return TFloat.CreateSaturating(0.5);
                case 12: return TFloat.CreateSaturating(90);
                case 13: return TFloat.Pi / TFloat.CreateSaturating(2);
                case 14: return TFloat.MaxValue;
                case 15: return TFloat.MinValue;
                default: throw new Exception($"Unexpected tokenResponse ({tokenResponse}) for ({x}, {y}, {z})");
            }
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

        private static int GetTokenResponse(int tokenType, int z)
        {
            return (z >> (4 * tokenType)) & 0xF;
        }

        private static int GetTokenType<TFloat>(TFloat x)
            where TFloat : IFloatingPointIeee754<TFloat>
        {
            if (TFloat.IsNaN(x))
            {
                return 0;
            }
            else if (TFloat.IsZero(x))
            {
                return 2;
            }
            else if (x == TFloat.One)
            {
                return 3;
            }
            else if (TFloat.IsInfinity(x))
            {
                return TFloat.IsNegative(x) ? 4 : 5;
            }
            else
            {
                return TFloat.IsNegative(x) ? 6 : 7;
            }
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

        private static TFloat RelativeError<TFloat>(TFloat expected, TFloat actual)
            where TFloat : IFloatingPointIeee754<TFloat>
        {
            TFloat absoluteError = TFloat.Abs(expected - actual);
            return absoluteError / expected;
        }
    }
}
