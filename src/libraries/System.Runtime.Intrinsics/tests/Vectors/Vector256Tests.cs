// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector256Tests
    {
        [Fact]
        public void Vector256ByteShiftLeftTest()
        {
            Vector256<byte> vector = Vector256.Create((byte)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int16ShiftLeftTest()
        {
            Vector256<short> vector = Vector256.Create((short)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int32ShiftLeftTest()
        {
            Vector256<int> vector = Vector256.Create((int)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int64ShiftLeftTest()
        {
            Vector256<long> vector = Vector256.Create((long)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256NIntShiftLeftTest()
        {
            Vector256<nint> vector = Vector256.Create((nint)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<nint>.Count; index++)
            {
                Assert.Equal((nint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256NUIntShiftLeftTest()
        {
            Vector256<nuint> vector = Vector256.Create((nuint)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<nuint>.Count; index++)
            {
                Assert.Equal((nuint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256SByteShiftLeftTest()
        {
            Vector256<sbyte> vector = Vector256.Create((sbyte)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt16ShiftLeftTest()
        {
            Vector256<ushort> vector = Vector256.Create((ushort)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt32ShiftLeftTest()
        {
            Vector256<uint> vector = Vector256.Create((uint)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt64ShiftLeftTest()
        {
            Vector256<ulong> vector = Vector256.Create((ulong)0x01);
            vector = Vector256.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int16ShiftRightArithmeticTest()
        {
            Vector256<short> vector = Vector256.Create(unchecked((short)0x8000));
            vector = Vector256.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal(unchecked((short)0xF800), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int32ShiftRightArithmeticTest()
        {
            Vector256<int> vector = Vector256.Create(unchecked((int)0x80000000));
            vector = Vector256.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal(unchecked((int)0xF8000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int64ShiftRightArithmeticTest()
        {
            Vector256<long> vector = Vector256.Create(unchecked((long)0x8000000000000000));
            vector = Vector256.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal(unchecked((long)0xF800000000000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256NIntShiftRightArithmeticTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector256<nint> vector = Vector256.Create(unchecked((nint)0x8000000000000000));
                vector = Vector256.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector256<nint> vector = Vector256.Create(unchecked((nint)0x80000000));
                vector = Vector256.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF8000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector256SByteShiftRightArithmeticTest()
        {
            Vector256<sbyte> vector = Vector256.Create(unchecked((sbyte)0x80));
            vector = Vector256.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal(unchecked((sbyte)0xF8), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256ByteShiftRightLogicalTest()
        {
            Vector256<byte> vector = Vector256.Create((byte)0x80);
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int16ShiftRightLogicalTest()
        {
            Vector256<short> vector = Vector256.Create(unchecked((short)0x8000));
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int32ShiftRightLogicalTest()
        {
            Vector256<int> vector = Vector256.Create(unchecked((int)0x80000000));
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256Int64ShiftRightLogicalTest()
        {
            Vector256<long> vector = Vector256.Create(unchecked((long)0x8000000000000000));
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256NIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector256<nint> vector = Vector256.Create(unchecked((nint)0x8000000000000000));
                vector = Vector256.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector256<nint> vector = Vector256.Create(unchecked((nint)0x80000000));
                vector = Vector256.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector256NUIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector256<nuint> vector = Vector256.Create(unchecked((nuint)0x8000000000000000));
                vector = Vector256.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector256<nuint> vector = Vector256.Create(unchecked((nuint)0x80000000));
                vector = Vector256.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector256SByteShiftRightLogicalTest()
        {
            Vector256<sbyte> vector = Vector256.Create(unchecked((sbyte)0x80));
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt16ShiftRightLogicalTest()
        {
            Vector256<ushort> vector = Vector256.Create(unchecked((ushort)0x8000));
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt32ShiftRightLogicalTest()
        {
            Vector256<uint> vector = Vector256.Create(0x80000000);
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256UInt64ShiftRightLogicalTest()
        {
            Vector256<ulong> vector = Vector256.Create(0x8000000000000000);
            vector = Vector256.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector256ByteSumTest()
        {
            Vector256<byte> vector = Vector256.Create((byte)0x01);
            Assert.Equal((byte)32, Vector256.Sum(vector));
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
            Assert.Equal((short)16, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256Int32SumTest()
        {
            Vector256<int> vector = Vector256.Create((int)0x01);
            Assert.Equal((int)8, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256Int64SumTest()
        {
            Vector256<long> vector = Vector256.Create((long)0x01);
            Assert.Equal((long)4, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256NIntSumTest()
        {
            Vector256<nint> vector = Vector256.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nint)4, Vector256.Sum(vector));
            }
            else
            {
                Assert.Equal((nint)8, Vector256.Sum(vector));
            }
        }

        [Fact]
        public void Vector256NUIntSumTest()
        {
            Vector256<nuint> vector = Vector256.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nuint)4, Vector256.Sum(vector));
            }
            else
            {
                Assert.Equal((nuint)8, Vector256.Sum(vector));
            }
        }

        [Fact]
        public void Vector256SByteSumTest()
        {
            Vector256<sbyte> vector = Vector256.Create((sbyte)0x01);
            Assert.Equal((sbyte)32, Vector256.Sum(vector));
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
            Assert.Equal((ushort)16, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256UInt32SumTest()
        {
            Vector256<uint> vector = Vector256.Create((uint)0x01);
            Assert.Equal((uint)8, Vector256.Sum(vector));
        }

        [Fact]
        public void Vector256UInt64SumTest()
        {
            Vector256<ulong> vector = Vector256.Create((ulong)0x01);
            Assert.Equal((ulong)4, Vector256.Sum(vector));
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
