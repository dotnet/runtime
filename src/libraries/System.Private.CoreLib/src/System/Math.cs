// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;

namespace System
{
    /// <summary>
    /// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
    /// </summary>
    public static partial class Math
    {
        public const double E = 2.7182818284590452354;

        public const double PI = 3.14159265358979323846;

        public const double Tau = 6.283185307179586476925;

        private const int maxRoundingDigits = 15;

        private const double doubleRoundLimit = 1e16d;

        // This table is required for the Round function which can specify the number of digits to round to
        private static ReadOnlySpan<double> RoundPower10Double =>
        [
            1E0, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8,
            1E9, 1E10, 1E11, 1E12, 1E13, 1E14, 1E15
        ];

        private const double SCALEB_C1 = 8.98846567431158E+307; // 0x1p1023

        private const double SCALEB_C2 = 2.2250738585072014E-308; // 0x1p-1022

        private const double SCALEB_C3 = 9007199254740992; // 0x1p53

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Abs(short value)
        {
            if (value < 0)
            {
                value = (short)-value;
                if (value < 0)
                {
                    ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Abs(int value)
        {
            if (value < 0)
            {
                value = -value;
                if (value < 0)
                {
                    ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Abs(long value)
        {
            if (value < 0)
            {
                value = -value;
                if (value < 0)
                {
                    ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        /// <summary>Returns the absolute value of a native signed integer.</summary>
        /// <param name="value">A number that is greater than <see cref="IntPtr.MinValue" />, but less than or equal to <see cref="IntPtr.MaxValue" />.</param>
        /// <returns>A native signed integer, x, such that 0 \u2264 x \u2264 <see cref="IntPtr.MaxValue" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Abs(nint value)
        {
            if (value < 0)
            {
                value = -value;
                if (value < 0)
                {
                    ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static sbyte Abs(sbyte value)
        {
            if (value < 0)
            {
                value = (sbyte)-value;
                if (value < 0)
                {
                    ThrowNegateTwosCompOverflow();
                }
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Abs(decimal value)
        {
            return decimal.Abs(value);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Abs(double value)
        {
            const ulong mask = 0x7FFFFFFFFFFFFFFF;
            ulong raw = BitConverter.DoubleToUInt64Bits(value);

            return BitConverter.UInt64BitsToDouble(raw & mask);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float value)
        {
            const uint mask = 0x7FFFFFFF;
            uint raw = BitConverter.SingleToUInt32Bits(value);

            return BitConverter.UInt32BitsToSingle(raw & mask);
        }

        [DoesNotReturn]
        [StackTraceHidden]
        internal static void ThrowNegateTwosCompOverflow()
        {
            throw new OverflowException(SR.Overflow_NegateTwosCompNum);
        }

        internal static ulong BigMul(uint a, uint b)
        {
            return ((ulong)a) * b;
        }

        public static long BigMul(int a, int b)
        {
            return ((long)a) * b;
        }

        /// <summary>Produces the full product of two unsigned 64-bit numbers.</summary>
        /// <param name="a">The first number to multiply.</param>
        /// <param name="b">The second number to multiply.</param>
        /// <param name="low">The low 64-bit of the product of the specified numbers.</param>
        /// <returns>The high 64-bit of the product of the specified numbers.</returns>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ulong BigMul(ulong a, ulong b, out ulong low)
        {
            if (Bmi2.X64.IsSupported)
            {
                ulong tmp;
                ulong high = Bmi2.X64.MultiplyNoFlags(a, b, &tmp);
                low = tmp;
                return high;
            }
            else if (ArmBase.Arm64.IsSupported)
            {
                low = a * b;
                return ArmBase.Arm64.MultiplyHigh(a, b);
            }

            return SoftwareFallback(a, b, out low);

            static ulong SoftwareFallback(ulong a, ulong b, out ulong low)
            {
                // Adaptation of algorithm for multiplication
                // of 32-bit unsigned integers described
                // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
                // Basically, it's an optimized version of FOIL method applied to
                // low and high dwords of each operand

                // Use 32-bit uints to optimize the fallback for 32-bit platforms.
                uint al = (uint)a;
                uint ah = (uint)(a >> 32);
                uint bl = (uint)b;
                uint bh = (uint)(b >> 32);

                ulong mull = ((ulong)al) * bl;
                ulong t = ((ulong)ah) * bl + (mull >> 32);
                ulong tl = ((ulong)al) * bh + (uint)t;

                low = tl << 32 | (uint)mull;

                return ((ulong)ah) * bh + (t >> 32) + (tl >> 32);
            }
        }

        /// <summary>Produces the full product of two 64-bit numbers.</summary>
        /// <param name="a">The first number to multiply.</param>
        /// <param name="b">The second number to multiply.</param>
        /// <param name="low">The low 64-bit of the product of the specified numbers.</param>
        /// <returns>The high 64-bit of the product of the specified numbers.</returns>
        public static long BigMul(long a, long b, out long low)
        {
            if (ArmBase.Arm64.IsSupported)
            {
                low = a * b;
                return ArmBase.Arm64.MultiplyHigh(a, b);
            }

            ulong high = BigMul((ulong)a, (ulong)b, out ulong ulow);
            low = (long)ulow;
            return (long)high - ((a >> 63) & b) - ((b >> 63) & a);
        }

        public static double BitDecrement(double x)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(x);

            if (!double.IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns MaxValue
                return (bits == double.PositiveInfinityBits) ? double.MaxValue : x;
            }

            if (bits == double.PositiveZeroBits)
            {
                // +0.0 returns -double.Epsilon
                return -double.Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            if (double.IsNegative(x))
            {
                bits += 1;
            }
            else
            {
                bits -= 1;
            }
            return BitConverter.UInt64BitsToDouble(bits);
        }

        public static double BitIncrement(double x)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(x);

            if (!double.IsFinite(x))
            {
                // NaN returns NaN
                // -Infinity returns MinValue
                // +Infinity returns +Infinity
                return (bits == double.NegativeInfinityBits) ? double.MinValue : x;
            }

            if (bits == double.NegativeZeroBits)
            {
                // -0.0 returns Epsilon
                return double.Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            if (double.IsNegative(x))
            {
                bits -= 1;
            }
            else
            {
                bits += 1;
            }
            return BitConverter.UInt64BitsToDouble(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CopySign(double x, double y)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                return Vector128.ConditionalSelect(Vector128.CreateScalarUnsafe(-0.0), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return SoftwareFallback(x, y);
            }

            static double SoftwareFallback(double x, double y)
            {
                // This method is required to work for all inputs,
                // including NaN, so we operate on the raw bits.
                ulong xbits = BitConverter.DoubleToUInt64Bits(x);
                ulong ybits = BitConverter.DoubleToUInt64Bits(y);

                // Remove the sign from x, and remove everything but the sign from y
                // Then, simply OR them to get the correct sign
                return BitConverter.UInt64BitsToDouble((xbits & ~double.SignMask) | (ybits & double.SignMask));
            }
        }

        public static int DivRem(int a, int b, out int result)
        {
            // TODO https://github.com/dotnet/runtime/issues/5213:
            // Restore to using % and / when the JIT is able to eliminate one of the idivs.
            // In the meantime, a * and - is measurably faster than an extra /.

            int div = a / b;
            result = a - (div * b);
            return div;
        }

        public static long DivRem(long a, long b, out long result)
        {
            long div = a / b;
            result = a - (div * b);
            return div;
        }

        /// <summary>Produces the quotient and the remainder of two signed 8-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (sbyte Quotient, sbyte Remainder) DivRem(sbyte left, sbyte right)
        {
            sbyte quotient = (sbyte)(left / right);
            return (quotient, (sbyte)(left - (quotient * right)));
        }

        /// <summary>Produces the quotient and the remainder of two unsigned 8-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte Quotient, byte Remainder) DivRem(byte left, byte right)
        {
            byte quotient = (byte)(left / right);
            return (quotient, (byte)(left - (quotient * right)));
        }

        /// <summary>Produces the quotient and the remainder of two signed 16-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (short Quotient, short Remainder) DivRem(short left, short right)
        {
            short quotient = (short)(left / right);
            return (quotient, (short)(left - (quotient * right)));
        }

        /// <summary>Produces the quotient and the remainder of two unsigned 16-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ushort Quotient, ushort Remainder) DivRem(ushort left, ushort right)
        {
            ushort quotient = (ushort)(left / right);
            return (quotient, (ushort)(left - (quotient * right)));
        }

        /// <summary>Produces the quotient and the remainder of two signed 32-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int Quotient, int Remainder) DivRem(int left, int right)
        {
            int quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <summary>Produces the quotient and the remainder of two unsigned 32-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (uint Quotient, uint Remainder) DivRem(uint left, uint right)
        {
            uint quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <summary>Produces the quotient and the remainder of two signed 64-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (long Quotient, long Remainder) DivRem(long left, long right)
        {
            long quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <summary>Produces the quotient and the remainder of two unsigned 64-bit numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong Quotient, ulong Remainder) DivRem(ulong left, ulong right)
        {
            ulong quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <summary>Produces the quotient and the remainder of two signed native-size numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (nint Quotient, nint Remainder) DivRem(nint left, nint right)
        {
            nint quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        /// <summary>Produces the quotient and the remainder of two unsigned native-size numbers.</summary>
        /// <param name="left">The dividend.</param>
        /// <param name="right">The divisor.</param>
        /// <returns>The quotient and the remainder of the specified numbers.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (nuint Quotient, nuint Remainder) DivRem(nuint left, nuint right)
        {
            nuint quotient = left / right;
            return (quotient, left - (quotient * right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Ceiling(decimal d)
        {
            return decimal.Ceiling(d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp(byte value, byte min, byte max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Clamp(decimal value, decimal min, decimal max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Clamp(short value, short min, short max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Clamp(long value, long min, long max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>Returns <paramref name="value" /> clamped to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</summary>
        /// <param name="value">The value to be clamped.</param>
        /// <param name="min">The lower bound of the result.</param>
        /// <param name="max">The upper bound of the result.</param>
        /// <returns>
        ///   <paramref name="value" /> if <paramref name="min" /> \u2264 <paramref name="value" /> \u2264 <paramref name="max" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="min" /> if <paramref name="value" /> &lt; <paramref name="min" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="max" /> if <paramref name="max" /> &lt; <paramref name="value" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Clamp(nint value, nint min, nint max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Clamp(ushort value, ushort min, ushort max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint Clamp(uint value, uint min, uint max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong Clamp(ulong value, ulong min, ulong max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>Returns <paramref name="value" /> clamped to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</summary>
        /// <param name="value">The value to be clamped.</param>
        /// <param name="min">The lower bound of the result.</param>
        /// <param name="max">The upper bound of the result.</param>
        /// <returns>
        ///   <paramref name="value" /> if <paramref name="min" /> \u2264 <paramref name="value" /> \u2264 <paramref name="max" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="min" /> if <paramref name="value" /> &lt; <paramref name="min" />.
        ///
        ///   -or-
        ///
        ///   <paramref name="max" /> if <paramref name="max" /> &lt; <paramref name="value" />.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static nuint Clamp(nuint value, nuint min, nuint max)
        {
            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Floor(decimal d)
        {
            return decimal.Floor(d);
        }

        public static double IEEERemainder(double x, double y)
        {
            if (double.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (double.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            double regularMod = x % y;

            if (double.IsNaN(regularMod))
            {
                return double.NaN;
            }

            if ((regularMod == 0) && double.IsNegative(x))
            {
                return double.NegativeZero;
            }

            double alternativeResult = (regularMod - (Abs(y) * Sign(x)));

            if (Abs(alternativeResult) == Abs(regularMod))
            {
                double divisionResult = x / y;
                double roundedResult = Round(divisionResult);

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

        public static int ILogB(double x)
        {
            // This code is based on `ilogb` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            if (!double.IsNormal(x)) // x is zero, subnormal, infinity, or NaN
            {
                if (double.IsZero(x))
                {
                    return int.MinValue;
                }

                if (!double.IsFinite(x)) // infinity or NaN
                {
                    return int.MaxValue;
                }

                Debug.Assert(double.IsSubnormal(x));
                return double.MinExponent - (BitOperations.TrailingZeroCount(x.TrailingSignificand) - double.BiasedExponentLength);
            }

            return x.Exponent;
        }

        public static double Log(double a, double newBase)
        {
            if (double.IsNaN(a))
            {
                return a; // IEEE 754-2008: NaN payload must be preserved
            }

            if (double.IsNaN(newBase))
            {
                return newBase; // IEEE 754-2008: NaN payload must be preserved
            }

            if (newBase == 1)
            {
                return double.NaN;
            }

            if ((a != 1) && ((newBase == 0) || double.IsPositiveInfinity(newBase)))
            {
                return double.NaN;
            }

            return Log(a) / Log(newBase);
        }

        [NonVersionable]
        public static byte Max(byte val1, byte val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Max(decimal val1, decimal val2)
        {
            return decimal.Max(val1, val2);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double val1, double val2)
        {
            // This matches the IEEE 754:2019 `maximum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs. It
            // treats +0 as greater than -0 as per the specification.

            if (val1 != val2)
            {
                if (!double.IsNaN(val1))
                {
                    return val2 < val1 ? val1 : val2;
                }

                return val1;
            }

            return double.IsNegative(val2) ? val1 : val2;
        }

        [NonVersionable]
        public static short Max(short val1, short val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [NonVersionable]
        public static int Max(int val1, int val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [NonVersionable]
        public static long Max(long val1, long val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        /// <summary>Returns the larger of two native signed integers.</summary>
        /// <param name="val1">The first of two native signed integers to compare.</param>
        /// <param name="val2">The second of two native signed integers to compare.</param>
        /// <returns>Parameter <paramref name="val1" /> or <paramref name="val2" />, whichever is larger.</returns>
        [NonVersionable]
        public static nint Max(nint val1, nint val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static sbyte Max(sbyte val1, sbyte val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float val1, float val2)
        {
            // This matches the IEEE 754:2019 `maximum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the greater of the inputs. It
            // treats +0 as greater than -0 as per the specification.

            if (val1 != val2)
            {
                if (!float.IsNaN(val1))
                {
                    return val2 < val1 ? val1 : val2;
                }

                return val1;
            }

            return float.IsNegative(val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static ushort Max(ushort val1, ushort val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static uint Max(uint val1, uint val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static ulong Max(ulong val1, ulong val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        /// <summary>Returns the larger of two native unsigned integers.</summary>
        /// <param name="val1">The first of two native unsigned integers to compare.</param>
        /// <param name="val2">The second of two native unsigned integers to compare.</param>
        /// <returns>Parameter <paramref name="val1" /> or <paramref name="val2" />, whichever is larger.</returns>
        [CLSCompliant(false)]
        [NonVersionable]
        public static nuint Max(nuint val1, nuint val2)
        {
            return (val1 >= val2) ? val1 : val2;
        }

        [Intrinsic]
        public static double MaxMagnitude(double x, double y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a greater magnitude.
            // It treats +0 as greater than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax > ay) || double.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return double.IsNegative(x) ? y : x;
            }

            return y;
        }

        [NonVersionable]
        public static byte Min(byte val1, byte val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Min(decimal val1, decimal val2)
        {
            return decimal.Min(val1, val2);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double val1, double val2)
        {
            // This matches the IEEE 754:2019 `minimum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the lesser of the inputs. It
            // treats +0 as greater than -0 as per the specification.

            if (val1 != val2)
            {
                if (!double.IsNaN(val1))
                {
                    return val1 < val2 ? val1 : val2;
                }

                return val1;
            }

            return double.IsNegative(val1) ? val1 : val2;
        }

        [NonVersionable]
        public static short Min(short val1, short val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [NonVersionable]
        public static int Min(int val1, int val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [NonVersionable]
        public static long Min(long val1, long val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        /// <summary>Returns the smaller of two native signed integers.</summary>
        /// <param name="val1">The first of two native signed integers to compare.</param>
        /// <param name="val2">The second of two native signed integers to compare.</param>
        /// <returns>Parameter <paramref name="val1" /> or <paramref name="val2" />, whichever is smaller.</returns>
        [NonVersionable]
        public static nint Min(nint val1, nint val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static sbyte Min(sbyte val1, sbyte val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float val1, float val2)
        {
            // This matches the IEEE 754:2019 `minimum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the lesser of the inputs. It
            // treats +0 as greater than -0 as per the specification.

            if (val1 != val2)
            {
                if (!float.IsNaN(val1))
                {
                    return val1 < val2 ? val1 : val2;
                }

                return val1;
            }

            return float.IsNegative(val1) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static ushort Min(ushort val1, ushort val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static uint Min(uint val1, uint val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [CLSCompliant(false)]
        [NonVersionable]
        public static ulong Min(ulong val1, ulong val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        /// <summary>Returns the smaller of two native unsigned integers.</summary>
        /// <param name="val1">The first of two native unsigned integers to compare.</param>
        /// <param name="val2">The second of two native unsigned integers to compare.</param>
        /// <returns>Parameter <paramref name="val1" /> or <paramref name="val2" />, whichever is smaller.</returns>
        [CLSCompliant(false)]
        [NonVersionable]
        public static nuint Min(nuint val1, nuint val2)
        {
            return (val1 <= val2) ? val1 : val2;
        }

        [Intrinsic]
        public static double MinMagnitude(double x, double y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a lesser magnitude.
            // It treats +0 as greater than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax < ay) || double.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                return double.IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <summary>Returns an estimate of the reciprocal of a specified number.</summary>
        /// <param name="d">The number whose reciprocal is to be estimated.</param>
        /// <returns>An estimate of the reciprocal of <paramref name="d" />.</returns>
        /// <remarks>
        ///    <para>On ARM64 hardware this may use the <c>FRECPE</c> instruction which performs a single Newton-Raphson iteration.</para>
        ///    <para>On hardware without specialized support, this may just return <c>1.0 / d</c>.</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReciprocalEstimate(double d)
        {
            // x86 doesn't provide an estimate instruction for double-precision reciprocal

            if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ReciprocalEstimateScalar(Vector64.CreateScalar(d)).ToScalar();
            }
            else
            {
                return 1.0 / d;
            }
        }

        /// <summary>Returns an estimate of the reciprocal square root of a specified number.</summary>
        /// <param name="d">The number whose reciprocal square root is to be estimated.</param>
        /// <returns>An estimate of the reciprocal square root <paramref name="d" />.</returns>
        /// <remarks>
        ///    <para>On ARM64 hardware this may use the <c>FRSQRTE</c> instruction which performs a single Newton-Raphson iteration.</para>
        ///    <para>On hardware without specialized support, this may just return <c>1.0 / Sqrt(d)</c>.</para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReciprocalSqrtEstimate(double d)
        {
            // x86 doesn't provide an estimate instruction for double-precision reciprocal square root

            if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ReciprocalSquareRootEstimateScalar(Vector64.CreateScalar(d)).ToScalar();
            }
            else
            {
                return 1.0 / Sqrt(d);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d)
        {
            return decimal.Round(d, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, int decimals)
        {
            return decimal.Round(d, decimals);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, MidpointRounding mode)
        {
            return decimal.Round(d, 0, mode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Round(decimal d, int decimals, MidpointRounding mode)
        {
            return decimal.Round(d, decimals, mode);
        }

        [Intrinsic]
        public static double Round(double a)
        {
            // ************************************************************************************
            // IMPORTANT: Do not change this implementation without also updating MathF.Round(float),
            //            FloatingPointUtils::round(double), and FloatingPointUtils::round(float)
            // ************************************************************************************

            // This code is based on `nearbyint` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // This represents the boundary at which point we can only represent whole integers
            const double IntegerBoundary = 4503599627370496.0; // 2^52

            if (Abs(a) >= IntegerBoundary)
            {
                // Values above this boundary don't have a fractional
                // portion and so we can simply return them as-is.
                return a;
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

            double temp = CopySign(IntegerBoundary, a);
            return CopySign((a + temp) - temp, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, int digits)
        {
            return Round(value, digits, MidpointRounding.ToEven);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, MidpointRounding mode)
        {
            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                case MidpointRounding.AwayFromZero:
                    // For ARM/ARM64 we can lower it down to a single instruction FRINTA
                    if (AdvSimd.IsSupported)
                        return AdvSimd.RoundAwayFromZeroScalar(Vector64.CreateScalarUnsafe(value)).ToScalar();
                    // For other platforms we use a fast managed implementation
                    // manually fold BitDecrement(0.5)
                    return Truncate(value + CopySign(0.49999999999999994, value));

                // Rounds to the nearest value; if the number falls midway,
                // it is rounded to the nearest value with an even least significant digit
                case MidpointRounding.ToEven:
                    return Round(value);
                // Directed rounding: Round to the nearest value, toward to zero
                case MidpointRounding.ToZero:
                    return Truncate(value);
                // Directed Rounding: Round down to the next value, toward negative infinity
                case MidpointRounding.ToNegativeInfinity:
                    return Floor(value);
                // Directed rounding: Round up to the next value, toward positive infinity
                case MidpointRounding.ToPositiveInfinity:
                    return Ceiling(value);

                default:
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, int digits, MidpointRounding mode)
        {
            if ((uint)digits > maxRoundingDigits)
            {
                ThrowHelper.ThrowArgumentOutOfRange_RoundingDigits(nameof(digits));
            }

            if (Abs(value) < doubleRoundLimit)
            {
                double power10 = RoundPower10Double[digits];
                value = Round(value * power10, mode) / power10;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(decimal value)
        {
            return decimal.Sign(value);
        }

        public static int Sign(double value)
        {
            if (value < 0)
            {
                return -1;
            }
            else if (value > 0)
            {
                return 1;
            }
            else if (value == 0)
            {
                return 0;
            }

            throw new ArithmeticException(SR.Arithmetic_NaN);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(short value)
        {
            return Sign((int)value);
        }

        public static int Sign(int value)
        {
            return unchecked(value >> 31 | (int)((uint)-value >> 31));
        }

        public static int Sign(long value)
        {
            return unchecked((int)(value >> 63 | (long)((ulong)-value >> 63)));
        }

        public static int Sign(nint value)
        {
#if TARGET_64BIT
            return unchecked((int)(value >> 63 | (long)((ulong)-value >> 63)));
#else
            return unchecked((int)(value >> 31) | (int)((uint)-value >> 31));
#endif
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(sbyte value)
        {
            return Sign((int)value);
        }

        public static int Sign(float value)
        {
            if (value < 0)
            {
                return -1;
            }
            else if (value > 0)
            {
                return 1;
            }
            else if (value == 0)
            {
                return 0;
            }

            throw new ArithmeticException(SR.Arithmetic_NaN);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Truncate(decimal d)
        {
            return decimal.Truncate(d);
        }

        [Intrinsic]
        public static unsafe double Truncate(double d)
        {
            ModF(d, &d);
            return d;
        }

        [DoesNotReturn]
        internal static void ThrowMinMaxException<T>(T min, T max)
        {
            throw new ArgumentException(SR.Format(SR.Argument_MinMaxValue, min, max));
        }

        public static double ScaleB(double x, int n)
        {
            // Implementation based on https://git.musl-libc.org/cgit/musl/tree/src/math/scalbln.c
            //
            // Performs the calculation x * 2^n efficiently. It constructs a double from 2^n by building
            // the correct biased exponent. If n is greater than the maximum exponent (1023) or less than
            // the minimum exponent (-1022), adjust x and n to compute correct result.

            double y = x;
            if (n > 1023)
            {
                y *= SCALEB_C1;
                n -= 1023;
                if (n > 1023)
                {
                    y *= SCALEB_C1;
                    n -= 1023;
                    if (n > 1023)
                    {
                        n = 1023;
                    }
                }
            }
            else if (n < -1022)
            {
                y *= SCALEB_C2 * SCALEB_C3;
                n += 1022 - 53;
                if (n < -1022)
                {
                    y *= SCALEB_C2 * SCALEB_C3;
                    n += 1022 - 53;
                    if (n < -1022)
                    {
                        n = -1022;
                    }
                }
            }

            double u = BitConverter.Int64BitsToDouble(((long)(0x3ff + n) << 52));
            return y * u;
        }
    }
}
