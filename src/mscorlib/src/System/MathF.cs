// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

//This class contains only static members and doesn't require serialization.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
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

        public static float Abs(float x) => Math.Abs(x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Acos(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Asin(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Atan(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Atan2(float y, float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Ceiling(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Cos(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Cosh(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Exp(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Floor(float x);

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

            if ((regularMod == 0) && float.IsNegative(x))
            {
                return float.NegativeZero;
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Log(float x);

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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Log10(float x);

        public static float Max(float x, float y) => Math.Max(x, y);

        public static float Min(float x, float y) => Math.Min(x, y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Pow(float x, float y);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Round(float x);

        public static float Round(float x, int digits)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
            }
            Contract.EndContractBlock();

            return InternalRound(x, digits, MidpointRounding.ToEven);
        }

        public static float Round(float x, int digits, MidpointRounding mode)
        {
            if ((digits < 0) || (digits > maxRoundingDigits))
            {
                throw new ArgumentOutOfRangeException(nameof(digits), Environment.GetResourceString("ArgumentOutOfRange_RoundingDigits"));
            }

            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnumx", mode, nameof(MidpointRounding)), nameof(mode));
            }
            Contract.EndContractBlock();

            return InternalRound(x, digits, mode);
        }

        public static float Round(float x, MidpointRounding mode)
        {
            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnumx", mode, nameof(MidpointRounding)), nameof(mode));
            }
            Contract.EndContractBlock();

            return InternalRound(x, 0, mode);
        }

        public static int Sign(float x) => Math.Sign(x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Sin(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Sinh(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Sqrt(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Tan(float x);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern float Tanh(float x);

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

        private unsafe static float InternalTruncate(float x)
        {
            SplitFractionSingle(&x);
            return x;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static unsafe extern float SplitFractionSingle(float* x);
    }
}
