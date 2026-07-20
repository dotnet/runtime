// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.Tests
{
    // Confirms a decimal IEEE 754 type is consumable through the generic
    // IDecimalFloatingPointIeee754<TSelf> surface, dispatching every function
    // family through the interface. Accuracy is covered by the per-function tests.
    internal static class GenericIeee754Surface
    {
        public static void Verify<TSelf>()
            where TSelf : IDecimalFloatingPointIeee754<TSelf>
        {
            Assert.True(TSelf.IsNaN(TSelf.NaN));
            Assert.True(TSelf.IsNegative(TSelf.NegativeZero));
            Assert.True(TSelf.IsPositiveInfinity(TSelf.PositiveInfinity));
            Assert.True(TSelf.IsNegativeInfinity(TSelf.NegativeInfinity));
            Assert.True(TSelf.Epsilon > TSelf.Zero);

            TSelf one = TSelf.One;
            TSelf two = one + one;

            Assert.True(TSelf.IsFinite(TSelf.Exp(one)));
            Assert.True(TSelf.IsFinite(TSelf.Exp2(one)));
            Assert.True(TSelf.IsFinite(TSelf.Exp10(one)));
            Assert.True(TSelf.IsFinite(TSelf.Log(TSelf.E)));
            Assert.True(TSelf.IsFinite(TSelf.Log2(two)));
            Assert.True(TSelf.IsFinite(TSelf.Log10(TSelf.E)));
            Assert.True(TSelf.IsFinite(TSelf.Pow(two, two)));
            Assert.True(TSelf.IsFinite(TSelf.Cbrt(two)));
            Assert.True(TSelf.IsFinite(TSelf.Hypot(one, one)));
            Assert.True(TSelf.IsFinite(TSelf.RootN(two, 3)));
            Assert.True(TSelf.IsFinite(TSelf.Sin(one)));
            Assert.True(TSelf.IsFinite(TSelf.Cos(one)));
            Assert.True(TSelf.IsFinite(TSelf.Tan(one)));
            Assert.True(TSelf.IsFinite(TSelf.Atan2(one, one)));
            Assert.True(TSelf.IsFinite(TSelf.Sinh(one)));
            Assert.True(TSelf.IsFinite(TSelf.Cosh(one)));
            Assert.True(TSelf.IsFinite(TSelf.Tanh(one)));
            Assert.True(TSelf.IsFinite(TSelf.Asinh(one)));
            Assert.True(TSelf.IsFinite(TSelf.FusedMultiplyAdd(one, one, one)));
            Assert.True(TSelf.IsFinite(TSelf.ScaleB(one, 1)));
            Assert.Equal(0, TSelf.ILogB(one));

            Assert.Equal(two, TSelf.Lerp(one, two, one));
            Assert.Equal(one, TSelf.Lerp(one, two, TSelf.Zero));

            Assert.Equal(one, TSelf.Quantize(one, one));
            Assert.True(TSelf.IsFinite(TSelf.GetQuantum(one)));
            Assert.True(TSelf.HaveSameQuantum(one, one));
        }
    }
}
