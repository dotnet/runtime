// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class VBMathTests
    {
        // Reset seed, without access to implementation internals.
        private void ResetSeed()
        {
            float x = BitConverter.ToSingle(BitConverter.GetBytes(0x8076F5C1), 0);
            VBMath.Rnd(x);

            float startupSeed = (float)0x50000;
            float period = 16777216.0f;
            float currentSeed = VBMath.Rnd(0.0f) * period;
            Assert.Equal(startupSeed, currentSeed);
        }

        [Fact]
        public void Rnd_0_RepeatsPreviousNumber()
        {
            ResetSeed();

            float expected;
            float actual;
            foreach (var i in Enumerable.Range(0, 5))
            {
                expected = VBMath.Rnd();
                actual = VBMath.Rnd(0.0f);
                Assert.Equal(expected, actual);
                actual = VBMath.Rnd(0.0f);
                Assert.Equal(expected, actual);
                actual = VBMath.Rnd(0.0f);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Rnd_0_PreviousNumberInSequenceIsNotAlwaysThePreviouslyGeneratedNumber()
        {
            ResetSeed();

            float previouslyGeneratedNumber = VBMath.Rnd();
            VBMath.Randomize(42.0f);
            float actual = VBMath.Rnd(0.0f);
            float expected = 0.251064479f;
            Assert.Equal(expected, actual);
            Assert.NotEqual(previouslyGeneratedNumber, actual);
        }

        [Theory]
        [InlineData(0.5f)]
        [InlineData(1.0f)]
        [InlineData(824000.34f)]
        public void Rnd_Positive_EqualsRndUnit(float positive)
        {
            ResetSeed();
            float actual = VBMath.Rnd(positive);
            ResetSeed();
            float expected = VBMath.Rnd();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Rnd_Unit_ReturnsExpectedSequence()
        {
            ResetSeed();

            float actual1 = VBMath.Rnd();
            float expected1 = 0.7055475f;
            Assert.Equal(expected1, actual1);

            float actual2 = VBMath.Rnd();
            float expected2 = 0.533424f;
            Assert.Equal(expected2, actual2);

            float actual3 = VBMath.Rnd();
            float expected3 = 0.5795186f;
            Assert.Equal(expected3, actual3);

            float actual4 = VBMath.Rnd();
            float expected4 = 0.289562464f;
            Assert.Equal(expected4, actual4);

            float actual5 = VBMath.Rnd();
            float expected5 = 0.301948f;
            Assert.Equal(expected5, actual5);
        }

        [Theory]
        [InlineData(-25820.53f)]
        [InlineData(-66.0f)]
        [InlineData(-2.00008f)]
        public void Rnd_Negative_DoesNotDependUponPreviousState(float number)
        {
            ResetSeed();

            float actual1 = VBMath.Rnd(number);
            VBMath.Rnd();
            float actual2 = VBMath.Rnd(number);
            VBMath.Rnd();
            VBMath.Rnd();
            float actual3 = VBMath.Rnd(number);
            VBMath.Rnd();
            VBMath.Rnd();
            VBMath.Rnd();
            VBMath.Rnd();
            float actual4 = VBMath.Rnd(number);
            Assert.Equal(actual2, actual1);
            Assert.Equal(actual3, actual1);
            Assert.Equal(actual4, actual1);
        }

        [Theory]
        [InlineData(-1.2345E38f, 0.0319542289f)]
        [InlineData(-8000000000.0f, 0.7537669f)]
        [InlineData(-79.4f, 0.828889251f)]
        [InlineData(-44.48306f, 0.5418172f)]
        public void Rnd_Negative_ReturnsExpected(float input, float expected)
        {
            ResetSeed();
            float actual = VBMath.Rnd(input);
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> Randomize_TestData()
        {
            yield return new object[] { -44.8 };
            yield return new object[] { 68.3E10 };
            yield return new object[] { 45782930.2389523 };
            yield return new object[] { 32452834095829304624.435 };
        }

        [Theory]
        [MemberData(nameof(Randomize_TestData))]
        public void Randomize_IsIdempotent(double seed)
        {
            ResetSeed();

            VBMath.Randomize(seed);
            float actualState1 = VBMath.Rnd(0.0f);
            VBMath.Randomize(seed);
            float actualState2 = VBMath.Rnd(0.0f);
            Assert.Equal(actualState1, actualState2);
        }

        [Theory]
        [MemberData(nameof(Randomize_TestData))]
        public void Randomize_UseExistingStateWhenGeneratingNewState(double seed)
        {
            ResetSeed();

            VBMath.Randomize(seed);
            float actualState1 = VBMath.Rnd(0.0f);

            VBMath.Rnd();
            VBMath.Randomize(seed);
            float actualState2 = VBMath.Rnd(0.0f);

            Assert.NotEqual(actualState1, actualState2);
        }

        [Fact]
        public void Randomize_SetsExpectedState()
        {
            ResetSeed();

            VBMath.Randomize(-2E30);
            Assert.Equal(-0.0297851562f, VBMath.Rnd(0.0f));
            VBMath.Randomize(-0.003356);
            Assert.Equal(-0.244613647f, VBMath.Rnd(0.0f));
            VBMath.Randomize(0.0);
            Assert.Equal(-1.0f, VBMath.Rnd(0.0f));
            VBMath.Randomize(10.12345678901);
            Assert.Equal(-0.503646851f, VBMath.Rnd(0.0f));
            VBMath.Randomize(3.5356E99);
            Assert.Equal(-0.462493896f, VBMath.Rnd(0.0f));
        }
    }
}
