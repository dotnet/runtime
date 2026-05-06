// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public sealed class IFloatingPointTests
    {
        const float baseValue = 5.5f;
        static readonly FloatingPointDimHelper helperValue = new FloatingPointDimHelper(baseValue);

        [Fact]
        public static void AwayFromZeroRoundingTest()
        {
            Assert.Equal(float.Round(baseValue, MidpointRounding.AwayFromZero), FloatingPointHelper<FloatingPointDimHelper>.Round(helperValue, MidpointRounding.AwayFromZero).Value);
        }

        [Fact]
        public static void ToEvenRoundingTest()
        {
            Assert.Equal(float.Round(baseValue, MidpointRounding.ToEven), FloatingPointHelper<FloatingPointDimHelper>.Round(helperValue, MidpointRounding.ToEven).Value);
        }

        [Fact]
        public static void ToNegativeInfinityRoundingTest()
        {
            Assert.Equal(float.Round(baseValue, MidpointRounding.ToNegativeInfinity), FloatingPointHelper<FloatingPointDimHelper>.Round(helperValue, MidpointRounding.ToNegativeInfinity).Value);
        }

        [Fact]
        public static void ToPositiveRoundingTest()
        {
            Assert.Equal(float.Round(baseValue, MidpointRounding.ToPositiveInfinity), FloatingPointHelper<FloatingPointDimHelper>.Round(helperValue, MidpointRounding.ToPositiveInfinity).Value);
        }

        [Fact]
        public static void ToZeroRoundingTest()
        {
            Assert.Equal(float.Round(baseValue, MidpointRounding.ToZero), FloatingPointHelper<FloatingPointDimHelper>.Round(helperValue, MidpointRounding.ToZero).Value);
        }
    }
}
