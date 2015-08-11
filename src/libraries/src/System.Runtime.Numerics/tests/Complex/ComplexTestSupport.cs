// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Xunit;

namespace ComplexTestSupport
{
    public static class Support
    {
        private static Random s_random;

        static Support()
        {
            s_random = new Random(-55);
        }

        public static Random Random
        {
            get { return s_random; }
        }

        // Valid values in double type
        public static Double[] doubleValidValues = new Double[] {
            double.MinValue,
            -1,
            0,
            double.Epsilon,
            1,
            double.MaxValue,
        };

        // Invalid values in double type
        public static Double[] doubleInvalidValues = new Double[] {
            double.NegativeInfinity,
            double.PositiveInfinity,
            double.NaN
        };

        // Typical phase values in double type
        public static Double[] phaseTypicalValues = new Double[] {
            -Math.PI/2,
            0,
            Math.PI/2
        };

        public static String[] supportedStdNumericFormats = new String[] { "C", "E", "F", "G", "N", "P", "R" };

        private static double GetRandomValue(double mult, bool fIsNegative)
        {
            double randomDouble = (mult * s_random.NextDouble());
            randomDouble %= (Double)(mult);
            return fIsNegative ? -randomDouble : randomDouble;
        }

        public static double GetRandomDoubleValue(bool fIsNegative)
        {
            return GetRandomValue(double.MaxValue, fIsNegative);
        }

        public static double GetSmallRandomDoubleValue(bool fIsNegative)
        {
            return GetRandomValue(1.0, fIsNegative);
        }

        public static Int16 GetRandomInt16Value(bool fIsNegative)
        {
            if (fIsNegative)
            {
                return ((Int16)s_random.Next(Int16.MinValue, 0));
            }
            else
            {
                return ((Int16)s_random.Next(1, Int16.MaxValue));
            }
        }

        public static Int32 GetRandomInt32Value(bool fIsNegative)
        {
            return ((Int32)GetRandomValue(Int32.MaxValue, fIsNegative));
        }

        public static Int64 GetRandomInt64Value(bool fIsNegative)
        {
            return ((Int64)GetRandomValue(Int64.MaxValue, fIsNegative));
        }
        
        public static Byte GetRandomByteValue()
        {
            return ((Byte)s_random.Next(1, Byte.MaxValue));
        }

#if CLS_Compliant
        public static SByte GetRandomSByteValue(bool fIsNegative)
        {
            if (fIsNegative)
            {
                return ((SByte) random.Next(SByte.MinValue, 0));
            }
            else
            {
                return ((SByte) random.Next(1, SByte.MaxValue));
            }
        }

        public static UInt16 GetRandomUInt16Value()
        {
            return ((UInt16)random.Next(1, UInt16.MaxValue));
        }

        public static UInt32 GetRandomUInt32Value()
        {
            return ((UInt32)GetRandomValue(UInt32.MaxValue, false));
        }

        public static UInt64 GetRandomUInt64Value()
        {
            return ((UInt64)GetRandomValue(UInt64.MaxValue, false));
        }
#endif

        public static Single GetRandomSingleValue(bool fIsNegative)
        {
            return ((Single)GetRandomValue(Single.MaxValue, fIsNegative));
        }

        public static BigInteger GetRandomBigIntegerValue(bool fIsNegative)
        {
            return ((BigInteger)GetRandomValue(double.MaxValue, fIsNegative));
        }

        public static Decimal GetRandomDecimalValue(bool fIsNegative)
        {
            if (fIsNegative)
            {
                return ((Decimal)new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    true,
                    (byte)s_random.Next(0, 29)));
            }
            else
            {
                return ((Decimal)new Decimal(
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    s_random.Next(Int32.MinValue, Int32.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29)));
            }
        }

        public static double GetRandomPhaseValue(bool fIsNegative)
        {
            return GetRandomValue((Math.PI / 2), fIsNegative);
        }

        public static bool IsDiffTolerable(double d1, double d2)
        {
            if (double.IsInfinity(d1))
            {
                return AreSameInfinity(d1, d2 * 10);
            }
            else if (double.IsInfinity(d2))
            {
                return AreSameInfinity(d1 * 10, d2);
            }
            else
            {
                double diffRatio = (d1 - d2) / d1;
                diffRatio *= Math.Pow(10, 6);
                diffRatio = Math.Abs(diffRatio);
                return (diffRatio < 1);
            }
        }

        private static bool AreSameInfinity(double d1, double d2)
        {
            return
                double.IsNegativeInfinity(d1) == double.IsNegativeInfinity(d2) &&
                double.IsPositiveInfinity(d1) == double.IsPositiveInfinity(d2);
        }

        public static void VerifyRealImaginaryProperties(Complex complex, double real, double imaginary, string message)
        {
            Assert.True(real.Equals((Double)complex.Real) || IsDiffTolerable(complex.Real, real), message);
            Assert.True(imaginary.Equals((Double)complex.Imaginary) || IsDiffTolerable(complex.Imaginary, imaginary), message);
        }

        public static void VerifyMagnitudePhaseProperties(Complex complex, double magnitude, double phase, string message)
        {
            // The magnitude (m) of a complex number (z = x + yi) is the absolute value - |z| = sqrt(x^2 + y^2)
            // Verification is done using the square of the magnitude since m^2 = x^2 + y^2
            double expectedMagnitudeSqr = magnitude * magnitude;
            double actualMagnitudeSqr = complex.Magnitude * complex.Magnitude;

            Assert.True(expectedMagnitudeSqr.Equals((Double)(actualMagnitudeSqr)) || IsDiffTolerable(actualMagnitudeSqr, expectedMagnitudeSqr), message);

            if (double.IsNaN(magnitude))
            {
                phase = double.NaN;
            }
            else if (magnitude == 0)
            {
                phase = 0;
            }
            else if (magnitude < 0)
            {
                phase += (phase < 0) ? Math.PI : -Math.PI;
            }

            Assert.True(phase.Equals((Double)complex.Phase) || IsDiffTolerable(complex.Phase, phase), message);
        }
    }
}
