// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class factoryMethodTest
    {
        private static void VerifyFromPolarCoordinates(double magnitude, double phase)
        {
            double m = magnitude;
            double p = phase;
            Complex c_new = Complex.FromPolarCoordinates(magnitude, phase);

            // double.IsNaN(magnitude) is checked in the verification method.
            if (double.IsNaN(phase) || double.IsInfinity(phase))
            {
                magnitude = double.NaN;
                phase = double.NaN;
            }
            // Special check in Complex.Abs method
            else if (double.IsInfinity(magnitude))
            {
                magnitude = double.PositiveInfinity;
                phase = double.NaN;
            }

            Support.VerifyMagnitudePhaseProperties(c_new, magnitude, phase, 
                string.Format("FromPolarCoordinates: ({0}, {1})", m, p));

            Complex c_new_ctor = new Complex(c_new.Real, c_new.Imaginary);
            Support.VerifyMagnitudePhaseProperties(c_new_ctor, magnitude, phase, 
                string.Format("FromPolarCoordinates: ({0}, {1})", m, p));
        }

        [Fact]
        public static void RunTests_TypicalValidValues()
        {
            // Test with valid double value pairs
            foreach (double magnitude in Support.doubleValidValues)
            {
                foreach (double phase in Support.phaseTypicalValues)
                {
                    VerifyFromPolarCoordinates(magnitude, phase);
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
            for (int i = 0; i < 3; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(false);
                phaseRandom = Support.GetRandomPhaseValue(false);
                VerifyFromPolarCoordinates(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (negative magnitude, positive phase)
            for (int i = 0; i < 3; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(true);
                phaseRandom = Support.GetRandomPhaseValue(false);
                VerifyFromPolarCoordinates(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (negative magnitude, negative phase)
            for (int i = 0; i < 3; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(true);
                phaseRandom = Support.GetRandomPhaseValue(true);
                VerifyFromPolarCoordinates(magnitudeRandom, phaseRandom);
            }

            // Complex numbers (positive magnitude, negative phase)
            for (int i = 0; i < 3; i++)
            {
                magnitudeRandom = Support.GetRandomDoubleValue(false);
                phaseRandom = Support.GetRandomPhaseValue(true);
                VerifyFromPolarCoordinates(magnitudeRandom, phaseRandom);
            }
        }

        [Fact]
        public static void RunTests_InvalidValues()
        {
            double magnitudeRandom = Support.GetRandomDoubleValue(false);
            double phaseRandom = Support.GetRandomDoubleValue(false);

            // Complex number with a random positive/negative magnitude and an invalid phase part
            foreach (double phaseInvalid in Support.doubleInvalidValues)
            {
                VerifyFromPolarCoordinates(magnitudeRandom, phaseInvalid);
                VerifyFromPolarCoordinates(-magnitudeRandom, phaseInvalid);
            }

            // Complex number with an invalid magnitude and a random positive phase part
            foreach (double magnitudeInvalid in Support.doubleInvalidValues)
            {
                VerifyFromPolarCoordinates(magnitudeInvalid, phaseRandom);
                VerifyFromPolarCoordinates(magnitudeInvalid, -phaseRandom);
            }

            // Complex number with an invalid magnitude and an invalid phase
            foreach (double magnitudeInvalid in Support.doubleInvalidValues)
            {
                foreach (double phaseInvalid in Support.doubleInvalidValues)
                {
                    VerifyFromPolarCoordinates(magnitudeInvalid, phaseInvalid);
                }
            }
        }
    }
}
