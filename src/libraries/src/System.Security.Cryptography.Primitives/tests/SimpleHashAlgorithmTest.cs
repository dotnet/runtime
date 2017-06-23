// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Test.IO.Streams;
using Xunit;

namespace System.Security.Cryptography.Hashing.Tests
{
    public class SimpleHashAlgorithmTest
    {
        private static readonly byte[] s_tenBytes = new byte[10];

        [Fact]
        public void EmptyArrayHash()
        {
            ArrayHash(new byte[0]);
        }

        [Fact]
        public void NonEmptyArrayHash()
        {
            ArrayHash(s_tenBytes);
        }

        [Fact]
        public void LargeArrayHash()
        {
            ArrayHash(new byte[32768]);
        }

        [Fact]
        public void ZeroCountHash()
        {
            ArrayHash(s_tenBytes, 0, 0);
        }

        [Fact]
        public void NonZeroCountHash()
        {
            ArrayHash(s_tenBytes, 0, 5);
        }

        [Fact]
        public void LargeLimitedArrayHash()
        {
            ArrayHash(new byte[32768], 16384, 16384);
        }

        [Fact]
        public void EmptyStreamHash()
        {
            StreamHash(0);
        }

        [Fact]
        public void StreamHash()
        {
            // This value is a completely arbitrary non-power-of-two.
            StreamHash(82);
        }

        [Fact]
        public void LargeStreamHash()
        {
            StreamHash(1048576);
        }

        [Fact]
        public void ValidateStreamContents()
        {
            byte[] result;
            const int ByteCount = 1026;

            using (var stream = new PositionValueStream(ByteCount))
            using (HashAlgorithm hash = new Sum32Hash())
            {
                result = hash.ComputeHash(stream);
            }

            AssertCorrectAnswer(ExpectedSum(0, ByteCount), result);
        }

        [Fact]
        public void ValidateOffset_0()
        {
            // HashCount should not be a multiple of 256, otherwise it can't
            // catch offset problems through Sum32 due to the data being cyclic.
            ValidateOffset(2048, 0, 1500);
        }

        [Fact]
        public void ValidateOffset_121()
        {
            ValidateOffset(2048, 121, 1500);
        }

        [Fact]
        public void ValidateOffset_255()
        {
            ValidateOffset(2048, 255, 1500);
        }

        [Fact]
        public void HashFullArrayInvalidArguments()
        {
            using (HashAlgorithm hash = new Length32Hash())
            {
                Assert.Throws<ArgumentNullException>(() => hash.ComputeHash((byte[])null));

                hash.Dispose();

                Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(s_tenBytes));
            }
        }

        [Fact]
        public void HashPartialArrayInvalidArguments()
        {
            using (HashAlgorithm hash = new Length32Hash())
            {
                Assert.Throws<ArgumentNullException>(() => hash.ComputeHash((byte[])null, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => hash.ComputeHash(s_tenBytes, -1, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(s_tenBytes, 0, -1));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(s_tenBytes, 0, 11));
                AssertExtensions.Throws<ArgumentException>(null, () => hash.ComputeHash(s_tenBytes, 9, 2));

                hash.Dispose();

                Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(s_tenBytes, 0, 10));
            }
        }

        [Fact]
        public void StreamHashInvalidArguments()
        {
            using (var stream = new PositionValueStream(0))
            using (HashAlgorithm hash = new Length32Hash())
            {
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));

                hash.Dispose();

                Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(stream));
            }
        }

        [Fact]
        public void ClearIsDispose()
        {
            using (var stream = new PositionValueStream(0))
            using (HashAlgorithm hash = new Length32Hash())
            {
                Assert.Throws<NullReferenceException>(() => hash.ComputeHash((Stream)null));

                hash.Clear();

                Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(stream));
            }
        }

        private void ArrayHash(byte[] array)
        {
            // Do not call ArrayHash(byte[], int, int).
            // Here we're verifying ComputeHash(byte[]).
            byte[] result;

            using (HashAlgorithm hash = new Length32Hash())
            {
                result = hash.ComputeHash(array);
            }

            AssertCorrectAnswer((uint)array.Length, result);
        }

        private void ArrayHash(byte[] array, int offset, int count)
        {
            byte[] result;

            using (HashAlgorithm hash = new Length32Hash())
            {
                result = hash.ComputeHash(array, offset, count);
            }

            AssertCorrectAnswer((uint)count, result);
        }

        private void StreamHash(int byteCount)
        {
            byte[] result;

            using (var stream = new PositionValueStream(byteCount))
            using (HashAlgorithm hash = new Length32Hash())
            {
                result = hash.ComputeHash(stream);
            }

            AssertCorrectAnswer((uint)byteCount, result);
        }

        private void ValidateOffset(int arraySize, int hashOffset, int hashCount)
        {
            byte[] input;

            using (var stream = new PositionValueStream(arraySize))
            using (var reader = new BinaryReader(stream))
            {
                input = reader.ReadBytes(arraySize);
            }

            byte[] result;

            using (HashAlgorithm hash = new Sum32Hash())
            {
                result = hash.ComputeHash(input, hashOffset, hashCount);
            }

            uint expectedSum = ExpectedSum(hashOffset, hashCount);

            AssertCorrectAnswer(expectedSum, result);
        }

        private void AssertCorrectAnswer(uint expectedValue, byte[] result)
        {
            Assert.NotNull(result);
            Assert.Equal(sizeof(uint), result.Length);

            uint interpreted = BitConverter.ToUInt32(result, 0);

            Assert.Equal(expectedValue, interpreted);
        }

        private static uint ExpectedSum(int offset, int byteCount)
        {
            // SUM(1..n) => n * (n + 1) / 2.
            // Since our data cycles 0, 1, ..., 254, 255, 0, 1, ... we need to break it down
            // into chunks.
            unchecked
            {
                uint max = (uint)byteCount + (uint)offset;
                uint skippedCycles = (uint)offset / 256;
                uint fullCycles = max / 256;
                uint partialCycleMax = unchecked((byte)max);
                uint partialCycleSkipped = unchecked((byte)offset);

                // For the partial cycles, reduce the value by 1 because the sum is 0..(n-1), not 1..n

                if (partialCycleMax > 0)
                {
                    partialCycleMax--;
                }

                if (partialCycleSkipped > 0)
                {
                    partialCycleSkipped--;
                }

                const uint FullCycleValue = 255 * 256 / 2;

                uint accum = (fullCycles - skippedCycles) * FullCycleValue;
                accum += partialCycleMax * (partialCycleMax + 1) / 2;
                accum -= partialCycleSkipped * (partialCycleSkipped + 1) / 2;

                return accum;
            }
        }
    }
}
