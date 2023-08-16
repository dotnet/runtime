// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

//This class contains only static members and doesn't require serialization.

using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;

namespace System
{
    public static class MathF
    {
        private static float singleRoundLimit = 1e8f;

        private const int maxRoundingDigits = 6;

        // This table is required for the Round function which can specify the number of digits to round to
        private static float[] roundPower10Single = new float[] {
            1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f
        };

        public const float PI = 3.14159265f;

        public const float E = 2.71828183f;

        private static float NegativeZero = Int32BitsToSingle(unchecked((int)0x80000000));

        private static unsafe float Int32BitsToSingle(int value)
        {
            return *((float*)&value);
        }

        [Pure]
        private static unsafe bool IsNegative(float f)
        {
            return (*(uint*)(&f) & 0x80000000) == 0x80000000;
        }

        public static float Abs(float x) => Math.Abs(x);

        public static float Acos(float x) => (float)Math.Acos(x);

        public static float Asin(float x) => (float)Math.Asin(x);

        public static float Atan(float x) => (float)Math.Atan(x);

        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);

        public static float Ceiling(float x) => (float)Math.Ceiling(x);

        public static float Cos(float x) => (float)Math.Cos(x);

        public static float Cosh(float x) => (float)Math.Cosh(x);

        public static float Exp(float x) => (float)Math.Exp(x);

        public static float Floor(float x) => (float)Math.Floor(x);

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

            var regularMod = x % y;

            if (float.IsNaN(regularMod))
            {
                return float.NaN;
            }

            if ((regularMod == 0) && IsNegative(x))
            {
                return NegativeZero;
            }

            var alternativeResult = (regularMod - (Abs(y) * Sign(x)));

            if (Abs(alternativeResult) == Abs(regularMod))
            {
                var divisionResult = x / y;
                var roundedResult = Round(divisionResult);

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

        public static float Log(float x) => (float)Math.Log(x);

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

        public static float Log10(float x) => (float)Math.Log10(x);

        public static float Max(float x, float y) => Math.Max(x, y);

        public static float Min(float x, float y) => Math.Min(x, y);

        public static float Pow(float x, float y) => (float)Math.Pow(x, y);

        public static float Round(float x) => (float)Math.Round(x);

        public static float Round(float x, int digits)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), SR.ArgumentOutOfRange_RoundingDigits);
            }
            Contract.EndContractBlock();

            return InternalRound(x, digits, MidpointRounding.ToEven);
        }

        public static float Round(float x, int digits, MidpointRounding mode)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), SR.ArgumentOutOfRange_RoundingDigits);
            }

            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnum, mode, nameof(MidpointRounding)), nameof(mode));
            }
            Contract.EndContractBlock();

            return InternalRound(x, digits, mode);
        }

        public static float Round(float x, MidpointRounding mode)
        {
            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnum, mode, nameof(MidpointRounding)), nameof(mode));
            }
            Contract.EndContractBlock();

            return InternalRound(x, 0, mode);
        }

        public static int Sign(float x) => Math.Sign(x);

        public static float Sin(float x) => (float)Math.Sin(x);

        public static float Sinh(float x) => (float)Math.Sinh(x);

        public static float Sqrt(float x) => (float)Math.Sqrt(x);

        public static float Tan(float x) => (float)Math.Tan(x);

        public static float Tanh(float x) => (float)Math.Tanh(x);

        public static float Truncate(float x) => InternalTruncate(x);

        private static unsafe float InternalRound(float x, int digits, MidpointRounding mode)
        {
            if (Abs(x) < singleRoundLimit)
            {
                var power10 = roundPower10Single[digits];

                x *= power10;

                if (mode == MidpointRounding.AwayFromZero)
                {
                    var fraction = SplitFractionSingle(&x);

                    if (Abs(fraction) >= 0.5f)
                    {
                        x += Sign(fraction);
                    }
                }
                else
                {
                    x = Round(x);
                }

                x /= power10;
            }

            return x;
        }

        private static unsafe float InternalTruncate(float x)
        {
            SplitFractionSingle(&x);
            return x;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe float SplitFractionSingle(float* x);
    }
}
