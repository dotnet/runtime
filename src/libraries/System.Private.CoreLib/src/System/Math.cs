// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

/*============================================================
**
**
**
** Purpose: Some floating-point math operations
**
**
===========================================================*/

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;

namespace System
{
    public static partial class Math
    {
        public const double E = 2.7182818284590452354;

        public const double PI = 3.14159265358979323846;

        public const double Tau = 6.283185307179586476925;

        private const int maxRoundingDigits = 15;

        private const double doubleRoundLimit = 1e16d;

        // This table is required for the Round function which can specify the number of digits to round to
        private static readonly double[] roundPower10Double = new double[] {
          1E0, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8,
          1E9, 1E10, 1E11, 1E12, 1E13, 1E14, 1E15
        };

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
                    ThrowAbsOverflow();
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
                    ThrowAbsOverflow();
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
                    ThrowAbsOverflow();
                }
            }
            return value;
        }

        /// <summary>Returns the absolute value of a native signed integer.</summary>
        /// <param name="value">A number that is greater than <see cref="IntPtr.MinValue" />, but less than or equal to <see cref="IntPtr.MaxValue" />.</param>
        /// <returns>A native signed integer, x, such that 0 ≤ x ≤ <see cref="IntPtr.MaxValue" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Abs(nint value)
        {
            if (value < 0)
            {
                value = -value;
                if (value < 0)
                {
                    ThrowAbsOverflow();
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
                    ThrowAbsOverflow();
                }
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Abs(decimal value)
        {
            return decimal.Abs(value);
        }

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowAbsOverflow()
        {
            throw new OverflowException(SR.Overflow_NegateTwosCompNum);
        }

        public static long BigMul(int a, int b)
        {
            return ((long)a) * b;
        }

        /// <summary>Produces the full product of two unsigned 64-bit numbers.</summary>
        /// <param name="a">The first number to multiply.</param>
        /// <param name="b">The second number to multiply.</param>
        /// <param name="low">The low 64-bit of the product of the specied numbers.</param>
        /// <returns>The high 64-bit of the product of the specied numbers.</returns>
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
        /// <param name="low">The low 64-bit of the product of the specied numbers.</param>
        /// <returns>The high 64-bit of the product of the specied numbers.</returns>
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
            long bits = BitConverter.DoubleToInt64Bits(x);

            if (((bits >> 32) & 0x7FF00000) >= 0x7FF00000)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns double.MaxValue
                return (bits == 0x7FF00000_00000000) ? double.MaxValue : x;
            }

            if (bits == 0x00000000_00000000)
            {
                // +0.0 returns -double.Epsilon
                return -double.Epsilon;
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            bits += ((bits < 0) ? +1 : -1);
            return BitConverter.Int64BitsToDouble(bits);
        }

        public static double BitIncrement(double x)
        {
            long bits = BitConverter.DoubleToInt64Bits(x);

            if (((bits >> 32) & 0x7FF00000) >= 0x7FF00000)
            {
                // NaN returns NaN
                // -Infinity returns double.MinValue
                // +Infinity returns +Infinity
                return (bits == unchecked((long)(0xFFF00000_00000000))) ? double.MinValue : x;
            }

            if (bits == unchecked((long)(0x80000000_00000000)))
            {
                // -0.0 returns double.Epsilon
                return double.Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            bits += ((bits < 0) ? -1 : +1);
            return BitConverter.Int64BitsToDouble(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CopySign(double x, double y)
        {
            if (Sse2.IsSupported || AdvSimd.IsSupported)
            {
                return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0.0), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
            }
            else
            {
                return SoftwareFallback(x, y);
            }

            static double SoftwareFallback(double x, double y)
            {
                const long signMask = 1L << 63;

                // This method is required to work for all inputs,
                // including NaN, so we operate on the raw bits.
                long xbits = BitConverter.DoubleToInt64Bits(x);
                long ybits = BitConverter.DoubleToInt64Bits(y);

                // Remove the sign from x, and remove everything but the sign from y
                xbits &= ~signMask;
                ybits &= signMask;

                // Simply OR them to get the correct sign
                return BitConverter.Int64BitsToDouble(xbits | ybits);
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
        ///   <paramref name="value" /> if <paramref name="min" /> ≤ <paramref name="value" /> ≤ <paramref name="max" />.
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
        ///   <paramref name="value" /> if <paramref name="min" /> ≤ <paramref name="value" /> ≤ <paramref name="max" />.
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double val1, double val2)
        {
            // This matches the IEEE 754:2019 `maximum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float val1, float val2)
        {
            // This matches the IEEE 754:2019 `maximum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

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

        public static double MaxMagnitude(double x, double y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double val1, double val2)
        {
            // This matches the IEEE 754:2019 `minimum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (val1 != val2 && !double.IsNaN(val1))
            {
                return val1 < val2 ? val1 : val2;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float val1, float val2)
        {
            // This matches the IEEE 754:2019 `minimum` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (val1 != val2 && !float.IsNaN(val1))
            {
                return val1 < val2 ? val1 : val2;
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

        public static double MinMagnitude(double x, double y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

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

            // This is based on the 'Berkeley SoftFloat Release 3e' algorithm

            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(a);
            int exponent = double.ExtractExponentFromBits(bits);

            if (exponent <= 0x03FE)
            {
                if ((bits << 1) == 0)
                {
                    // Exactly +/- zero should return the original value
                    return a;
                }

                // Any value less than or equal to 0.5 will always round to exactly zero
                // and any value greater than 0.5 will always round to exactly one. However,
                // we need to preserve the original sign for IEEE compliance.

                double result = ((exponent == 0x03FE) && (double.ExtractSignificandFromBits(bits) != 0)) ? 1.0 : 0.0;
                return CopySign(result, a);
            }

            if (exponent >= 0x0433)
            {
                // Any value greater than or equal to 2^52 cannot have a fractional part,
                // So it will always round to exactly itself.

                return a;
            }

            // The absolute value should be greater than or equal to 1.0 and less than 2^52
            Debug.Assert((0x03FF <= exponent) && (exponent <= 0x0432));

            // Determine the last bit that represents the integral portion of the value
            // and the bits representing the fractional portion

            ulong lastBitMask = 1UL << (0x0433 - exponent);
            ulong roundBitsMask = lastBitMask - 1;

            // Increment the first fractional bit, which represents the midpoint between
            // two integral values in the current window.

            bits += lastBitMask >> 1;

            if ((bits & roundBitsMask) == 0)
            {
                // If that overflowed and the rest of the fractional bits are zero
                // then we were exactly x.5 and we want to round to the even result

                bits &= ~lastBitMask;
            }
            else
            {
                // Otherwise, we just want to strip the fractional bits off, truncating
                // to the current integer value.

                bits &= ~roundBitsMask;
            }

            return BitConverter.Int64BitsToDouble((long)bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, int digits)
        {
            return Round(value, digits, MidpointRounding.ToEven);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, MidpointRounding mode)
        {
            return Round(value, 0, mode);
        }

        public static unsafe double Round(double value, int digits, MidpointRounding mode)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), SR.ArgumentOutOfRange_RoundingDigits);
            }

            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
            }

            if (Abs(value) < doubleRoundLimit)
            {
                double power10 = roundPower10Double[digits];

                value *= power10;

                switch (mode)
                {
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value with an even least significant digit
                    case MidpointRounding.ToEven:
                    {
                        value = Round(value);
                        break;
                    }
                    // Rounds to the nearest value; if the number falls midway,
                    // it is rounded to the nearest value above (for positive numbers) or below (for negative numbers)
                    case MidpointRounding.AwayFromZero:
                    {
                        double fraction = ModF(value, &value);

                        if (Abs(fraction) >= 0.5)
                        {
                            value += Sign(fraction);
                        }

                        break;
                    }
                    // Directed rounding: Round to the nearest value, toward to zero
                    case MidpointRounding.ToZero:
                    {
                        value = Truncate(value);
                        break;
                    }
                    // Directed Rounding: Round down to the next value, toward negative infinity
                    case MidpointRounding.ToNegativeInfinity:
                    {
                        value = Floor(value);
                        break;
                    }
                    // Directed rounding: Round up to the next value, toward positive infinity
                    case MidpointRounding.ToPositiveInfinity:
                    {
                        value = Ceiling(value);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, nameof(MidpointRounding)), nameof(mode));
                    }
                }

                value /= power10;
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

        public static unsafe double Truncate(double d)
        {
            ModF(d, &d);
            return d;
        }

        [DoesNotReturn]
        private static void ThrowMinMaxException<T>(T min, T max)
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
