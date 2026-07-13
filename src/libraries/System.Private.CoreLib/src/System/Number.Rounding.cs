// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class Number
    {
        // Attempts to round `value` to `digits` fractional decimal digits without resorting to the
        // arbitrary precision arithmetic used by `RoundToDecimalDigits`. It produces the exact same
        // (correctly rounded) result as that routine when it succeeds, and returns `false` otherwise
        // so the caller can fall back.
        //
        // The double-double `FusedMultiplyAdd` approach is fastest wherever the hardware provides a
        // fused-multiply-add (FMA3 on x86, baseline on Arm64); off such hardware it degrades to slow
        // software emulation, so we fall back to the exact integer approach there instead.
        public static bool TryRoundToDecimalDigitsFast(double value, int digits, MidpointRounding mode, out double result)
        {
            Debug.Assert(double.IsFinite(value));
            Debug.Assert((uint)digits <= 19);
            Debug.Assert((uint)mode <= (uint)MidpointRounding.ToPositiveInfinity);

            return (Fma.IsSupported || AdvSimd.Arm64.IsSupported)
                ? TryRoundToDecimalDigitsViaFusedMultiplyAdd(value, digits, mode, out result)
                : TryRoundToDecimalDigitsViaInteger(value, digits, mode, out result);
        }

        /// <inheritdoc cref="TryRoundToDecimalDigitsFast(double, int, MidpointRounding, out double)" />
        public static bool TryRoundToDecimalDigitsFast(float value, int digits, MidpointRounding mode, out float result)
        {
            Debug.Assert(float.IsFinite(value));
            Debug.Assert((uint)digits <= 10);
            Debug.Assert((uint)mode <= (uint)MidpointRounding.ToPositiveInfinity);

            return (Fma.IsSupported || AdvSimd.Arm64.IsSupported)
                ? TryRoundToDecimalDigitsViaFusedMultiplyAdd(value, digits, mode, out result)
                : TryRoundToDecimalDigitsViaInteger(value, digits, mode, out result);
        }

        // The scaled value `|value| * 10^digits` is computed as an exact `hi + lo` double-double via a
        // fused-multiply-add. `10^digits` is exactly representable for the supported `digits` range, so
        // the only rounding is the single one folded into `hi`, which `lo` recovers exactly. We can then
        // determine the correctly rounded integer part directly, and materialize the result with a single
        // correctly rounded division so long as that integer is exactly representable (which is guaranteed
        // while `hi` stays below the point where every value is already an integer).
        private static bool TryRoundToDecimalDigitsViaFusedMultiplyAdd<TNumber>(TNumber value, int digits, MidpointRounding mode, out TNumber result)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            TNumber one = TNumber.One;

            // `10^digits` is exact for every supported `digits` and comes from the shared powers-of-ten table.
            TNumber pow10 = TNumber.CreateTruncating(Pow10DoubleTable[digits]);

            TNumber av = TNumber.Abs(value);
            TNumber hi = av * pow10;
            TNumber lo = TNumber.FusedMultiplyAdd(av, pow10, -hi);

            // `2^(NormalMantissaBits - 1)` is the point at or above which every representable value is already
            // an integer. If the scaled value reaches it, the rounded integer would no longer be exactly
            // representable and the final division would no longer be correctly rounded.
            TNumber integerBoundary = TNumber.ScaleB(one, TNumber.NormalMantissaBits - 1);

            if (hi >= integerBoundary)
            {
                result = default;
                return false;
            }

            bool isNegative = TNumber.IsNegative(value);
            TNumber zero = TNumber.Zero;

            // The nearest integer to `hi` is exactly representable, and `hi - rn` is exact (its magnitude
            // is at most 1/2). The exact fractional part of the scaled value relative to `rn` is therefore
            // `(hi - rn) + lo`, which we hold exactly as the double-double `s + e` via a two-sum.
            TNumber rn = TNumber.Round(hi, MidpointRounding.ToEven);
            TNumber diff = hi - rn;

            TNumber s = diff + lo;
            TNumber bb = s - diff;
            TNumber e = (diff - (s - bb)) + (lo - bb);

            int signOfResidual = (s > zero) ? 1 : (s < zero) ? -1 : (e > zero) ? 1 : (e < zero) ? -1 : 0;
            bool hasRemainder = (s != zero) || (e != zero);

            TNumber half = one / (one + one);

            // `floor` of the scaled value and how its fractional part compares to the midpoint `0.5`.
            TNumber floor;
            int midpointComparison;

            if (signOfResidual >= 0)
            {
                floor = rn;
                midpointComparison = CompareResidualToThreshold(s, e, half);
            }
            else
            {
                floor = rn - one;
                midpointComparison = CompareResidualToThreshold(s, e, -half);
            }

            bool isFloorOdd = (long.CreateTruncating(floor) & 1L) != 0L;
            bool roundUp = ShouldRoundUp(mode, midpointComparison, isFloorOdd, hasRemainder, isNegative);

            TNumber quotient = roundUp ? (floor + one) : floor;

            // `quotient` and `pow10` are both exact, so this division is correctly rounded and yields the
            // nearest representable value to the exactly rounded decimal result.
            TNumber rounded = quotient / pow10;
            result = isNegative ? -rounded : rounded;
            return true;
        }

        // Rounds by operating on the exact value directly. With `value = mantissa * 2^exponent`, the scaled
        // value `|value| * 10^digits` is the integer `mantissa * 10^digits` right-shifted by `-exponent`,
        // so the floor and the exact comparison of the discarded fraction to `1/2` are pure integer work.
        // The `mantissa * 10^digits` product stays in a `ulong` for the common case and only widens to a
        // `UInt128` when it overflows 64 bits, which cannot happen for `float`.
        private static bool TryRoundToDecimalDigitsViaInteger(double value, int digits, MidpointRounding mode, out double result)
        {
            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);
            bool isNegative = double.IsNegative(value);

            // When `exponent + digits >= 0` the scaled value is already an integer, so `value` is an exact
            // multiple of `10^-digits` and rounding leaves it unchanged.
            if ((exponent + digits) >= 0)
            {
                result = value;
                return true;
            }

            int shift = -exponent;
            ulong high = Math.BigMul(mantissa, (ulong)Pow10DoubleTable[digits], out ulong low);

            ulong floor;
            int midpointComparison;
            bool hasRemainder;

            bool inRange = (high == 0)
                ? TryGetFloorAndMidpoint(low, shift, DoubleIntegerBoundaryLog2, out floor, out midpointComparison, out hasRemainder)
                : TryGetFloorAndMidpoint(new UInt128(high, low), shift, DoubleIntegerBoundaryLog2, out floor, out midpointComparison, out hasRemainder);

            if (!inRange)
            {
                result = default;
                return false;
            }

            bool isFloorOdd = (floor & 1) != 0;
            bool roundUp = ShouldRoundUp(mode, midpointComparison, isFloorOdd, hasRemainder, isNegative);

            ulong quotient = roundUp ? (floor + 1) : floor;

            // `quotient` (<= 2^52) and `10^digits` are both exact, so this division is correctly rounded.
            double rounded = quotient / Pow10DoubleTable[digits];
            result = isNegative ? -rounded : rounded;
            return true;
        }

        /// <inheritdoc cref="TryRoundToDecimalDigitsViaInteger(double, int, MidpointRounding, out double)" />
        private static bool TryRoundToDecimalDigitsViaInteger(float value, int digits, MidpointRounding mode, out float result)
        {
            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);
            bool isNegative = float.IsNegative(value);

            if ((exponent + digits) >= 0)
            {
                result = value;
                return true;
            }

            int shift = -exponent;

            // `mantissa` (< 2^24) times `10^digits` (<= 10^10) is at most ~2^57, so it never overflows a ulong.
            ulong scaled = mantissa * (ulong)Pow10DoubleTable[digits];

            if (!TryGetFloorAndMidpoint(scaled, shift, SingleIntegerBoundaryLog2, out ulong floor, out int midpointComparison, out bool hasRemainder))
            {
                result = default;
                return false;
            }

            bool isFloorOdd = (floor & 1) != 0;
            bool roundUp = ShouldRoundUp(mode, midpointComparison, isFloorOdd, hasRemainder, isNegative);

            ulong quotient = roundUp ? (floor + 1) : floor;

            // `quotient` (<= 2^23) and `10^digits` are both exact in `float`, so this division is correctly rounded.
            float rounded = quotient / (float)Pow10DoubleTable[digits];
            result = isNegative ? -rounded : rounded;
            return true;
        }

        // `2^IntegerBoundaryLog2` is the point at or above which every representable value is already an
        // integer; a floor that reaches it could not be materialized exactly by the final division.
        private const int DoubleIntegerBoundaryLog2 = 52;
        private const int SingleIntegerBoundaryLog2 = 23;

        // Computes `floor(scaled / 2^shift)` and how the discarded fraction `(scaled mod 2^shift) / 2^shift`
        // compares to the `1/2` midpoint, where `scaled` is the exact `mantissa * 10^digits`. Returns false
        // when the integer part would not be exactly representable so the caller can fall back.
        private static unsafe bool TryGetFloorAndMidpoint<TUInt>(TUInt scaled, int shift, int integerBoundaryLog2, out ulong floor, out int midpointComparison, out bool hasRemainder)
            where TUInt : unmanaged, IBinaryInteger<TUInt>, IUnsignedNumber<TUInt>
        {
            int bitWidth = sizeof(TUInt) * 8;

            if (shift >= bitWidth)
            {
                // `scaled < 2^bitWidth <= 2^shift`, so the integer part is zero. The midpoint `2^(shift-1)`
                // only overlaps the value's range when `shift == bitWidth`; when `shift > bitWidth` the
                // fraction is strictly below `1/2`, even when `scaled` is itself zero.
                floor = 0;
                hasRemainder = scaled != TUInt.Zero;

                if (shift == bitWidth)
                {
                    TUInt half = TUInt.One << (bitWidth - 1);
                    midpointComparison = (scaled < half) ? -1 : (scaled > half) ? 1 : 0;
                }
                else
                {
                    midpointComparison = -1;
                }
                return true;
            }

            TUInt integerPart = scaled >> shift;

            if (integerPart >= (TUInt.One << integerBoundaryLog2))
            {
                floor = 0;
                midpointComparison = 0;
                hasRemainder = false;
                return false;
            }

            TUInt remainder = scaled - (integerPart << shift);
            hasRemainder = remainder != TUInt.Zero;

            TUInt half2 = TUInt.One << (shift - 1);
            midpointComparison = (remainder < half2) ? -1 : (remainder > half2) ? 1 : 0;

            floor = ulong.CreateTruncating(integerPart);
            return true;
        }

        // Resolves whether the floor should be incremented for the given rounding `mode`, using the sign of
        // the fractional part relative to the `1/2` midpoint (`midpointComparison`) and whether the floor is
        // odd. `mode` is validated by the caller, so the final arm is unreachable.
        private static bool ShouldRoundUp(MidpointRounding mode, int midpointComparison, bool isFloorOdd, bool hasRemainder, bool isNegative)
        {
            return mode switch
            {
                MidpointRounding.ToEven => (midpointComparison > 0) || ((midpointComparison == 0) && isFloorOdd),
                MidpointRounding.AwayFromZero => midpointComparison >= 0,
                MidpointRounding.ToZero => false,
                MidpointRounding.ToNegativeInfinity => isNegative && hasRemainder,
                MidpointRounding.ToPositiveInfinity => !isNegative && hasRemainder,
                _ => throw new UnreachableException(),
            };
        }

        // Compares the exact residual `s + e` against `threshold` (one of `+/-0.5`), returning the sign
        // of the difference. `s - threshold` is exact for these inputs, so it decides the result unless
        // it is zero, in which case the low part `e` breaks the tie exactly.
        private static int CompareResidualToThreshold<TNumber>(TNumber s, TNumber e, TNumber threshold)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            TNumber zero = TNumber.Zero;
            TNumber g = s - threshold;

            if (g != zero)
            {
                return (g > zero) ? 1 : -1;
            }

            return (e > zero) ? 1 : (e < zero) ? -1 : 0;
        }

        // Rounds the exact value represented by `value` to `digits` fractional decimal digits
        // using the specified `mode`, returning the nearest representable result.
        //
        // Unlike scaling the input by a power of 10 and rounding, this operates on the exact
        // value of the input using arbitrary precision arithmetic and so produces the correctly
        // rounded result for all finite inputs, including those that would otherwise appear to be
        // a midpoint after an inexact scaling (e.g. `655.924999999999954525...` scaling to exactly
        // `65592.5` when multiplied by `100`).
        //
        // The caller is responsible for handling values which cannot have a fractional portion at
        // the requested number of digits (namely non-finite values and values whose magnitude is at
        // or above the point where all representable values are integers).
        public static unsafe TNumber RoundToDecimalDigits<TNumber>(TNumber value, int digits, MidpointRounding mode)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            Debug.Assert(TNumber.IsFinite(value));
            Debug.Assert(digits >= 0);

            bool isNegative = TNumber.IsNegative(value);

            // Decompose the input into `mantissa * 2^exponent`, giving us the exact value.
            ulong mantissa = ExtractFractionAndBiasedExponent(value, out int exponent);

            // When `exponent + digits >= 0` the scaled value is already an integer, so `value` is an
            // exact multiple of `10^-digits` and rounding leaves it unchanged. This also bounds the
            // work below: only `digits < -exponent` reaches the arbitrary-precision arithmetic, keeping
            // both the `BigInteger` and the digit buffer within their fixed capacities. The widening to
            // `long` avoids overflow for pathologically large `digits`.
            if (((long)exponent + digits) >= 0)
            {
                return value;
            }

            // We want the nearest integer to `|value| * 10^digits`, which is `numerator / denominator`
            // where both are computed exactly. The `2^exponent` term stays in the numerator when the
            // exponent is non-negative and moves to the denominator otherwise.

            BigInteger.SetUInt64(out BigInteger numerator, mantissa);
            BigInteger denominator;

            if (exponent >= 0)
            {
                numerator.ShiftLeft(exponent);
                numerator.MultiplyPow10((uint)digits);
                BigInteger.SetUInt32(out denominator, 1);
            }
            else
            {
                numerator.MultiplyPow10((uint)digits);
                BigInteger.Pow2(-exponent, out denominator);
            }

            BigInteger.DivRem(ref numerator, ref denominator, out BigInteger quotient, out BigInteger remainder);

            // The fractional portion we are rounding is `remainder / denominator`. Comparing
            // `2 * remainder` against `denominator` tells us whether it is below, exactly at, or
            // above the midpoint without any loss of precision.

            bool hasRemainder = !remainder.IsZero();
            remainder.Multiply(2);
            int midpointComparison = BigInteger.Compare(ref remainder, ref denominator);

            bool roundUp;

            switch (mode)
            {
                // Rounds to the nearest value; if the number falls midway, it is rounded to the
                // nearest value with an even least significant digit.
                case MidpointRounding.ToEven:
                    roundUp = (midpointComparison > 0) || ((midpointComparison == 0) && !quotient.IsZero() && ((quotient.GetBlock(0) & 1) != 0));
                    break;

                // Rounds to the nearest value; if the number falls midway, it is rounded to the
                // nearest value away from zero.
                case MidpointRounding.AwayFromZero:
                    roundUp = midpointComparison >= 0;
                    break;

                // Directed rounding: round toward zero.
                case MidpointRounding.ToZero:
                    roundUp = false;
                    break;

                // Directed rounding: round toward negative infinity.
                case MidpointRounding.ToNegativeInfinity:
                    roundUp = isNegative && hasRemainder;
                    break;

                // Directed rounding: round toward positive infinity.
                case MidpointRounding.ToPositiveInfinity:
                    roundUp = !isNegative && hasRemainder;
                    break;

                default:
                    ThrowHelper.ThrowArgumentException_InvalidEnumValue(mode);
                    return default;
            }

            if (roundUp)
            {
                quotient.Add(1);
            }

            // `quotient` is now the exactly rounded integer value of `|value| * 10^digits`. The final
            // result is the nearest representable value to `quotient * 10^-digits`, which we obtain by
            // materializing the decimal digits and letting the shared conversion perform the correctly
            // rounded decimal-to-binary step.

            byte* pDigits = stackalloc byte[TNumber.NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, TNumber.NumberBufferLength);
            number.IsNegative = isNegative;

            Span<byte> buffer = number.Digits;
            int digitCount = 0;

            if (!quotient.IsZero())
            {
                BigInteger.SetUInt32(out BigInteger ten, 10);

                // Extract the digits least-significant first.
                do
                {
                    BigInteger.DivRem(ref quotient, ref ten, out BigInteger newQuotient, out BigInteger digit);
                    uint digitValue = digit.IsZero() ? 0 : digit.GetBlock(0);
                    buffer[digitCount++] = (byte)('0' + digitValue);
                    BigInteger.SetValue(out quotient, ref newQuotient);
                }
                while (!quotient.IsZero());

                // The decimal point sits `digits` places to the left of the least significant digit,
                // so the scale (number of digits to the left of the decimal point) is `digitCount - digits`.
                number.Scale = digitCount - digits;

                // Reorder to most-significant first, as expected by NumberBuffer.
                buffer.Slice(0, digitCount).Reverse();

                // Trailing zeros carry no value and are not stored in a NumberBuffer.
                while ((digitCount > 0) && (buffer[digitCount - 1] == '0'))
                {
                    digitCount--;
                }
            }

            buffer[digitCount] = (byte)'\0';
            number.DigitsCount = digitCount;

            return NumberToFloat<TNumber>(ref number);
        }
    }
}
