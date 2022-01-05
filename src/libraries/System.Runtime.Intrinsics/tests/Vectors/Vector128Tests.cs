// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector128Tests
    {
        [Fact]
        public void Vector128ByteSumTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)0x01);
            Assert.Equal(16, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128DoubleSumTest()
        {
            Vector128<double> vector = Vector128.Create((double)0x01);
            Assert.Equal(2.0, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128Int16SumTest()
        {
            Vector128<short> vector = Vector128.Create((short)0x01);
            Assert.Equal(8, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128Int32SumTest()
        {
            Vector128<int> vector = Vector128.Create((int)0x01);
            Assert.Equal(4, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128Int64SumTest()
        {
            Vector128<long> vector = Vector128.Create((long)0x01);
            Assert.Equal(2, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128NIntSumTest()
        {
            Vector128<nint> vector = Vector128.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(2, Vector128.Sum(vector));
            }
            else
            {
                Assert.Equal(4, Vector128.Sum(vector));
            }
        }

        [Fact]
        public void Vector128NUIntSumTest()
        {
            Vector128<nuint> vector = Vector128.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(2, Vector128.Sum(vector));
            }
            else
            {
                Assert.Equal(4, Vector128.Sum(vector));
            }
        }

        [Fact]
        public void Vector128SByteSumTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)0x01);
            Assert.Equal(16, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128SingleSumTest()
        {
            Vector128<float> vector = Vector128.Create((float)0x01);
            Assert.Equal(4.0f, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128UInt16SumTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)0x01);
            Assert.Equal(8, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128UInt32SumTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)0x01);
            Assert.Equal(4, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128UInt64SumTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)0x01);
            Assert.Equal(2, Vector128.Sum(vector));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8)]
        [InlineData(50, 430, int.MaxValue, int.MinValue)]
        public void Vector128Int32IndexerTest(params int[] values)
        {
            var vector = Vector128.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
        }

        [Theory]
        [InlineData(0L, 0L)]
        [InlineData(1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(50L, 430L, long.MaxValue, long.MinValue)]
        public void Vector128Int64IndexerTest(params long[] values)
        {
            var vector = Vector128.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
        }
    }
}
