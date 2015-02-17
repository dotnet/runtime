// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class factoryMethodTest
    {
        private static void VerifyFactoryMethod(double magnitude, double phase)
        {
            double m = magnitude;
            double p = phase;
            Complex c_new = Complex.FromPolarCoordinates(magnitude, phase);
            //Double.IsNaN(magnitude) is checked in the verifiation method.
            if (Double.IsNaN(phase) || Double.IsInfinity(phase))
            {
                magnitude = Double.NaN;
                phase = Double.NaN;
            }
            // Special check in Complex.Abs method
            else if (Double.IsInfinity(magnitude))
            {
                magnitude = Double.PositiveInfinity;
                phase = Double.NaN;
            }

            if (false == Support.VerifyMagnitudePhaseProperties(c_new, magnitude, phase))
            {
                Console.WriteLine("Error_89fdl!!! FromPolorCoordinates: ({0}, {1})", m, p);

                Assert.True(false, "Verification Failed");
            }
            else // if the first verification returns TrUe, do the second one!
            {
                Complex c_new_ctor = new Complex(c_new.Real, c_new.Imaginary);
                if (false == Support.VerifyMagnitudePhaseProperties(c_new_ctor, magnitude, phase))
                {
                    Console.WriteLine("Error_fs46!!! FromPolorCoordinates: ({0}, {1})", m, p);

                    Assert.True(false, "Verification Failed");
                }
            }
        }

        [Fact]
        public static void RunTests_TypicalValidValues()
        {
            // Test with valid double value pairs
            foreach (double magnitude in Support.doubleValidValues)
            {
                foreach (double phase in Support.phaseTypicalValues)
                {
                    VerifyFactoryMethod(magnitude, phase);
                }
            }
        }

        [Fact]
        public static void RunTests_RandomValidValues()
        {
            // local Random real and imaginary parts
            double magnitudeRandom;
            double phaseRandom;

            // Complex numbers (postive magnitude, positive phase)
            for (int i = 0; i < Support.randomSample; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(false);
                phaseRandom = Support.GetRandomPhaseValue(false);
                VerifyFactoryMethod(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (negative magnitude, positive phase)
            for (int i = 0; i < Support.randomSample; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(true);
                phaseRandom = Support.GetRandomPhaseValue(false);
                VerifyFactoryMethod(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (negative magnitude, negative phase)
            for (int i = 0; i < Support.randomSample; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(true);
                phaseRandom = Support.GetRandomPhaseValue(true);
                VerifyFactoryMethod(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (positive magnitude, negative phase)
            for (int i = 0; i < Support.randomSample; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(false);
                phaseRandom = Support.GetRandomPhaseValue(true);
                VerifyFactoryMethod(magnitudeRandom, phaseRandom);
            }
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            // local variables
            Double magnitudeRandom = Support.GetRandomDoubleValue(false);
            Double phaseRandom = Support.GetRandomDoubleValue(false);

            // Complex number with a random positive/negative magnitude and an invalid phase part
            foreach (double phaseInvalid in Support.doubleInvalidValues)
            {
                VerifyFactoryMethod(magnitudeRandom, phaseInvalid);
                VerifyFactoryMethod(-magnitudeRandom, phaseInvalid);
            }

            // Complex number with an invalid magnitude and a random positive phase part
            foreach (double magnitudeInvalid in Support.doubleInvalidValues)
            {
                VerifyFactoryMethod(magnitudeInvalid, phaseRandom);
                VerifyFactoryMethod(magnitudeInvalid, -phaseRandom);
            }

            // Complex number with an invalid magnitude and an invalid phase
            foreach (double magnitudeInvalid in Support.doubleInvalidValues)
            {
                foreach (double phaseInvalid in Support.doubleInvalidValues)
                {
                    VerifyFactoryMethod(magnitudeInvalid, phaseInvalid);
                }
            }
        }
    }
}
