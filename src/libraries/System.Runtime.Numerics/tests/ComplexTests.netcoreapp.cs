// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

using Xunit;

namespace System.Numerics.Tests
{
    public partial class ComplexTests
    {
        [Fact]
        public static void NaN()
        {   
            Assert.True(Complex.IsNaN(new Complex(double.NaN, double.NaN)));
            Assert.True(Complex.IsNaN(new Complex(1, double.NaN)));
            Assert.True(Complex.IsNaN(new Complex(double.NaN, 1)));
            Assert.True(Complex.IsNaN(Complex.NaN));

            Assert.False(Complex.IsNaN(new Complex(double.PositiveInfinity, double.NaN)));
            Assert.False(Complex.IsNaN(new Complex(double.NaN, double.PositiveInfinity)));
            Assert.False(Complex.IsNaN(Complex.Infinity));

            VerifyRealImaginaryProperties(Complex.NaN, double.NaN, double.NaN);
            VerifyMagnitudePhaseProperties(Complex.NaN, double.NaN, double.NaN);
        }

        [Fact]
        public static void Infinity()
        {
            Assert.True(Complex.IsInfinity(new Complex(double.PositiveInfinity, double.PositiveInfinity)));
            Assert.True(Complex.IsInfinity(new Complex(1, double.PositiveInfinity)));
            Assert.True(Complex.IsInfinity(new Complex(double.PositiveInfinity, 1)));

            Assert.True(Complex.IsInfinity(new Complex(double.NegativeInfinity, double.NegativeInfinity)));
            Assert.True(Complex.IsInfinity(new Complex(1, double.NegativeInfinity)));
            Assert.True(Complex.IsInfinity(new Complex(double.NegativeInfinity, 1)));

            Assert.True(Complex.IsInfinity(Complex.Infinity));
            Assert.False(Complex.IsInfinity(Complex.NaN));
            

            VerifyRealImaginaryProperties(Complex.Infinity, double.PositiveInfinity, double.PositiveInfinity);
            VerifyMagnitudePhaseProperties(Complex.Infinity, double.PositiveInfinity, Math.PI / 4);
        }

        [Fact]
        public static void Finite()
        {
            Assert.False(Complex.IsFinite(new Complex(double.NaN, double.NaN)));
            Assert.False(Complex.IsFinite(new Complex(1, double.NaN)));
            Assert.False(Complex.IsFinite(new Complex(double.NaN, 1)));

            Assert.False(Complex.IsFinite(new Complex(double.PositiveInfinity, double.PositiveInfinity)));
            Assert.False(Complex.IsFinite(new Complex(1, double.PositiveInfinity)));
            Assert.False(Complex.IsFinite(new Complex(double.PositiveInfinity, 1)));

            Assert.False(Complex.IsFinite(new Complex(double.NegativeInfinity, double.NegativeInfinity)));
            Assert.False(Complex.IsFinite(new Complex(1, double.NegativeInfinity)));
            Assert.False(Complex.IsFinite(new Complex(double.NegativeInfinity, 1)));

            Assert.False(Complex.IsFinite(Complex.Infinity));
            Assert.False(Complex.IsFinite(Complex.NaN));
            Assert.True(Complex.IsFinite(Complex.ImaginaryOne));
        }
    }
}
