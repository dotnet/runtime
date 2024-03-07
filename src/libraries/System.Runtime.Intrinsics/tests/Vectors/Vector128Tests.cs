// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector128Tests
    {
        /// <summary>Verifies that two <see cref="Vector128{Single}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector128<float> expected, Vector128<float> actual, Vector128<float> variance)
        {
            Vector64Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector64Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        /// <summary>Verifies that two <see cref="Vector128{Double}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector128<double> expected, Vector128<double> actual, Vector128<double> variance)
        {
            Vector64Tests.AssertEqual(expected.GetLower(), actual.GetLower(), variance.GetLower());
            Vector64Tests.AssertEqual(expected.GetUpper(), actual.GetUpper(), variance.GetUpper());
        }

        [Fact]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Vector128))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/81785", TestPlatforms.Browser)]
        public unsafe void Vector128IsHardwareAcceleratedTest()
        {
            MethodInfo methodInfo = typeof(Vector128).GetMethod("get_IsHardwareAccelerated");
            Assert.Equal(Vector128.IsHardwareAccelerated, methodInfo.Invoke(null, null));
        }

        [Fact]
        public unsafe void Vector128ByteExtractMostSignificantBitsTest()
        {
            Vector128<byte> vector = Vector128.Create(
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

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector128DoubleExtractMostSignificantBitsTest()
        {
            Vector128<double> vector = Vector128.Create(
                +1.0,
                -0.0
            );

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector128Int16ExtractMostSignificantBitsTest()
        {
            Vector128<short> vector = Vector128.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            ).AsInt16();

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector128Int32ExtractMostSignificantBitsTest()
        {
            Vector128<int> vector = Vector128.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            ).AsInt32();

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector128Int64ExtractMostSignificantBitsTest()
        {
            Vector128<long> vector = Vector128.Create(
                0x0000000000000001UL,
                0x8000000000000000UL
            ).AsInt64();

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector128NIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector128<nint> vector = Vector128.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNInt();

                uint result = Vector128.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10u, result);
            }
            else
            {
                Vector128<nint> vector = Vector128.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNInt();

                uint result = Vector128.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1010u, result);
            }
        }

        [Fact]
        public unsafe void Vector128NUIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector128<nuint> vector = Vector128.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNUInt();

                uint result = Vector128.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10u, result);
            }
            else
            {
                Vector128<nuint> vector = Vector128.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNUInt();

                uint result = Vector128.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1010u, result);
            }
        }

        [Fact]
        public unsafe void Vector128SByteExtractMostSignificantBitsTest()
        {
            Vector128<sbyte> vector = Vector128.Create(
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

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector128SingleExtractMostSignificantBitsTest()
        {
            Vector128<float> vector = Vector128.Create(
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f
            );

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector128UInt16ExtractMostSignificantBitsTest()
        {
            Vector128<ushort> vector = Vector128.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000,
                0x0001,
                0x8000
            );

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector128UInt32ExtractMostSignificantBitsTest()
        {
            Vector128<uint> vector = Vector128.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            );

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector128UInt64ExtractMostSignificantBitsTest()
        {
            Vector128<ulong> vector = Vector128.Create(
                0x0000000000000001UL,
                0x8000000000000000UL
            );

            uint result = Vector128.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector128ByteLoadTest()
        {
            byte* value = stackalloc byte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128<byte> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128DoubleLoadTest()
        {
            double* value = stackalloc double[2] {
                0,
                1,
            };

            Vector128<double> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int16LoadTest()
        {
            short* value = stackalloc short[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128<short> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int32LoadTest()
        {
            int* value = stackalloc int[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<int> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int64LoadTest()
        {
            long* value = stackalloc long[2] {
                0,
                1,
            };

            Vector128<long> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128NIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector128<nint> vector = Vector128.Load(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128<nint> vector = Vector128.Load(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector128<nuint> vector = Vector128.Load(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128<nuint> vector = Vector128.Load(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteLoadTest()
        {
            sbyte* value = stackalloc sbyte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128<sbyte> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128SingleLoadTest()
        {
            float* value = stackalloc float[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<float> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt16LoadTest()
        {
            ushort* value = stackalloc ushort[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128<ushort> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt32LoadTest()
        {
            uint* value = stackalloc uint[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<uint> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt64LoadTest()
        {
            ulong* value = stackalloc ulong[2] {
                0,
                1,
            };

            Vector128<ulong> vector = Vector128.Load(value);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128ByteLoadAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128<byte> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<byte>.Count; index++)
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
        public unsafe void Vector128DoubleLoadAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<double> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<double>.Count; index++)
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
        public unsafe void Vector128Int16LoadAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128<short> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<short>.Count; index++)
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
        public unsafe void Vector128Int32LoadAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<int> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<int>.Count; index++)
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
        public unsafe void Vector128Int64LoadAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<long> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<long>.Count; index++)
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
        public unsafe void Vector128NIntLoadAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128<nint> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
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
        public unsafe void Vector128NUIntLoadAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128<nuint> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
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
        public unsafe void Vector128SByteLoadAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128<sbyte> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<sbyte>.Count; index++)
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
        public unsafe void Vector128SingleLoadAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<float> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<float>.Count; index++)
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
        public unsafe void Vector128UInt16LoadAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128<ushort> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<ushort>.Count; index++)
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
        public unsafe void Vector128UInt32LoadAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<uint> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<uint>.Count; index++)
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
        public unsafe void Vector128UInt64LoadAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<ulong> vector = Vector128.LoadAligned(value);

                for (int index = 0; index < Vector128<ulong>.Count; index++)
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
        public unsafe void Vector128ByteLoadAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128<byte> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<byte>.Count; index++)
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
        public unsafe void Vector128DoubleLoadAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<double> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<double>.Count; index++)
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
        public unsafe void Vector128Int16LoadAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128<short> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<short>.Count; index++)
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
        public unsafe void Vector128Int32LoadAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<int> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<int>.Count; index++)
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
        public unsafe void Vector128Int64LoadAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<long> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<long>.Count; index++)
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
        public unsafe void Vector128NIntLoadAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128<nint> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
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
        public unsafe void Vector128NUIntLoadAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128<nuint> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
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
        public unsafe void Vector128SByteLoadAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128<sbyte> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<sbyte>.Count; index++)
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
        public unsafe void Vector128SingleLoadAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<float> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<float>.Count; index++)
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
        public unsafe void Vector128UInt16LoadAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128<ushort> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<ushort>.Count; index++)
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
        public unsafe void Vector128UInt32LoadAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128<uint> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<uint>.Count; index++)
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
        public unsafe void Vector128UInt64LoadAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128<ulong> vector = Vector128.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<ulong>.Count; index++)
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
        public unsafe void Vector128ByteLoadUnsafeTest()
        {
            byte* value = stackalloc byte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128<byte> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128DoubleLoadUnsafeTest()
        {
            double* value = stackalloc double[2] {
                0,
                1,
            };

            Vector128<double> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int16LoadUnsafeTest()
        {
            short* value = stackalloc short[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128<short> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int32LoadUnsafeTest()
        {
            int* value = stackalloc int[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<int> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int64LoadUnsafeTest()
        {
            long* value = stackalloc long[2] {
                0,
                1,
            };

            Vector128<long> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128NIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector128<nint> vector = Vector128.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128<nint> vector = Vector128.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector128<nuint> vector = Vector128.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128<nuint> vector = Vector128.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteLoadUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128<sbyte> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128SingleLoadUnsafeTest()
        {
            float* value = stackalloc float[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<float> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt16LoadUnsafeTest()
        {
            ushort* value = stackalloc ushort[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128<ushort> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt32LoadUnsafeTest()
        {
            uint* value = stackalloc uint[4] {
                0,
                1,
                2,
                3,
            };

            Vector128<uint> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt64LoadUnsafeTest()
        {
            ulong* value = stackalloc ulong[2] {
                0,
                1,
            };

            Vector128<ulong> vector = Vector128.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128ByteLoadUnsafeIndexTest()
        {
            byte* value = stackalloc byte[16 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
            };

            Vector128<byte> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128DoubleLoadUnsafeIndexTest()
        {
            double* value = stackalloc double[2 + 1] {
                0,
                1,
                2,
            };

            Vector128<double> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int16LoadUnsafeIndexTest()
        {
            short* value = stackalloc short[8 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
            };

            Vector128<short> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int32LoadUnsafeIndexTest()
        {
            int* value = stackalloc int[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128<int> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128Int64LoadUnsafeIndexTest()
        {
            long* value = stackalloc long[2 + 1] {
                0,
                1,
                2,
            };

            Vector128<long> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128NIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector128<nint> vector = Vector128.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector128<nint> vector = Vector128.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector128<nuint> vector = Vector128.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector128<nuint> vector = Vector128.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteLoadUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[16 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
            };

            Vector128<sbyte> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128SingleLoadUnsafeIndexTest()
        {
            float* value = stackalloc float[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128<float> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt16LoadUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[8 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
            };

            Vector128<ushort> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt32LoadUnsafeIndexTest()
        {
            uint* value = stackalloc uint[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128<uint> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128UInt64LoadUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[2 + 1] {
                0,
                1,
                2,
            };

            Vector128<ulong> vector = Vector128.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(index + 1), vector.GetElement(index));
            }
        }

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
                Assert.Equal((short)0x0800, vector.GetElement(index));
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
        public void Vector128ByteShuffleOneInputTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<byte> result = Vector128.Shuffle(vector, Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128DoubleShuffleOneInputTest()
        {
            Vector128<double> vector = Vector128.Create((double)1, 2);
            Vector128<double> result = Vector128.Shuffle(vector, Vector128.Create((long)1, 0));

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShuffleOneInputTest()
        {
            Vector128<short> vector = Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<short> result = Vector128.Shuffle(vector, Vector128.Create((short)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShuffleOneInputTest()
        {
            Vector128<int> vector = Vector128.Create((int)1, 2, 3, 4);
            Vector128<int> result = Vector128.Shuffle(vector, Vector128.Create((int)3, 2, 1, 0));

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShuffleOneInputTest()
        {
            Vector128<long> vector = Vector128.Create((long)1, 2);
            Vector128<long> result = Vector128.Shuffle(vector, Vector128.Create((long)1, 0));

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShuffleOneInputTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<sbyte> result = Vector128.Shuffle(vector, Vector128.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SingleShuffleOneInputTest()
        {
            Vector128<float> vector = Vector128.Create((float)1, 2, 3, 4);
            Vector128<float> result = Vector128.Shuffle(vector, Vector128.Create((int)3, 2, 1, 0));

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShuffleOneInputTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<ushort> result = Vector128.Shuffle(vector, Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShuffleOneInputTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)1, 2, 3, 4);
            Vector128<uint> result = Vector128.Shuffle(vector, Vector128.Create((uint)3, 2, 1, 0));

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShuffleOneInputTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)1, 2);
            Vector128<ulong> result = Vector128.Shuffle(vector, Vector128.Create((ulong)1, 0));

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteShuffleOneInputWithDirectVectorTest()
        {
            Vector128<byte> result = Vector128.Shuffle(Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128DoubleShuffleOneInputWithDirectVectorTest()
        {
            Vector128<double> result = Vector128.Shuffle(Vector128.Create((double)1, 2), Vector128.Create((long)1, 0));

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<short> result = Vector128.Shuffle(Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8), Vector128.Create((short)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<int> result = Vector128.Shuffle(Vector128.Create((int)1, 2, 3, 4), Vector128.Create((int)3, 2, 1, 0));

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<long> result = Vector128.Shuffle(Vector128.Create((long)1, 2), Vector128.Create((long)1, 0));

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShuffleOneInputWithDirectVectorTest()
        {
            Vector128<sbyte> result = Vector128.Shuffle(Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), Vector128.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SingleShuffleOneInputWithDirectVectorTest()
        {
            Vector128<float> result = Vector128.Shuffle(Vector128.Create((float)1, 2, 3, 4), Vector128.Create((int)3, 2, 1, 0));

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<ushort> result = Vector128.Shuffle(Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8), Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<uint> result = Vector128.Shuffle(Vector128.Create((uint)1, 2, 3, 4), Vector128.Create((uint)3, 2, 1, 0));

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShuffleOneInputWithDirectVectorTest()
        {
            Vector128<ulong> result = Vector128.Shuffle(Vector128.Create((ulong)1, 2), Vector128.Create((ulong)1, 0));

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<byte> indices = Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector128<byte> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector128<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128DoubleShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<double> vector = Vector128.Create((double)1, 2);
            Vector128<long> indices = Vector128.Create((long)1, 0);
            Vector128<double> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)(Vector128<double>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<short> vector = Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<short> indices = Vector128.Create((short)7, 6, 5, 4, 3, 2, 1, 0);
            Vector128<short> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)(Vector128<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<int> vector = Vector128.Create((int)1, 2, 3, 4);
            Vector128<int> indices = Vector128.Create((int)3, 2, 1, 0);
            Vector128<int> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)(Vector128<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<long> vector = Vector128.Create((long)1, 2);
            Vector128<long> indices = Vector128.Create((long)1, 0);
            Vector128<long> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)(Vector128<long>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<sbyte> indices = Vector128.Create((sbyte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);
            Vector128<sbyte> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector128<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SingleShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<float> vector = Vector128.Create((float)1, 2, 3, 4);
            Vector128<int> indices = Vector128.Create((int)3, 2, 1, 0);
            Vector128<float> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)(Vector128<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<ushort> indices = Vector128.Create((ushort)7, 6, 5, 4, 3, 2, 1, 0);
            Vector128<ushort> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector128<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)1, 2, 3, 4);
            Vector128<uint> indices = Vector128.Create((uint)3, 2, 1, 0);
            Vector128<uint> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector128<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShuffleOneInputWithLocalIndicesTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)1, 2);
            Vector128<ulong> indices = Vector128.Create((ulong)1, 0);
            Vector128<ulong> result = Vector128.Shuffle(vector, indices);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(Vector128<ulong>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<byte> result = Vector128.Shuffle(vector, Vector128<byte>.AllBitsSet);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128DoubleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<double> vector = Vector128.Create((double)1, 2);
            Vector128<double> result = Vector128.Shuffle(vector, Vector128<long>.AllBitsSet);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<short> vector = Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<short> result = Vector128.Shuffle(vector, Vector128<short>.AllBitsSet);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<int> vector = Vector128.Create((int)1, 2, 3, 4);
            Vector128<int> result = Vector128.Shuffle(vector, Vector128<int>.AllBitsSet);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<long> vector = Vector128.Create((long)1, 2);
            Vector128<long> result = Vector128.Shuffle(vector, Vector128<long>.AllBitsSet);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<sbyte> result = Vector128.Shuffle(vector, Vector128<sbyte>.AllBitsSet);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SingleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<float> vector = Vector128.Create((float)1, 2, 3, 4);
            Vector128<float> result = Vector128.Shuffle(vector, Vector128<int>.AllBitsSet);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<ushort> result = Vector128.Shuffle(vector, Vector128<ushort>.AllBitsSet);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)1, 2, 3, 4);
            Vector128<uint> result = Vector128.Shuffle(vector, Vector128<uint>.AllBitsSet);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)1, 2);
            Vector128<ulong> result = Vector128.Shuffle(vector, Vector128<ulong>.AllBitsSet);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128ByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<byte> vector = Vector128.Create((byte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<byte> result = Vector128.Shuffle(vector, Vector128<byte>.Zero);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128DoubleShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<double> vector = Vector128.Create((double)1, 2);
            Vector128<double> result = Vector128.Shuffle(vector, Vector128<long>.Zero);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<short> vector = Vector128.Create((short)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<short> result = Vector128.Shuffle(vector, Vector128<short>.Zero);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<int> vector = Vector128.Create((int)1, 2, 3, 4);
            Vector128<int> result = Vector128.Shuffle(vector, Vector128<int>.Zero);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128Int64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<long> vector = Vector128.Create((long)1, 2);
            Vector128<long> result = Vector128.Shuffle(vector, Vector128<long>.Zero);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<sbyte> vector = Vector128.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Vector128<sbyte> result = Vector128.Shuffle(vector, Vector128<sbyte>.Zero);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128SingleShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<float> vector = Vector128.Create((float)1, 2, 3, 4);
            Vector128<float> result = Vector128.Shuffle(vector, Vector128<int>.Zero);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<ushort> vector = Vector128.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8);
            Vector128<ushort> result = Vector128.Shuffle(vector, Vector128<ushort>.Zero);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<uint> vector = Vector128.Create((uint)1, 2, 3, 4);
            Vector128<uint> result = Vector128.Shuffle(vector, Vector128<uint>.Zero);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector128UInt64ShuffleOneInputWithZeroIndicesTest()
        {
            Vector128<ulong> vector = Vector128.Create((ulong)1, 2);
            Vector128<ulong> result = Vector128.Shuffle(vector, Vector128<ulong>.Zero);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)1, result.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector128ByteStoreTest()
        {
            byte* value = stackalloc byte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128.Create((byte)0x1).Store(value);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128DoubleStoreTest()
        {
            double* value = stackalloc double[2] {
                0,
                1,
            };

            Vector128.Create((double)0x1).Store(value);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int16StoreTest()
        {
            short* value = stackalloc short[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128.Create((short)0x1).Store(value);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int32StoreTest()
        {
            int* value = stackalloc int[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((int)0x1).Store(value);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int64StoreTest()
        {
            long* value = stackalloc long[2] {
                0,
                1,
            };

            Vector128.Create((long)0x1).Store(value);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128NIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector128.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector128.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteStoreTest()
        {
            sbyte* value = stackalloc sbyte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128.Create((sbyte)0x1).Store(value);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128SingleStoreTest()
        {
            float* value = stackalloc float[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((float)0x1).Store(value);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt16StoreTest()
        {
            ushort* value = stackalloc ushort[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128.Create((ushort)0x1).Store(value);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt32StoreTest()
        {
            uint* value = stackalloc uint[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((uint)0x1).Store(value);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt64StoreTest()
        {
            ulong* value = stackalloc ulong[2] {
                0,
                1,
            };

            Vector128.Create((ulong)0x1).Store(value);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128ByteStoreAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128.Create((byte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<byte>.Count; index++)
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
        public unsafe void Vector128DoubleStoreAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((double)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<double>.Count; index++)
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
        public unsafe void Vector128Int16StoreAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128.Create((short)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<short>.Count; index++)
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
        public unsafe void Vector128Int32StoreAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((int)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<int>.Count; index++)
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
        public unsafe void Vector128Int64StoreAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((long)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<long>.Count; index++)
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
        public unsafe void Vector128NIntStoreAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128.Create((nint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
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
        public unsafe void Vector128NUIntStoreAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128.Create((nuint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
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
        public unsafe void Vector128SByteStoreAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128.Create((sbyte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<sbyte>.Count; index++)
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
        public unsafe void Vector128SingleStoreAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((float)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<float>.Count; index++)
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
        public unsafe void Vector128UInt16StoreAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128.Create((ushort)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<ushort>.Count; index++)
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
        public unsafe void Vector128UInt32StoreAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((uint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<uint>.Count; index++)
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
        public unsafe void Vector128UInt64StoreAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((ulong)0x1).StoreAligned(value);

                for (int index = 0; index < Vector128<ulong>.Count; index++)
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
        public unsafe void Vector128ByteStoreAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128.Create((byte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<byte>.Count; index++)
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
        public unsafe void Vector128DoubleStoreAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((double)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<double>.Count; index++)
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
        public unsafe void Vector128Int16StoreAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128.Create((short)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<short>.Count; index++)
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
        public unsafe void Vector128Int32StoreAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((int)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<int>.Count; index++)
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
        public unsafe void Vector128Int64StoreAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((long)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<long>.Count; index++)
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
        public unsafe void Vector128NIntStoreAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128.Create((nint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<nint>.Count; index++)
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
        public unsafe void Vector128NUIntStoreAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }

                Vector128.Create((nuint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
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
        public unsafe void Vector128SByteStoreAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;
                value[8] = 8;
                value[9] = 9;
                value[10] = 10;
                value[11] = 11;
                value[12] = 12;
                value[13] = 13;
                value[14] = 14;
                value[15] = 15;

                Vector128.Create((sbyte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<sbyte>.Count; index++)
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
        public unsafe void Vector128SingleStoreAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((float)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<float>.Count; index++)
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
        public unsafe void Vector128UInt16StoreAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector128.Create((ushort)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<ushort>.Count; index++)
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
        public unsafe void Vector128UInt32StoreAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector128.Create((uint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<uint>.Count; index++)
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
        public unsafe void Vector128UInt64StoreAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 16, alignment: 16);

                value[0] = 0;
                value[1] = 1;

                Vector128.Create((ulong)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector128<ulong>.Count; index++)
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
        public unsafe void Vector128ByteStoreUnsafeTest()
        {
            byte* value = stackalloc byte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128.Create((byte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128DoubleStoreUnsafeTest()
        {
            double* value = stackalloc double[2] {
                0,
                1,
            };

            Vector128.Create((double)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int16StoreUnsafeTest()
        {
            short* value = stackalloc short[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128.Create((short)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int32StoreUnsafeTest()
        {
            int* value = stackalloc int[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((int)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128Int64StoreUnsafeTest()
        {
            long* value = stackalloc long[2] {
                0,
                1,
            };

            Vector128.Create((long)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128NIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector128.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector128.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector128.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteStoreUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[16] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
            };

            Vector128.Create((sbyte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128SingleStoreUnsafeTest()
        {
            float* value = stackalloc float[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((float)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt16StoreUnsafeTest()
        {
            ushort* value = stackalloc ushort[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector128.Create((ushort)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt32StoreUnsafeTest()
        {
            uint* value = stackalloc uint[4] {
                0,
                1,
                2,
                3,
            };

            Vector128.Create((uint)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt64StoreUnsafeTest()
        {
            ulong* value = stackalloc ulong[2] {
                0,
                1,
            };

            Vector128.Create((ulong)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector128ByteStoreUnsafeIndexTest()
        {
            byte* value = stackalloc byte[16 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
            };

            Vector128.Create((byte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128DoubleStoreUnsafeIndexTest()
        {
            double* value = stackalloc double[2 + 1] {
                0,
                1,
                2,
            };

            Vector128.Create((double)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128Int16StoreUnsafeIndexTest()
        {
            short* value = stackalloc short[8 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
            };

            Vector128.Create((short)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128Int32StoreUnsafeIndexTest()
        {
            int* value = stackalloc int[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128.Create((int)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128Int64StoreUnsafeIndexTest()
        {
            long* value = stackalloc long[2 + 1] {
                0,
                1,
                2,
            };

            Vector128.Create((long)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128NIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector128.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
            else
            {
                nint* value = stackalloc nint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector128.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128NUIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector128.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector128.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector128<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector128SByteStoreUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[16 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9,
                10,
                11,
                12,
                13,
                14,
                15,
                16,
            };

            Vector128.Create((sbyte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128SingleStoreUnsafeIndexTest()
        {
            float* value = stackalloc float[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128.Create((float)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt16StoreUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[8 + 1] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
            };

            Vector128.Create((ushort)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt32StoreUnsafeIndexTest()
        {
            uint* value = stackalloc uint[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector128.Create((uint)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector128UInt64StoreUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[2 + 1] {
                0,
                1,
                2,
            };

            Vector128.Create((ulong)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector128<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index + 1]);
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

        [Fact]
        public void Vector128DoubleEqualsNaNTest()
        {
            Vector128<double> nan = Vector128.Create(double.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector128SingleEqualsNaNTest()
        {
            Vector128<float> nan = Vector128.Create(float.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector128DoubleEqualsNonCanonicalNaNTest()
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
                    Assert.True(Vector128.Create(i).Equals(Vector128.Create(j)));
                    Assert.False(Vector128.Create(i) == Vector128.Create(j));
                }
            }
        }

        [Fact]
        public void Vector128SingleEqualsNonCanonicalNaNTest()
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
                    Assert.True(Vector128.Create(i).Equals(Vector128.Create(j)));
                    Assert.False(Vector128.Create(i) == Vector128.Create(j));
                }
            }
        }

        [Fact]
        public void Vector128SingleCreateFromArrayTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
            Vector128<float> vector = Vector128.Create(array);
            Assert.Equal(Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f), vector);
        }

        [Fact]
        public void Vector128SingleCreateFromArrayOffsetTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f, 4.0f, 5.0f];
            Vector128<float> vector = Vector128.Create(array, 1);
            Assert.Equal(Vector128.Create(2.0f, 3.0f, 4.0f, 5.0f), vector);
        }

        [Fact]
        public void Vector128SingleCopyToTest()
        {
            float[] array = new float[4];
            Vector128.Create(2.0f).CopyTo(array);
            Assert.True(array.AsSpan().SequenceEqual([2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector128SingleCopyToOffsetTest()
        {
            float[] array = new float[5];
            Vector128.Create(2.0f).CopyTo(array, 1);
            Assert.True(array.AsSpan().SequenceEqual([0.0f, 2.0f, 2.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector128SByteAbs_MinValue()
        {
            Vector128<sbyte> vector = Vector128.Create(sbyte.MinValue);
            Vector128<sbyte> abs = Vector128.Abs(vector);
            for (int index = 0; index < Vector128<sbyte>.Count; index++)
            {
                Assert.Equal(sbyte.MinValue, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector128NuintGreaterThan_MaxValue()
        {
            Vector128<nuint> vector = Vector128.Create(nuint.MaxValue);
            Assert.True(Vector128.EqualsAll(Vector128.GreaterThan(vector, Vector128<nuint>.Zero), vector));
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
            Assert.True(Vector128<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector128<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
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
            Assert.False(Vector128<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector128<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.False((bool)methodInfo.Invoke(null, null));
        }

        [Fact]
        public void GetOneByte() => TestGetOne<byte>();

        [Fact]
        public void GetOneDouble() => TestGetOne<double>();

        [Fact]
        public void GetOneInt16() => TestGetOne<short>();

        [Fact]
        public void GetOneInt32() => TestGetOne<int>();

        [Fact]
        public void GetOneInt64() => TestGetOne<long>();

        [Fact]
        public void GetOneIntPtr() => TestGetOne<nint>();

        [Fact]
        public void GetOneSByte() => TestGetOne<sbyte>();

        [Fact]
        public void GetOneSingle() => TestGetOne<float>();

        [Fact]
        public void GetOneUInt16() => TestGetOne<ushort>();

        [Fact]
        public void GetOneUInt32() => TestGetOne<uint>();

        [Fact]
        public void GetOneUInt64() => TestGetOne<ulong>();

        [Fact]
        public void GetOneUIntPtr() => TestGetOne<nuint>();

        private static void TestGetOne<T>()
            where T : struct, INumber<T>
        {
            Assert.Equal(Vector128<T>.One, Vector128.Create(T.One));

            MethodInfo methodInfo = typeof(Vector128<T>).GetProperty("One", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.Equal((Vector128<T>)methodInfo.Invoke(null, null), Vector128.Create(T.One));
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
            Vector128<T> indices = Vector128<T>.Indices;

            for (int index = 0; index < Vector128<T>.Count; index++)
            {
                Assert.Equal(T.CreateTruncating(index), indices.GetElement(index));
            }
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(15, unchecked((byte)(-1)))]
        public void CreateSequenceByteTest(byte start, byte step) => TestCreateSequence<byte>(start, step);

        [Theory]
        [InlineData(0.0, +2.0)]
        [InlineData(3.0, +3.0)]
        [InlineData(1.0, -1.0)]
        public void CreateSequenceDoubleTest(double start, double step) => TestCreateSequence<double>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(7, -1)]
        public void CreateSequenceInt16Test(short start, short step) => TestCreateSequence<short>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(3, -1)]
        public void CreateSequenceInt32Test(int start, int step) => TestCreateSequence<int>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(7, -1)]
        public void CreateSequenceInt64Test(long start, long step) => TestCreateSequence<long>(start, step);

        [Theory]
        [InlineData(0, +2)]
        [InlineData(3, +3)]
        [InlineData(15, -1)]
        public void CreateSequenceSByteTest(sbyte start, sbyte step) => TestCreateSequence<sbyte>(start, step);

        [Theory]
        [InlineData(0.0f, +2.0f)]
        [InlineData(3.0f, +3.0f)]
        [InlineData(3.0f, -1.0f)]
        public void CreateSequenceSingleTest(float start, float step) => TestCreateSequence<float>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(7, unchecked((ushort)(-1)))]
        public void CreateSequenceUInt16Test(ushort start, ushort step) => TestCreateSequence<ushort>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(3, unchecked((uint)(-1)))]
        public void CreateSequenceUInt32Test(uint start, uint step) => TestCreateSequence<uint>(start, step);

        [Theory]
        [InlineData(0, 2)]
        [InlineData(3, 3)]
        [InlineData(1, unchecked((ulong)(-1)))]
        public void CreateSequenceUInt64Test(ulong start, ulong step) => TestCreateSequence<ulong>(start, step);

        private static void TestCreateSequence<T>(T start, T step)
            where T : INumber<T>
        {
            Vector128<T> sequence = Vector128.CreateSequence(start, step);
            T expected = start;

            for (int index = 0; index < Vector128<T>.Count; index++)
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
            Vector128<double> actualResult = Vector128.Exp(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.ExpSingle), MemberType = typeof(VectorTestMemberData))]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/97176")]
        public void ExpSingleTest(float value, float expectedResult, float variance)
        {
            Vector128<float> actualResult = Vector128.Exp(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogDouble), MemberType = typeof(VectorTestMemberData))]
        public void LogDoubleTest(double value, double expectedResult, double variance)
        {
            Vector128<double> actualResult = Vector128.Log(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogSingle), MemberType = typeof(VectorTestMemberData))]
        public void LogSingleTest(float value, float expectedResult, float variance)
        {
            Vector128<float> actualResult = Vector128.Log(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Double), MemberType = typeof(VectorTestMemberData))]
        public void Log2DoubleTest(double value, double expectedResult, double variance)
        {
            Vector128<double> actualResult = Vector128.Log2(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Single), MemberType = typeof(VectorTestMemberData))]
        public void Log2SingleTest(float value, float expectedResult, float variance)
        {
            Vector128<float> actualResult = Vector128.Log2(Vector128.Create(value));
            AssertEqual(Vector128.Create(expectedResult), actualResult, Vector128.Create(variance));
        }
    }
}
