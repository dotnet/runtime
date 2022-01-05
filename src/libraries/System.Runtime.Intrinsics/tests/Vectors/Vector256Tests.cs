// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector256Tests
    {
        [Fact]
        public void Vector256ByteSumTest()
        {
            Vector256<byte> vector = Vector256.Create((byte)0x01);
            Assert.Equal(32, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256DoubleSumTest()
        {
            Vector256<double> vector = Vector256.Create((double)0x01);
            Assert.Equal(4.0, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256Int16SumTest()
        {
            Vector256<short> vector = Vector256.Create((short)0x01);
            Assert.Equal(16, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256Int32SumTest()
        {
            Vector256<int> vector = Vector256.Create((int)0x01);
            Assert.Equal(8, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256Int64SumTest()
        {
            Vector256<long> vector = Vector256.Create((long)0x01);
            Assert.Equal(4, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256NIntSumTest()
        {
            Vector256<nint> vector = Vector256.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(4, Vector256.Sum(vector));
            }
            else
            {
                Assert.Equal(8, Vector256.Sum(vector));
            }
        }

        [Fact]
        public void Vector256NUIntSumTest()
        {
            Vector256<nuint> vector = Vector256.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(4, Vector256.Sum(vector));
            }
            else
            {
                Assert.Equal(8, Vector256.Sum(vector));
            }
        }

        [Fact]
        public void Vector256SByteSumTest()
        {
            Vector256<sbyte> vector = Vector256.Create((sbyte)0x01);
            Assert.Equal(32, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256SingleSumTest()
        {
            Vector256<float> vector = Vector256.Create((float)0x01);
            Assert.Equal(8.0f, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256UInt16SumTest()
        {
            Vector256<ushort> vector = Vector256.Create((ushort)0x01);
            Assert.Equal(16, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256UInt32SumTest()
        {
            Vector256<uint> vector = Vector256.Create((uint)0x01);
            Assert.Equal(8, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256UInt64SumTest()
        {
            Vector256<ulong> vector = Vector256.Create((ulong)0x01);
            Assert.Equal(4, Vector256.Sum(vector));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
        [InlineData(-1, -1, -1, -1, -1, -1, -1, -1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8)]
        [InlineData(0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue)]
        public void Vector256Int32IndexerTest(params int[] values)
        {
            var vector = Vector256.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
            Assert.Equal(vector[4], values[4]);
            Assert.Equal(vector[5], values[5]);
            Assert.Equal(vector[6], values[6]);
            Assert.Equal(vector[7], values[7]);
        }

        [Theory]
        [InlineData(0L, 0L, 0L, 0L)]
        [InlineData(1L, 1L, 1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(0L, 0L, 50L, 430L, -64L, 0L, long.MaxValue, long.MinValue)]
        public void Vector256Int64IndexerTest(params long[] values)
        {
            var vector = Vector256.Create(values);
            
            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
        }
    }
}
