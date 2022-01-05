// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector128Tests
    {
        [Fact]
        public void Vector128ByteShiftLeftTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShiftLeftTest()
        {
            Vector128<short> vector = Vector128.Create((short)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShiftLeftTest()
        {
            Vector128<int> vector = Vector128.Create((int)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShiftLeftTest()
        {
            Vector128<long> vector = Vector128.Create((long)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128NIntShiftLeftTest()
        {
            Vector128<nint> vector = Vector128.Create((nint)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<nint>.Count; index++)
            {
                Assert.Equal((nint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128NUIntShiftLeftTest()
        {
            Vector128<nuint> vector = Vector128.Create((nuint)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<nuint>.Count; index++)
            {
                Assert.Equal((nuint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShiftLeftTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShiftLeftTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShiftLeftTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShiftLeftTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)0x01);
            vector = Vector128.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShiftRightArithmeticTest()
        {
            Vector128<short> vector = Vector128.Create(unchecked((short)0x8000));
            vector = Vector128.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal(unchecked((short)0xF800), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShiftRightArithmeticTest()
        {
            Vector128<int> vector = Vector128.Create(unchecked((int)0x80000000));
            vector = Vector128.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal(unchecked((int)0xF8000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShiftRightArithmeticTest()
        {
            Vector128<long> vector = Vector128.Create(unchecked((long)0x8000000000000000));
            vector = Vector128.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal(unchecked((long)0xF800000000000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128NIntShiftRightArithmeticTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector128<nint> vector = Vector128.Create(unchecked((nint)0x8000000000000000));
                vector = Vector128.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector128<nint> vector = Vector128.Create(unchecked((nint)0x80000000));
                vector = Vector128.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF8000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector128SByteShiftRightArithmeticTest()
        {
            Vector128<sbyte> vector = Vector128.Create(unchecked((sbyte)0x80));
            vector = Vector128.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal(unchecked((sbyte)0xF8), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteShiftRightLogicalTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)0x80);
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShiftRightLogicalTest()
        {
            Vector128<short> vector = Vector128.Create(unchecked((short)0x8000));
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShiftRightLogicalTest()
        {
            Vector128<int> vector = Vector128.Create(unchecked((int)0x80000000));
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShiftRightLogicalTest()
        {
            Vector128<long> vector = Vector128.Create(unchecked((long)0x8000000000000000));
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128NIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector128<nint> vector = Vector128.Create(unchecked((nint)0x8000000000000000));
                vector = Vector128.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector128<nint> vector = Vector128.Create(unchecked((nint)0x80000000));
                vector = Vector128.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector128NUIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector128<nuint> vector = Vector128.Create(unchecked((nuint)0x8000000000000000));
                vector = Vector128.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector128<nuint> vector = Vector128.Create(unchecked((nuint)0x80000000));
                vector = Vector128.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector128SByteShiftRightLogicalTest()
        {
            Vector128<sbyte> vector = Vector128.Create(unchecked((sbyte)0x80));
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShiftRightLogicalTest()
        {
            Vector128<ushort> vector = Vector128.Create(unchecked((ushort)0x8000));
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShiftRightLogicalTest()
        {
            Vector128<uint> vector = Vector128.Create(0x80000000);
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShiftRightLogicalTest()
        {
            Vector128<ulong> vector = Vector128.Create(0x8000000000000000);
            vector = Vector128.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteSumTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)0x01);
            Assert.Equal((byte)16, Vector128.Sum(vector));
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
            Assert.Equal((short)8, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128Int32SumTest()
        {
            Vector128<int> vector = Vector128.Create((int)0x01);
            Assert.Equal((int)4, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128Int64SumTest()
        {
            Vector128<long> vector = Vector128.Create((long)0x01);
            Assert.Equal((long)2, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128NIntSumTest()
        {
            Vector128<nint> vector = Vector128.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nint)2, Vector128.Sum(vector));
            }
            else
            {
                Assert.Equal((nint)4, Vector128.Sum(vector));
            }
        }

        [Fact]
        public void Vector128NUIntSumTest()
        {
            Vector128<nuint> vector = Vector128.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nuint)2, Vector128.Sum(vector));
            }
            else
            {
                Assert.Equal((nuint)4, Vector128.Sum(vector));
            }
        }

        [Fact]
        public void Vector128SByteSumTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)0x01);
            Assert.Equal((sbyte)16, Vector128.Sum(vector));
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
            Assert.Equal((ushort)8, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128UInt32SumTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)0x01);
            Assert.Equal((uint)4, Vector128.Sum(vector));
        }

        [Fact]
        public void Vector128UInt64SumTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)0x01);
            Assert.Equal((ulong)2, Vector128.Sum(vector));
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
