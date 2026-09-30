// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// This is a struct that will be passed as a split struct
struct S
{
    public double d1;
    public double d2;
};

public static class GitHub_18362
{
    private static bool AreSameInfinity(double d1, double d2)
    {
        return
            double.IsNegativeInfinity(d1) == double.IsNegativeInfinity(d2) &&
            double.IsPositiveInfinity(d1) == double.IsPositiveInfinity(d2);
    }

    private static bool IsDiffTolerable(double d1, double d2)
    {
        if (double.IsInfinity(d1))
        {
            return AreSameInfinity(d1, d2 * 10);
        }
        if (double.IsInfinity(d2))
        {
            return AreSameInfinity(d1 * 10, d2);
        }
        double diffRatio = (d1 - d2) / d1;
        diffRatio *= Math.Pow(10, 6);
        return Math.Abs(diffRatio) < 1;
    }

    private static void VerifyRealImaginaryProperties(Complex complex, double real, double imaginary, [CallerLineNumber] int lineNumber = 0)
    {
        if (!real.Equals(complex.Real) && !IsDiffTolerable(complex.Real, real))
        {
            Console.WriteLine("Failure at line {0}. Expected real: {1}. Actual real: {2}", lineNumber, real, complex.Real);
            throw new Exception();
        }
        if (!imaginary.Equals(complex.Imaginary) && !IsDiffTolerable(complex.Imaginary, imaginary))
        {
            Console.WriteLine("Failure at line {0}. Expected imaginary: {1}. Actual imaginary: {2}", lineNumber, imaginary, complex.Imaginary);
            throw new Exception();
        }
    }


    private static void VerifyMagnitudePhaseProperties(Complex complex, double magnitude, double phase, [CallerLineNumber] int lineNumber = 0)
    {
        // The magnitude (m) of a complex number (z = x + yi) is the absolute value - |z| = sqrt(x^2 + y^2)
        // Verification is done using the square of the magnitude since m^2 = x^2 + y^2
        double expectedMagnitudeSquared = magnitude * magnitude;
        double actualMagnitudeSquared = complex.Magnitude * complex.Magnitude;

        if (!expectedMagnitudeSquared.Equals(actualMagnitudeSquared) && !IsDiffTolerable(actualMagnitudeSquared, expectedMagnitudeSquared))
        {
            Console.WriteLine("Failure at line {0}. Expected magnitude squared: {1}. Actual magnitude squared: {2}", lineNumber, expectedMagnitudeSquared, actualMagnitudeSquared);
            throw new Exception();
        }

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

        if (!phase.Equals(complex.Phase) && !IsDiffTolerable(complex.Phase, phase))
        {
            Console.WriteLine("Failure at line {0}. Expected phase: {1}. Actual phase: {2}", lineNumber, phase, complex.Phase);
            throw new Exception();
        }
    }

    internal static void Conjugate(double real, double imaginary)
    {
        int returnVal = 100;

        var complex = new Complex(real, imaginary);
        Complex result = Complex.Conjugate(complex);

        VerifyRealImaginaryProperties(result, real, -imaginary);
        VerifyMagnitudePhaseProperties(result, complex.Magnitude, -complex.Phase);
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Conjugate(2.0, 3.0);
        }
        catch (Exception e)
        {
            return -1;
        }
        return 100;
    }
}
