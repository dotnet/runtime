// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System
{
    /// <summary>
    /// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
    /// </summary>
    public static partial class MathF
    {
        public const float E = 2.71828183f;

        public const float PI = 3.14159265f;

        public const float Tau = 6.283185307f;

        private const int maxRoundingDigits = 6;

        // This table is required for the Round function which can specify the number of digits to round to
        private static ReadOnlySpan<float> RoundPower10Single =>
        [
            1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f
        ];

        private const float singleRoundLimit = 1e8f;

        private const float SCALEB_C1 = 1.7014118E+38f; // 0x1p127f

        private const float SCALEB_C2 = 1.1754944E-38f; // 0x1p-126f

        private const float SCALEB_C3 = 16777216f; // 0x1p24f

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float x)
        {
            return Math.Abs(x);
        }

        public static float BitDecrement(float x)
        {
            uint bits = BitConverter.SingleToUInt32Bits(x);

            if (!float.IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns MaxValue
                return (bits == float.PositiveInfinityBits) ? float.MaxValue : x;
            }

            if (bits == float.PositiveZeroBits)
            {
                // +0.0 returns -float.Epsilon
                return -float.Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            if (float.IsNegative(x))
            {
                bits += 1;
            }
            else
            {
                bits -= 1;
            }
            return BitConverter.UInt32BitsToSingle(bits);
        }

        public static float BitIncrement(float x)
        {
            uint bits = BitConverter.SingleToUInt32Bits(x);

            if (!float.IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns MinValue
                // +Infinity returns +Infinity
                return (bits == float.NegativeInfinityBits) ? float.MinValue : x;
            }

            if (bits == float.NegativeZeroBits)
            {
                // -0.0 returns Epsilon
                return float.Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            if (float.IsNegative(x))
            {
                bits -= 1;
            }
            else
            {
                bits += 1;
            }
            return BitConverter.UInt32BitsToSingle(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CopySign(float x, float y)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                return Vector128.ConditionalSelect(Vector128.CreateScalarUnsafe(-0.0f), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return SoftwareFallback(x, y);
            }

            static float SoftwareFallback(float x, float y)
            {
                // This method is required to work for all inputs,
                // including NaN, so we operate on the raw bits.
                uint xbits = BitConverter.SingleToUInt32Bits(x);
                uint ybits = BitConverter.SingleToUInt32Bits(y);

                // Remove the sign from x, and remove everything but the sign from y
                // Then, simply OR them to get the correct sign
                return BitConverter.UInt32BitsToSingle((xbits & ~float.SignMask) | (ybits & float.SignMask));
            }
        }

        public static float IEEERemainder(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            float regularMod = x % y;

            if (float.IsNaN(regularMod))
            {
                return float.NaN;
            }

            if ((regularMod == 0) && float.IsNegative(x))
            {
                return float.NegativeZero;
            }

            float alternativeResult = (regularMod - (Abs(y) * Sign(x)));

            if (Abs(alternativeResult) == Abs(regularMod))
            {
                float divisionResult = x / y;
                float roundedResult = Round(divisionResult);

                if (Abs(roundedResult) > Abs(divisionResult))
                {
                    return alternativeResult;
                }
                else
                {
                    return regularMod;
                }
            }

            if (Abs(alternativeResult) < Abs(regularMod))
            {
                return alternativeResult;
            }
            else
            {
                return regularMod;
            }
        }

        public static int ILogB(float x)
        {
            // This code is based on `ilogbf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (!float.IsNormal(x)) // x is zero, subnormal, infinity, or NaN
            {
                if (float.IsZero(x))
                {
                    return int.MinValue;
                }

                if (!float.IsFinite(x)) // infinity or NaN
                {
                    return int.MaxValue;
                }

                Debug.Assert(float.IsSubnormal(x));
                return float.MinExponent - (BitOperations.TrailingZeroCount(x.TrailingSignificand) - float.BiasedExponentLength);
            }

            return x.Exponent;
        }

        public static float Log(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            if (y == 1)
            {
                return float.NaN;
            }

            if ((x != 1) && ((y == 0) || float.IsPositiveInfinity(y)))
            {
                return float.NaN;
            }

            return Log(x) / Log(y);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float x, float y)
        {
            return Math.Max(x, y);
        }

        [Intrinsic]
        public static float MaxMagnitude(float x, float y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a greater magnitude.
            // It treats +0 as greater than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

            if ((ax > ay) || float.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return float.IsNegative(x) ? y : x;
            }

            return y;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float x, float y)
        {
            return Math.Min(x, y);
        }

        [Intrinsic]
        public static float MinMagnitude(float x, float y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a lesser magnitude.
            // It treats +0 as greater than -0 as per the specification.

            float ax = Abs(x);
            float ay = Abs(y);

            if ((ax < ay) || float.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return float.IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <summary>Returns an estimate of the reciprocal of a specified number.</summary>
        /// <param name="x">The number whose reciprocal is to be estimated.</param>
        /// <returns>An estimate of the reciprocal of <paramref name="x" />.</returns>
        /// <remarks>
        ///    <para>On x86/x64 hardware this may use the <c>RCPSS</c> instruction which has a maximum relative error of <c>1.5 * 2^-12</c>.</para>
        ///    <para>On ARM64 hardware this may use the <c>FRECPE</c> instruction which performs a single Newton-Raphson iteration.</para>
        ///    <para>On hardware without specialized support, this may just return <c>1.0 / x</c>.</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReciprocalEstimate(float x)
        {
            if (Sse.IsSupported)
            {
                return Sse.ReciprocalScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ReciprocalEstimateScalar(Vector64.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return 1.0f / x;
            }
        }

        /// <summary>Returns an estimate of the reciprocal square root of a specified number.</summary>
        /// <param name="x">The number whose reciprocal square root is to be estimated.</param>
        /// <returns>An estimate of the reciprocal square root <paramref name="x" />.</returns>
        /// <remarks>
        ///    <para>On x86/x64 hardware this may use the <c>RSQRTSS</c> instruction which has a maximum relative error of <c>1.5 * 2^-12</c>.</para>
        ///    <para>On ARM64 hardware this may use the <c>FRSQRTE</c> instruction which performs a single Newton-Raphson iteration.</para>
        ///    <para>On hardware without specialized support, this may just return <c>1.0 / Sqrt(x)</c>.</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReciprocalSqrtEstimate(float x)
        {
            if (Sse.IsSupported)
            {
                return Sse.ReciprocalSqrtScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ReciprocalSquareRootEstimateScalar(Vector64.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return 1.0f / Sqrt(x);
            }
        }

        [Intrinsic]
        public static float Round(float x)
        {
            // ************************************************************************************
            // IMPORTANT: Do not change this implementation without also updating Math.Round(float),
            //            FloatingPointUtils::round(double), and FloatingPointUtils::round(float)
            // ************************************************************************************

            // This code is based on `nearbyintf` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // This represents the boundary at which point we can only represent whole integers
            const float IntegerBoundary = 8388608.0f; // 2^23

            if (Abs(x) >= IntegerBoundary)
            {
                // Values above this boundary don't have a fractional
                // portion and so we can simply return them as-is.
                return x;
            }

            // Otherwise, since floating-point takes the inputs, performs
            // the computation as if to infinite precision and unbounded
            // range, and then rounds to the nearest representable result
            // using the current rounding mode, we can rely on this to
            // cheaply round.
            //
            // In particular, .NET doesn't support changing the rounding
            // mode and defaults to "round to nearest, ties to even", thus
            // by adding the original value to the IntegerBoundary we get
            // an exactly represented whole integer that is precisely the
            // IntegerBoundary greater in magnitude than the answer we want.
            //
            // We can then simply remove that offset to get the correct answer,
            // noting that we also need to copy back the original sign to
            // correctly handle -0.0

            float temp = CopySign(IntegerBoundary, x);
            return CopySign((x + temp) - temp, x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float x, int digits)
        {
            return Round(x, digits, MidpointRounding.ToEven);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float x, MidpointRounding mode)
        {
            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                case MidpointRounding.AwayFromZero:
                    // For ARM/ARM64 we can lower it down to a single instruction FRINTA
                    if (AdvSimd.IsSupported)
                        return AdvSimd.RoundAwayFromZeroScalar(Vector64.CreateScalarUnsafe(x)).ToScalar();
                    // For other platforms we use a fast managed implementation
                    // manually fold BitDecrement(0.5)
                    return Truncate(x + CopySign(0.49999997f, x));

                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value with an even least significant digit
                case MidpointRounding.ToEven:
                    return Round(x);
                // Directed rounding: Round to the nearest value, toward to zero
                case MidpointRounding.ToZero:
                    return Truncate(x);
                // Directed Rounding: Round down to the next value, toward negative infinity
                case MidpointRounding.ToNegativeInfinity:
                    return Floor(x);
                // Directed rounding: Round up to the next value, toward positive infinity
                case MidpointRounding.ToPositiveInfinity:
                    return Ceiling(x);

                default:
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float x, int digits, MidpointRounding mode)
        {
            if ((uint)digits > maxRoundingDigits)
            {
                ThrowHelper.ThrowArgumentOutOfRange_RoundingDigits_MathF(nameof(digits));
            }

            if (Abs(x) < singleRoundLimit)
            {
                float power10 = RoundPower10Single[digits];
                x = Round(x * power10, mode) / power10;
            }

            return x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(float x)
        {
            return Math.Sign(x);
        }

        [Intrinsic]
        public static unsafe float Truncate(float x)
        {
            ModF(x, &x);
            return x;
        }

        public static float ScaleB(float x, int n)
        {
            // Implementation based on https://git.musl-libc.org/cgit/musl/tree/src/math/scalblnf.c
            //
            // Performs the calculation x * 2^n efficiently. It constructs a float from 2^n by building
            // the correct biased exponent. If n is greater than the maximum exponent (127) or less than
            // the minimum exponent (-126), adjust x and n to compute correct result.

            float y = x;
            if (n > 127)
            {
                y *= SCALEB_C1;
                n -= 127;
                if (n > 127)
                {
                    y *= SCALEB_C1;
                    n -= 127;
                    if (n > 127)
                    {
                        n = 127;
                    }
                }
            }
            else if (n < -126)
            {
                y *= SCALEB_C2 * SCALEB_C3;
                n += 126 - 24;
                if (n < -126)
                {
                    y *= SCALEB_C2 * SCALEB_C3;
                    n += 126 - 24;
                    if (n < -126)
                    {
                        n = -126;
                    }
                }
            }

            float u = BitConverter.Int32BitsToSingle(((int)(0x7f + n) << 23));
            return y * u;
        }
    }
}
