// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class BitStackTests
    {
        private static readonly Random s_random = new Random(42);

        [Fact]
        public static void DefaultBitStack()
        {
            BitStack bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);
        }

        [Fact]
        public static void SetResetFirstBit()
        {
            BitStack bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);
            bitStack.SetFirstBit();
            Assert.Equal(1, bitStack.CurrentDepth);
            Assert.False(bitStack.Pop());
            Assert.Equal(0, bitStack.CurrentDepth);

            bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);
            bitStack.ResetFirstBit();
            Assert.Equal(1, bitStack.CurrentDepth);
            Assert.False(bitStack.Pop());
            Assert.Equal(0, bitStack.CurrentDepth);

            bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);
            bitStack.SetFirstBit();
            Assert.Equal(1, bitStack.CurrentDepth);
            Assert.False(bitStack.Pop());
            Assert.Equal(0, bitStack.CurrentDepth);
            bitStack.ResetFirstBit();
            Assert.Equal(1, bitStack.CurrentDepth);
            Assert.False(bitStack.Pop());
            Assert.Equal(0, bitStack.CurrentDepth);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        [InlineData(256)]
        public static void BitStackPushPop(int bitLength)
        {
            BitStack bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);

            var values = new bool[bitLength];
            for (int i = 0; i < bitLength; i++)
            {
                values[i] = s_random.NextDouble() >= 0.5;
            }

            for (int i = 0; i < bitLength; i++)
            {
                if (values[i])
                {
                    bitStack.PushTrue();
                }
                else
                {
                    bitStack.PushFalse();
                }
                Assert.Equal(i + 1, bitStack.CurrentDepth);
            }

            // Loop backwards when popping.
            for (int i = bitLength - 1; i > 0; i--)
            {
                // We need the value at the top *after* popping off the last one.
                Assert.Equal(values[i - 1], bitStack.Pop());
                Assert.Equal(i, bitStack.CurrentDepth);
            }
        }

        [Theory]
        [OuterLoop]
        [InlineData(3_200_000)]
        [InlineData(int.MaxValue / 32 + 1)]    // 67_108_864
        public static void BitStackPushPopLarge(int bitLength)
        {
            BitStack bitStack = default;
            Assert.Equal(0, bitStack.CurrentDepth);

            var values = new bool[bitLength];
            for (int i = 0; i < bitLength; i++)
            {
                values[i] = s_random.NextDouble() >= 0.5;
            }

            const int IterationCapacity = 1_600_000;

            int expectedDepth = 0;
            // Only set and compare the first and last few (otherwise, the test takes too long)
            for (int i = 0; i < IterationCapacity; i++)
            {
                if (values[i])
                {
                    bitStack.PushTrue();
                }
                else
                {
                    bitStack.PushFalse();
                }
                expectedDepth++;
                Assert.Equal(expectedDepth, bitStack.CurrentDepth);
            }
            for (int i = bitLength - IterationCapacity; i < bitLength; i++)
            {
                if (values[i])
                {
                    bitStack.PushTrue();
                }
                else
                {
                    bitStack.PushFalse();
                }
                expectedDepth++;
                Assert.Equal(expectedDepth, bitStack.CurrentDepth);
            }

            Assert.Equal(expectedDepth, IterationCapacity * 2);

            // Loop backwards when popping.
            for (int i = bitLength - 1; i >= bitLength - IterationCapacity; i--)
            {
                // We need the value at the top *after* popping off the last one.
                Assert.Equal(values[i - 1], bitStack.Pop());

                expectedDepth--;
                Assert.Equal(expectedDepth, bitStack.CurrentDepth);
            }
            for (int i = IterationCapacity - 1; i > 0; i--)
            {
                // We need the value at the top *after* popping off the last one.
                Assert.Equal(values[i - 1], bitStack.Pop());

                expectedDepth--;
                Assert.Equal(expectedDepth, bitStack.CurrentDepth);
            }
        }
    }
}
