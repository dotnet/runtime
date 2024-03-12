// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector512Tests
    {
        /// <summary>Verifies that two <see cref="Vector512{Single}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector512<float> expected, Vector512<float> actual, Vector512<float> variance)
        {
            Vector256Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector256Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        /// <summary>Verifies that two <see cref="Vector512{Double}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector512<double> expected, Vector512<double> actual, Vector512<double> variance)
        {
            Vector256Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector256Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        [Fact]
        public unsafe void Vector512IsHardwareAcceleratedTest()
        {
            MethodInfo methodInfo = typeof(Vector512).GetMethod("get_IsHardwareAccelerated");
            Assert.Equal(Vector512.IsHardwareAccelerated, methodInfo.Invoke(null, null));
        }

        [Fact]
        public unsafe void Vector512ByteExtractMostSignificantBitsTest()
        {
            Vector512<byte> vector = Vector512.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512DoubleExtractMostSignificantBitsTest()
        {
            Vector512<double> vector = Vector512.Create(
                +1.0,
                -0.0,
                +1.0,
                -0.0,
                +1.0,
                -0.0,
                +1.0,
                -0.0
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int16ExtractMostSignificantBitsTest()
        {
            Vector512<short> vector = Vector512.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            ).AsInt16();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int32ExtractMostSignificantBitsTest()
        {
            Vector512<int> vector = Vector512.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            ).AsInt32();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512Int64ExtractMostSignificantBitsTest()
        {
            Vector512<long> vector = Vector512.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            ).AsInt64();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010_1010UL, result);
        }

        [Fact]
        public unsafe void Vector512NIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010UL, result);
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010_10101010UL, result);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nuint> vector = Vector512.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNUInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010UL, result);
            }
            else
            {
                Vector512<nuint> vector = Vector512.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNUInt();

                ulong result = Vector512.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010_10101010UL, result);
            }
        }

        [Fact]
        public unsafe void Vector512SByteExtractMostSignificantBitsTest()
        {
            Vector512<sbyte> vector = Vector512.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            ).AsSByte();

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512SingleExtractMostSignificantBitsTest()
        {
            Vector512<float> vector = Vector512.Create(
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt16ExtractMostSignificantBitsTest()
        {
            Vector512<ushort> vector = Vector512.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt32ExtractMostSignificantBitsTest()
        {
            Vector512<uint> vector = Vector512.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512UInt64ExtractMostSignificantBitsTest()
        {
            Vector512<ulong> vector = Vector512.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            );

            ulong result = Vector512.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010UL, result);
        }

        [Fact]
        public unsafe void Vector512ByteLoadTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.Load(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.Load(value);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512<byte> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<double> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512<short> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<int> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<long> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512<nint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512<nuint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512<sbyte> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<float> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512<ushort> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512<uint> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512<ulong> vector = Vector512.LoadAligned(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512<byte> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<double> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512<short> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<int> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<long> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512<nint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512<nuint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512<sbyte> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<float> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512<ushort> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512<uint> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512<ulong> vector = Vector512.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)index, vector.GetElement(index));
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadUnsafeTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadUnsafeTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadUnsafeTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadUnsafeTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadUnsafeTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadUnsafeTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadUnsafeTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadUnsafeTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadUnsafeTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteLoadUnsafeIndexTest()
        {
            byte* value = stackalloc byte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512<byte> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512DoubleLoadUnsafeIndexTest()
        {
            double* value = stackalloc double[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<double> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int16LoadUnsafeIndexTest()
        {
            short* value = stackalloc short[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (short)(index);
            }

            Vector512<short> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int32LoadUnsafeIndexTest()
        {
            int* value = stackalloc int[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<int> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512Int64LoadUnsafeIndexTest()
        {
            long* value = stackalloc long[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<long> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512NIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512<nint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512<nuint> vector = Vector512.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteLoadUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512<sbyte> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512SingleLoadUnsafeIndexTest()
        {
            float* value = stackalloc float[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512<float> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt16LoadUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512<ushort> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt32LoadUnsafeIndexTest()
        {
            uint* value = stackalloc uint[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512<uint> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512UInt64LoadUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512<ulong> vector = Vector512.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShiftLeftTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftLeftTest()
        {
            Vector512<short> vector = Vector512.Create((short)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftLeftTest()
        {
            Vector512<int> vector = Vector512.Create((int)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftLeftTest()
        {
            Vector512<long> vector = Vector512.Create((long)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftLeftTest()
        {
            Vector512<nint> vector = Vector512.Create((nint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<nint>.Count; index++)
            {
                Assert.Equal((nint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NUIntShiftLeftTest()
        {
            Vector512<nuint> vector = Vector512.Create((nuint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<nuint>.Count; index++)
            {
                Assert.Equal((nuint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShiftLeftTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShiftLeftTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShiftLeftTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShiftLeftTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)0x01);
            vector = Vector512.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftRightArithmeticTest()
        {
            Vector512<short> vector = Vector512.Create(unchecked((short)0x8000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal(unchecked((short)0xF800), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftRightArithmeticTest()
        {
            Vector512<int> vector = Vector512.Create(unchecked((int)0x80000000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal(unchecked((int)0xF8000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftRightArithmeticTest()
        {
            Vector512<long> vector = Vector512.Create(unchecked((long)0x8000000000000000));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal(unchecked((long)0xF800000000000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftRightArithmeticTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x8000000000000000));
                vector = Vector512.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x80000000));
                vector = Vector512.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF8000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512SByteShiftRightArithmeticTest()
        {
            Vector512<sbyte> vector = Vector512.Create(unchecked((sbyte)0x80));
            vector = Vector512.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal(unchecked((sbyte)0xF8), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShiftRightLogicalTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x80);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShiftRightLogicalTest()
        {
            Vector512<short> vector = Vector512.Create(unchecked((short)0x8000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShiftRightLogicalTest()
        {
            Vector512<int> vector = Vector512.Create(unchecked((int)0x80000000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShiftRightLogicalTest()
        {
            Vector512<long> vector = Vector512.Create(unchecked((long)0x8000000000000000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512NIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x8000000000000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nint> vector = Vector512.Create(unchecked((nint)0x80000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512NUIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector512<nuint> vector = Vector512.Create(unchecked((nuint)0x8000000000000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector512<nuint> vector = Vector512.Create(unchecked((nuint)0x80000000));
                vector = Vector512.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector512SByteShiftRightLogicalTest()
        {
            Vector512<sbyte> vector = Vector512.Create(unchecked((sbyte)0x80));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShiftRightLogicalTest()
        {
            Vector512<ushort> vector = Vector512.Create(unchecked((ushort)0x8000));
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShiftRightLogicalTest()
        {
            Vector512<uint> vector = Vector512.Create(0x80000000);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShiftRightLogicalTest()
        {
            Vector512<ulong> vector = Vector512.Create(0x8000000000000000);
            vector = Vector512.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0)
            );

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<byte>.Count; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - (index - Vector128<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count - Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - Vector128<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count + Vector128<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count - Vector128<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<double>.Count; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - (index - Vector128<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count - Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - Vector128<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count + Vector128<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count - Vector128<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<short>.Count; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - (index - Vector128<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count - Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - Vector128<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count + Vector128<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count - Vector128<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<int>.Count; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - (index - Vector128<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count - Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - Vector128<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count + Vector128<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count - Vector128<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<long>.Count; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - (index - Vector128<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count - Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - Vector128<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count + Vector128<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count - Vector128<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48)
            );

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<sbyte>.Count; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - (index - Vector128<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count - Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - Vector128<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count + Vector128<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count - Vector128<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<float>.Count; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - (index - Vector128<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count - Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - Vector128<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count + Vector128<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count - Vector128<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8, 23, 22, 21, 20, 19, 18, 17, 16, 31, 30, 29, 28, 27, 26, 25, 24)
            );

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ushort>.Count; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - (index - Vector128<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count - Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - Vector128<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count + Vector128<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count - Vector128<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12)
            );

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<uint>.Count; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - (index - Vector128<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count - Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - Vector128<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count + Vector128<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count - Vector128<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorAndNoCross128BitLaneTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)1, 0, 3, 2, 5, 4, 7, 6)
            );

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector128<ulong>.Count; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - (index - Vector128<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count - Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - Vector128<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count + Vector128<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count - Vector128<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<byte> result = Vector512.Shuffle(
                Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((byte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector256<byte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<byte>.Count; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - (index - Vector256<byte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<double> result = Vector512.Shuffle(
                Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(Vector256<double>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<double>.Count; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - (index - Vector256<double>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<short> result = Vector512.Shuffle(
                Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((short)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(Vector256<short>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<short>.Count; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - (index - Vector256<short>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<int> result = Vector512.Shuffle(
                Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(Vector256<int>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<int>.Count; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - (index - Vector256<int>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<long> result = Vector512.Shuffle(
                Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((long)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(Vector256<long>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<long>.Count; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - (index - Vector256<long>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<sbyte> result = Vector512.Shuffle(
                Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64),
                Vector512.Create((sbyte)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32)
            );

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector256<sbyte>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<sbyte>.Count; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - (index - Vector256<sbyte>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<float> result = Vector512.Shuffle(
                Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((int)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(Vector256<float>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<float>.Count; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - (index - Vector256<float>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ushort> result = Vector512.Shuffle(
                Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32),
                Vector512.Create((ushort)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16)
            );

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector256<ushort>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ushort>.Count; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - (index - Vector256<ushort>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<uint> result = Vector512.Shuffle(
                Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16),
                Vector512.Create((uint)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8)
            );

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector256<uint>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<uint>.Count; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - (index - Vector256<uint>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithDirectVectorAndNoCross256BitLaneTest()
        {
            Vector512<ulong> result = Vector512.Shuffle(
                Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create((ulong)3, 2, 1, 0, 7, 6, 5, 4)
            );

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector256<ulong>.Count - index), result.GetElement(index));
            }

            for (int index = Vector256<ulong>.Count; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - (index - Vector256<ulong>.Count)), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> indices = Vector512.Create((byte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<byte> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector512<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<double> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)(Vector512<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> indices = Vector512.Create((short)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<short> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)(Vector512<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<int> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)(Vector512<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> indices = Vector512.Create((long)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<long> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)(Vector512<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> indices = Vector512.Create((sbyte)63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<sbyte> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector512<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> indices = Vector512.Create((int)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<float> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)(Vector512<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> indices = Vector512.Create((ushort)31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ushort> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector512<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> indices = Vector512.Create((uint)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<uint> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector512<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> indices = Vector512.Create((ulong)7, 6, 5, 4, 3, 2, 1, 0);
            Vector512<ulong> result = Vector512.Shuffle(vector, indices);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector512<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512<byte>.AllBitsSet);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512<long>.AllBitsSet);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512<short>.AllBitsSet);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512<int>.AllBitsSet);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512<long>.AllBitsSet);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512<sbyte>.AllBitsSet);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512<int>.AllBitsSet);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512<ushort>.AllBitsSet);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512<uint>.AllBitsSet);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512<ulong>.AllBitsSet);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512ByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<byte> result = Vector512.Shuffle(vector, Vector512<byte>.Zero);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512DoubleShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<double> vector = Vector512.Create((double)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<double> result = Vector512.Shuffle(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<short> vector = Vector512.Create((short)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<short> result = Vector512.Shuffle(vector, Vector512<short>.Zero);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<int> vector = Vector512.Create((int)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<int> result = Vector512.Shuffle(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512Int64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<long> vector = Vector512.Create((long)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<long> result = Vector512.Shuffle(vector, Vector512<long>.Zero);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64);
            Vector512<sbyte> result = Vector512.Shuffle(vector, Vector512<sbyte>.Zero);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512SingleShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<float> vector = Vector512.Create((float)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<float> result = Vector512.Shuffle(vector, Vector512<int>.Zero);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> result = Vector512.Shuffle(vector, Vector512<ushort>.Zero);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector512<uint> result = Vector512.Shuffle(vector, Vector512<uint>.Zero);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector512UInt64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)1, 2, 3, 4, 5, 6, 7, 8);
            Vector512<ulong> result = Vector512.Shuffle(vector, Vector512<ulong>.Zero);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)1, result.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).Store(value);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).Store(value);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).Store(value);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).Store(value);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).Store(value);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).Store(value);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).Store(value);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).Store(value);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).Store(value);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).Store(value);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512.Create((byte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((double)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512.Create((short)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((int)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((long)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512.Create((nint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512.Create((nuint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512.Create((sbyte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((float)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512.Create((ushort)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512.Create((uint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512.Create((ulong)0x1).StoreAligned(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (byte)(index);
                }

                Vector512.Create((byte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<byte>.Count; index++)
                {
                    Assert.Equal((byte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((double)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<double>.Count; index++)
                {
                    Assert.Equal((double)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (short)(index);
                }

                Vector512.Create((short)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<short>.Count; index++)
                {
                    Assert.Equal((short)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((int)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<int>.Count; index++)
                {
                    Assert.Equal((int)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((long)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<long>.Count; index++)
                {
                    Assert.Equal((long)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = index;
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = index;
                    }
                }

                Vector512.Create((nint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                if (Environment.Is64BitProcess)
                {
                    for (int index = 0; index < 8; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }
                else
                {
                    for (int index = 0; index < 16; index++)
                    {
                        value[index] = (nuint)(index);
                    }
                }

                Vector512.Create((nuint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 64; index++)
                {
                    value[index] = (sbyte)(index);
                }

                Vector512.Create((sbyte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<sbyte>.Count; index++)
                {
                    Assert.Equal((sbyte)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((float)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<float>.Count; index++)
                {
                    Assert.Equal((float)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 32; index++)
                {
                    value[index] = (ushort)(index);
                }

                Vector512.Create((ushort)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ushort>.Count; index++)
                {
                    Assert.Equal((ushort)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (uint)(index);
                }

                Vector512.Create((uint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<uint>.Count; index++)
                {
                    Assert.Equal((uint)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 64, alignment: 64);

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (ulong)(index);
                }

                Vector512.Create((ulong)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector512<ulong>.Count; index++)
                {
                    Assert.Equal((ulong)0x1, value[index]);
                }
            }
            finally
            {
                NativeMemory.AlignedFree(value);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreUnsafeTest()
        {
            byte* value = stackalloc byte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreUnsafeTest()
        {
            double* value = stackalloc double[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreUnsafeTest()
        {
            short* value = stackalloc short[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreUnsafeTest()
        {
            int* value = stackalloc int[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreUnsafeTest()
        {
            long* value = stackalloc long[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8];

                for (int index = 0; index < 8; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16];

                for (int index = 0; index < 16; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[64];

            for (int index = 0; index < 64; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreUnsafeTest()
        {
            float* value = stackalloc float[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreUnsafeTest()
        {
            ushort* value = stackalloc ushort[32];

            for (int index = 0; index < 32; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreUnsafeTest()
        {
            uint* value = stackalloc uint[16];

            for (int index = 0; index < 16; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreUnsafeTest()
        {
            ulong* value = stackalloc ulong[8];

            for (int index = 0; index < 8; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector512ByteStoreUnsafeIndexTest()
        {
            byte* value = stackalloc byte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (byte)(index);
            }

            Vector512.Create((byte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512DoubleStoreUnsafeIndexTest()
        {
            double* value = stackalloc double[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((double)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int16StoreUnsafeIndexTest()
        {
            short* value = stackalloc short[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (short)(index);
            }

            Vector512.Create((short)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int32StoreUnsafeIndexTest()
        {
            int* value = stackalloc int[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((int)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512Int64StoreUnsafeIndexTest()
        {
            long* value = stackalloc long[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((long)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512NIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
            else
            {
                nint* value = stackalloc nint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = index;
                }

                Vector512.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512NUIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[8 + 1];

                for (int index = 0; index < 8 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[16 + 1];

                for (int index = 0; index < 16 + 1; index++)
                {
                    value[index] = (nuint)(index);
                }

                Vector512.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector512<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector512SByteStoreUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[64 + 1];

            for (int index = 0; index < 64 + 1; index++)
            {
                value[index] = (sbyte)(index);
            }

            Vector512.Create((sbyte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512SingleStoreUnsafeIndexTest()
        {
            float* value = stackalloc float[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = index;
            }

            Vector512.Create((float)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt16StoreUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[32 + 1];

            for (int index = 0; index < 32 + 1; index++)
            {
                value[index] = (ushort)(index);
            }

            Vector512.Create((ushort)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt32StoreUnsafeIndexTest()
        {
            uint* value = stackalloc uint[16 + 1];

            for (int index = 0; index < 16 + 1; index++)
            {
                value[index] = (uint)(index);
            }

            Vector512.Create((uint)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector512UInt64StoreUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[8 + 1];

            for (int index = 0; index < 8 + 1; index++)
            {
                value[index] = (ulong)(index);
            }

            Vector512.Create((ulong)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector512<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index + 1]);
            }
        }

        [Fact]
        public void Vector512ByteSumTest()
        {
            Vector512<byte> vector = Vector512.Create((byte)0x01);
            Assert.Equal((byte)64, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512DoubleSumTest()
        {
            Vector512<double> vector = Vector512.Create((double)0x01);
            Assert.Equal(8.0, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int16SumTest()
        {
            Vector512<short> vector = Vector512.Create((short)0x01);
            Assert.Equal((short)32, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int32SumTest()
        {
            Vector512<int> vector = Vector512.Create((int)0x01);
            Assert.Equal((int)16, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512Int64SumTest()
        {
            Vector512<long> vector = Vector512.Create((long)0x01);
            Assert.Equal((long)8, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512NIntSumTest()
        {
            Vector512<nint> vector = Vector512.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nint)8, Vector512.Sum(vector));
            }
            else
            {
                Assert.Equal((nint)16, Vector512.Sum(vector));
            }
        }

        [Fact]
        public void Vector512NUIntSumTest()
        {
            Vector512<nuint> vector = Vector512.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nuint)8, Vector512.Sum(vector));
            }
            else
            {
                Assert.Equal((nuint)16, Vector512.Sum(vector));
            }
        }

        [Fact]
        public void Vector512SByteSumTest()
        {
            Vector512<sbyte> vector = Vector512.Create((sbyte)0x01);
            Assert.Equal((sbyte)64, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512SingleSumTest()
        {
            Vector512<float> vector = Vector512.Create((float)0x01);
            Assert.Equal(16.0f, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt16SumTest()
        {
            Vector512<ushort> vector = Vector512.Create((ushort)0x01);
            Assert.Equal((ushort)32, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt32SumTest()
        {
            Vector512<uint> vector = Vector512.Create((uint)0x01);
            Assert.Equal((uint)16, Vector512.Sum(vector));
        }

        [Fact]
        public void Vector512UInt64SumTest()
        {
            Vector512<ulong> vector = Vector512.Create((ulong)0x01);
            Assert.Equal((ulong)8, Vector512.Sum(vector));
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
        [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
        [InlineData(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15)]
        [InlineData(0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue, 0, 0, 50, 430, -64, 0, int.MaxValue, int.MinValue)]
        public void Vector512Int32IndexerTest(params int[] values)
        {
            var vector = Vector512.Create(values);

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
        [InlineData(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L)]
        [InlineData(1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(0L, 0L, 50L, 430L, -64L, 0L, long.MaxValue, long.MinValue)]
        public void Vector512Int64IndexerTest(params long[] values)
        {
            var vector = Vector512.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
            Assert.Equal(vector[2], values[2]);
            Assert.Equal(vector[3], values[3]);
        }

        [Fact]
        public void Vector512DoubleEqualsNaNTest()
        {
            Vector512<double> nan = Vector512.Create(double.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector512SingleEqualsNaNTest()
        {
            Vector512<float> nan = Vector512.Create(float.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector512DoubleEqualsNonCanonicalNaNTest()
        {
            // max 8 bit exponent, just under half max mantissa
            var snan = BitConverter.UInt64BitsToDouble(0x7FF7_FFFF_FFFF_FFFF);
            var nans = new double[]
            {
                double.CopySign(double.NaN, -0.0), // -qnan same as double.NaN
                double.CopySign(double.NaN, +0.0), // +qnan
                double.CopySign(snan, -0.0),       // -snan
                double.CopySign(snan, +0.0),       // +snan
            };

            // all Vector<double> NaNs .Equals compare the same, but == compare as different
            foreach(var i in nans)
            {
                foreach(var j in nans)
                {
                    Assert.True(Vector512.Create(i).Equals(Vector512.Create(j)));
                    Assert.False(Vector512.Create(i) == Vector512.Create(j));
                }
            }
        }

        [Fact]
        public void Vector512SingleEqualsNonCanonicalNaNTest()
        {
            // max 11 bit exponent, just under half max mantissa
            var snan = BitConverter.UInt32BitsToSingle(0x7FBF_FFFF);
            var nans = new float[]
            {
                float.CopySign(float.NaN, -0.0f), // -qnan same as float.NaN
                float.CopySign(float.NaN, +0.0f), // +qnan
                float.CopySign(snan, -0.0f),      // -snan
                float.CopySign(snan, +0.0f),      // +snan
            };

            // all Vector<float> NaNs .Equals compare the same, but == compare as different
            foreach(var i in nans)
            {
                foreach(var j in nans)
                {
                    Assert.True(Vector512.Create(i).Equals(Vector512.Create(j)));
                    Assert.False(Vector512.Create(i) == Vector512.Create(j));
                }
            }
        }

        [Fact]
        public void Vector512SingleCreateFromArrayTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f];
            Vector512<float> vector = Vector512.Create(array);
            Assert.Equal(Vector512.Create(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f), vector);
        }

        [Fact]
        public void Vector512SingleCreateFromArrayOffsetTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f];
            Vector512<float> vector = Vector512.Create(array, 1);
            Assert.Equal(Vector512.Create(2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f, 16.0f, 17.0f), vector);
        }

        [Fact]
        public void Vector512SingleCopyToTest()
        {
            float[] array = new float[16];
            Vector512.Create(2.0f).CopyTo(array);
            Assert.True(array.AsSpan().SequenceEqual([2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector512SingleCopyToOffsetTest()
        {
            float[] array = new float[17];
            Vector512.Create(2.0f).CopyTo(array, 1);
            Assert.True(array.AsSpan().SequenceEqual([0.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector512SByteAbs_MinValue()
        {
            Vector512<sbyte> vector = Vector512.Create(sbyte.MinValue);
            Vector512<sbyte> abs = Vector512.Abs(vector);
            for (int index = 0; index < Vector512<sbyte>.Count; index++)
            {
                Assert.Equal(sbyte.MinValue, vector.GetElement(index));
            }
        }

        [Fact]
        public void IsSupportedByte() => TestIsSupported<byte>();

        [Fact]
        public void IsSupportedDouble() => TestIsSupported<double>();

        [Fact]
        public void IsSupportedInt16() => TestIsSupported<short>();

        [Fact]
        public void IsSupportedInt32() => TestIsSupported<int>();

        [Fact]
        public void IsSupportedInt64() => TestIsSupported<long>();

        [Fact]
        public void IsSupportedIntPtr() => TestIsSupported<nint>();

        [Fact]
        public void IsSupportedSByte() => TestIsSupported<sbyte>();

        [Fact]
        public void IsSupportedSingle() => TestIsSupported<float>();

        [Fact]
        public void IsSupportedUInt16() => TestIsSupported<ushort>();

        [Fact]
        public void IsSupportedUInt32() => TestIsSupported<uint>();

        [Fact]
        public void IsSupportedUInt64() => TestIsSupported<ulong>();

        [Fact]
        public void IsSupportedUIntPtr() => TestIsSupported<nuint>();

        private static void TestIsSupported<T>()
            where T : struct
        {
            Assert.True(Vector512<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector512<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.True((bool)methodInfo.Invoke(null, null));
        }

        [Fact]
        public void IsNotSupportedBoolean() => TestIsNotSupported<bool>();

        [Fact]
        public void IsNotSupportedChar() => TestIsNotSupported<char>();

        [Fact]
        public void IsNotSupportedHalf() => TestIsNotSupported<Half>();

        [Fact]
        public void IsNotSupportedInt128() => TestIsNotSupported<Int128>();

        [Fact]
        public void IsNotSupportedUInt128() => TestIsNotSupported<UInt128>();

        private static void TestIsNotSupported<T>()
            where T : struct
        {
            Assert.False(Vector512<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector512<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.False((bool)methodInfo.Invoke(null, null));
        }

        [Fact]
        public void GetIndicesByteTest() => TestGetIndices<byte>();

        [Fact]
        public void GetIndicesDoubleTest() => TestGetIndices<double>();

        [Fact]
        public void GetIndicesInt16Test() => TestGetIndices<short>();

        [Fact]
        public void GetIndicesInt32Test() => TestGetIndices<int>();

        [Fact]
        public void GetIndicesInt64Test() => TestGetIndices<long>();

        [Fact]
        public void GetIndicesNIntTest() => TestGetIndices<nint>();

        [Fact]
        public void GetIndicesNUIntTest() => TestGetIndices<nuint>();

        [Fact]
        public void GetIndicesSByteTest() => TestGetIndices<sbyte>();

        [Fact]
        public void GetIndicesSingleTest() => TestGetIndices<float>();

        [Fact]
        public void GetIndicesUInt16Test() => TestGetIndices<ushort>();

        [Fact]
        public void GetIndicesUInt32Test() => TestGetIndices<uint>();

        [Fact]
        public void GetIndicesUInt64Test() => TestGetIndices<ulong>();

        private static void TestGetIndices<T>()
            where T : INumber<T>
        {
            Vector512<T> indices = Vector512<T>.Indices;

            for (int index = 0; index < Vector512<T>.Count; index++)
            {
                Assert.Equal(T.CreateTruncating(index), indices.GetElement(index));
            }
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(63, unchecked((byte)(-1)))]
        public void CreateSequenceByteTest(byte start, byte step) => TestCreateSequence<byte>(start, step);

        [Theory]
        [InlineData(0.0, +2.0)]
        [InlineData(3.0, +3.0)]
        [InlineData(7.0, -1.0)]
        public void CreateSequenceDoubleTest(double start, double step) => TestCreateSequence<double>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(31, -1)]
        public void CreateSequenceInt16Test(short start, short step) => TestCreateSequence<short>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(15, -1)]
        public void CreateSequenceInt32Test(int start, int step) => TestCreateSequence<int>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(31, -1)]
        public void CreateSequenceInt64Test(long start, long step) => TestCreateSequence<long>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(63, -1)]
        public void CreateSequenceSByteTest(sbyte start, sbyte step) => TestCreateSequence<sbyte>(start, step);

        [Theory]
        [InlineData(0.0f, +2.0f)]
        [InlineData(3.0f, +3.0f)]
        [InlineData(15.0f, -1.0f)]
        public void CreateSequenceSingleTest(float start, float step) => TestCreateSequence<float>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(31, unchecked((ushort)(-1)))]
        public void CreateSequenceUInt16Test(ushort start, ushort step) => TestCreateSequence<ushort>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(15, unchecked((uint)(-1)))]
        public void CreateSequenceUInt32Test(uint start, uint step) => TestCreateSequence<uint>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(7, unchecked((ulong)(-1)))]
        public void CreateSequenceUInt64Test(ulong start, ulong step) => TestCreateSequence<ulong>(start, step);

        private static void TestCreateSequence<T>(T start, T step)
            where T : INumber<T>
        {
            Vector512<T> sequence = Vector512.CreateSequence(start, step);
            T expected = start;

            for (int index = 0; index < Vector512<T>.Count; index++)
            {
                Assert.Equal(expected, sequence.GetElement(index));
                expected += step;
            }
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.ExpDouble), MemberType = typeof(VectorTestMemberData))]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/97176")]
        public void ExpDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Exp(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.ExpSingle), MemberType = typeof(VectorTestMemberData))]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/97176")]
        public void ExpSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Exp(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogDouble), MemberType = typeof(VectorTestMemberData))]
        public void LogDoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Log(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogSingle), MemberType = typeof(VectorTestMemberData))]
        public void LogSingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Log(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Double), MemberType = typeof(VectorTestMemberData))]
        public void Log2DoubleTest(double value, double expectedResult, double variance)
        {
            Vector512<double> actualResult = Vector512.Log2(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Single), MemberType = typeof(VectorTestMemberData))]
        public void Log2SingleTest(float value, float expectedResult, float variance)
        {
            Vector512<float> actualResult = Vector512.Log2(Vector512.Create(value));
            AssertEqual(Vector512.Create(expectedResult), actualResult, Vector512.Create(variance));
        }
    }
}
