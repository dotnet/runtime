// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Numerics;
using Xunit;

namespace ComplexTestSupport
{
    public static class Support
    {
        private static Random s_random;
        public static int randomSample;

        static Support()
        {
            s_random = new Random(-55);

            // randomSample assignment
            randomSample = 3;
        }

        public static Random Random
        {
            get { return s_random; }
        }

        public static int RandomSampleCount
        {
            get { return randomSample; }
        }

        // Valid values in double type
        public static Double[] doubleValidValues = new Double[] {
            Double.MinValue,
            -1,
            0,
            Double.Epsilon,
            1,
            Double.MaxValue,
        };

        // Invalid values in double type
        public static Double[] doubleInvalidValues = new Double[] {
            Double.NegativeInfinity,
            Double.PositiveInfinity,
            Double.NaN
        };

        // Typical phase values in double type
        public static Double[] phaseTypicalValues = new Double[] {
            -Math.PI/2,
            0,
            Math.PI/2
        };

        public static String[] supportedStdNumericFormats = new String[] { "C", "E", "F", "G", "N", "P", "R" };

        private static double GetRandomValue(Double mult, bool fIsNegative)
        {
            Double randomDouble = (mult * s_random.NextDouble());
            randomDouble %= (Double)(mult);

            if (fIsNegative)
            {
                return -randomDouble;
            }

            return randomDouble;
        }

        public static double GetRandomDoubleValue(bool fIsNegative)
        {
            return GetRandomValue(Double.MaxValue, fIsNegative);
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
            return ((BigInteger)GetRandomValue(Double.MaxValue, fIsNegative));
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

        public static bool IsDiffTolerable(Double d1, Double d2)
        {
            Double diffRatio = (d1 - d2) / d1;
            diffRatio *= Math.Pow(10, 6);
            diffRatio = Math.Abs(diffRatio);
            return (diffRatio < 1);
        }

        public static bool VerifyRealImaginaryProperties(Complex complex, double real, double imaginary)
        {
            bool retValue = true; // assume verification is true!

            if (!(real.Equals((Double)complex.Real) || IsDiffTolerable(complex.Real, real)))
            {
                Console.WriteLine("ErRoR!!! Actual Complex ({0}, {1}) real part to be equal to Double:{2}", complex.Real, complex.Imaginary, real);
                retValue = false;
            }

            if (!(imaginary.Equals((Double)complex.Imaginary) || IsDiffTolerable(complex.Imaginary, imaginary)))
            {
                Console.WriteLine("ErRoR!!! Actual Complex ({0}, {1}) imaginary part to be equal to Double:{2}", complex.Real, complex.Imaginary, imaginary);
                retValue = false;
            }

            return retValue;
        }

        public static bool VerifyMagnitudePhaseProperties(Complex complex, double magnitude, double phase)
        {
            bool retValue = true; // assume verification is true!

            // the magnitude is abs value
            // Verification is done by square of magnitude, since m*m == r*r + i*i
            Double expectedMagnitudeSqr = magnitude * magnitude;
            Double actualMagnitudeSqr = complex.Magnitude * complex.Magnitude;

            if (!(expectedMagnitudeSqr.Equals((Double)(actualMagnitudeSqr)) || IsDiffTolerable(actualMagnitudeSqr, expectedMagnitudeSqr)))
            {
                Console.WriteLine(
                                    "ErRoR!!! Actual Complex({0}, {1}) -  Magnitude:{2}, Phase:{3} - magnitude to be equal to Double:{4}",
                                    complex.Real,
                                    complex.Imaginary,
                                    complex.Magnitude,
                                    complex.Phase,
                                    Math.Sqrt(expectedMagnitudeSqr)
                                 );
                retValue = false;
            }

            if (Double.IsNaN(magnitude))
            {
                phase = Double.NaN;
            }
            else if (magnitude == 0)
            {
                phase = 0;
            }
            else if (magnitude < 0)
            {
                if (phase < 0)
                    phase += Math.PI;
                else
                    phase -= Math.PI;
            }

            if (!(phase.Equals((Double)complex.Phase) || IsDiffTolerable(complex.Phase, phase)))
            {
                Console.WriteLine(
                                    "ErRoR!!! Actual Complex({0}, {1}) - Magnitude:{2}, Phase:{3} - phase to be equal to Double:{4}",
                                    complex.Real,
                                    complex.Imaginary,
                                    complex.Magnitude,
                                    complex.Phase,
                                    phase
                                 );
                retValue = false;
            }

            return retValue;
        }

        public static bool EvaluateTestResult(int countTestCase, int countError, string strLog)
        {
            Console.WriteLine(strLog);
            if (0 == countError)
            {
                Console.WriteLine("PaSs!!! Number of Test Cases: {0}", countTestCase);
                return true;
            }
            Console.WriteLine("FaiL!!! Number of Test Cases: {0}, Number of ErRoRs: {1}", countTestCase, countError);
            return false;
        }

        //public static Boolean IsX86orWOW
        //{
        //	get
        //	{
        //		return String.Compare(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), "x86", StringComparison.InvariantCultureIgnoreCase) == 0;
        //	}
        //}

        //public static Boolean IsIA64
        //{
        //	get
        //	{
        //		return String.Compare(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), "IA64", StringComparison.InvariantCultureIgnoreCase) == 0;
        //	}
        //}

        //public static Boolean IsARM
        //{
        //	get
        //	{
        //		return String.Compare(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"), "ARM", StringComparison.InvariantCultureIgnoreCase) == 0;
        //	}
        //}
    }
}
