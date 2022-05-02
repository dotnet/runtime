// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector256Tests
    {
        [Fact]
        public unsafe void Vector256ByteExtractMostSignificantBitsTest()
        {
            Vector256<byte> vector = Vector256.Create(
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

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector256DoubleExtractMostSignificantBitsTest()
        {
            Vector256<double> vector = Vector256.Create(
                +1.0,
                -0.0,
                +1.0,
                -0.0
            );

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector256Int16ExtractMostSignificantBitsTest()
        {
            Vector256<short> vector = Vector256.Create(
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

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector256Int32ExtractMostSignificantBitsTest()
        {
            Vector256<int> vector = Vector256.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            ).AsInt32();

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector256Int64ExtractMostSignificantBitsTest()
        {
            Vector256<long> vector = Vector256.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            ).AsInt64();

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector256NIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector256<nint> vector = Vector256.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNInt();

                uint result = Vector256.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1010u, result);
            }
            else
            {
                Vector256<nint> vector = Vector256.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNInt();

                uint result = Vector256.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010u, result);
            }
        }

        [Fact]
        public unsafe void Vector256NUIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector256<nuint> vector = Vector256.Create(
                    0x0000000000000001UL,
                    0x8000000000000000UL,
                    0x0000000000000001UL,
                    0x8000000000000000UL
                ).AsNUInt();

                uint result = Vector256.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1010u, result);
            }
            else
            {
                Vector256<nuint> vector = Vector256.Create(
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U,
                    0x00000001U,
                    0x80000000U
                ).AsNUInt();

                uint result = Vector256.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10101010u, result);
            }
        }

        [Fact]
        public unsafe void Vector256SByteExtractMostSignificantBitsTest()
        {
            Vector256<sbyte> vector = Vector256.Create(
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

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010_10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector256SingleExtractMostSignificantBitsTest()
        {
            Vector256<float> vector = Vector256.Create(
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f,
                +1.0f,
                -0.0f
            );

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector256UInt16ExtractMostSignificantBitsTest()
        {
            Vector256<ushort> vector = Vector256.Create(
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

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010_10101010u, result);
        }

        [Fact]
        public unsafe void Vector256UInt32ExtractMostSignificantBitsTest()
        {
            Vector256<uint> vector = Vector256.Create(
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U,
                0x00000001U,
                0x80000000U
            );

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector256UInt64ExtractMostSignificantBitsTest()
        {
            Vector256<ulong> vector = Vector256.Create(
                0x0000000000000001UL,
                0x8000000000000000UL,
                0x0000000000000001UL,
                0x8000000000000000UL
            );

            uint result = Vector256.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector256ByteLoadTest()
        {
            byte* value = stackalloc byte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256<byte> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256DoubleLoadTest()
        {
            double* value = stackalloc double[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<double> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int16LoadTest()
        {
            short* value = stackalloc short[16] {
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

            Vector256<short> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int32LoadTest()
        {
            int* value = stackalloc int[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<int> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int64LoadTest()
        {
            long* value = stackalloc long[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<long> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256NIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256<nint> vector = Vector256.Load(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256<nint> vector = Vector256.Load(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256<nuint> vector = Vector256.Load(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256<nuint> vector = Vector256.Load(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteLoadTest()
        {
            sbyte* value = stackalloc sbyte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256<sbyte> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256SingleLoadTest()
        {
            float* value = stackalloc float[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<float> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt16LoadTest()
        {
            ushort* value = stackalloc ushort[16] {
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

            Vector256<ushort> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt32LoadTest()
        {
            uint* value = stackalloc uint[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<uint> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt64LoadTest()
        {
            ulong* value = stackalloc ulong[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<ulong> vector = Vector256.Load(value);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256ByteLoadAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256<byte> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<byte>.Count; index++)
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
        public unsafe void Vector256DoubleLoadAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<double> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<double>.Count; index++)
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
        public unsafe void Vector256Int16LoadAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256<short> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<short>.Count; index++)
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
        public unsafe void Vector256Int32LoadAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<int> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<int>.Count; index++)
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
        public unsafe void Vector256Int64LoadAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<long> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<long>.Count; index++)
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
        public unsafe void Vector256NIntLoadAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256<nint> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
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
        public unsafe void Vector256NUIntLoadAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256<nuint> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
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
        public unsafe void Vector256SByteLoadAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256<sbyte> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<sbyte>.Count; index++)
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
        public unsafe void Vector256SingleLoadAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<float> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<float>.Count; index++)
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
        public unsafe void Vector256UInt16LoadAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256<ushort> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<ushort>.Count; index++)
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
        public unsafe void Vector256UInt32LoadAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<uint> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<uint>.Count; index++)
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
        public unsafe void Vector256UInt64LoadAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<ulong> vector = Vector256.LoadAligned(value);

                for (int index = 0; index < Vector256<ulong>.Count; index++)
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
        public unsafe void Vector256ByteLoadAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256<byte> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<byte>.Count; index++)
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
        public unsafe void Vector256DoubleLoadAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<double> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<double>.Count; index++)
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
        public unsafe void Vector256Int16LoadAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256<short> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<short>.Count; index++)
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
        public unsafe void Vector256Int32LoadAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<int> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<int>.Count; index++)
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
        public unsafe void Vector256Int64LoadAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<long> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<long>.Count; index++)
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
        public unsafe void Vector256NIntLoadAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256<nint> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
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
        public unsafe void Vector256NUIntLoadAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256<nuint> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
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
        public unsafe void Vector256SByteLoadAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256<sbyte> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<sbyte>.Count; index++)
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
        public unsafe void Vector256SingleLoadAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<float> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<float>.Count; index++)
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
        public unsafe void Vector256UInt16LoadAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256<ushort> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<ushort>.Count; index++)
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
        public unsafe void Vector256UInt32LoadAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256<uint> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<uint>.Count; index++)
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
        public unsafe void Vector256UInt64LoadAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256<ulong> vector = Vector256.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<ulong>.Count; index++)
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
        public unsafe void Vector256ByteLoadUnsafeTest()
        {
            byte* value = stackalloc byte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256<byte> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256DoubleLoadUnsafeTest()
        {
            double* value = stackalloc double[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<double> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int16LoadUnsafeTest()
        {
            short* value = stackalloc short[16] {
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

            Vector256<short> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int32LoadUnsafeTest()
        {
            int* value = stackalloc int[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<int> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int64LoadUnsafeTest()
        {
            long* value = stackalloc long[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<long> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256NIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256<nint> vector = Vector256.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256<nint> vector = Vector256.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256<nuint> vector = Vector256.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256<nuint> vector = Vector256.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteLoadUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256<sbyte> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256SingleLoadUnsafeTest()
        {
            float* value = stackalloc float[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<float> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt16LoadUnsafeTest()
        {
            ushort* value = stackalloc ushort[16] {
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

            Vector256<ushort> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt32LoadUnsafeTest()
        {
            uint* value = stackalloc uint[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256<uint> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt64LoadUnsafeTest()
        {
            ulong* value = stackalloc ulong[4] {
                0,
                1,
                2,
                3,
            };

            Vector256<ulong> vector = Vector256.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256ByteLoadUnsafeIndexTest()
        {
            byte* value = stackalloc byte[32 + 1] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
            };

            Vector256<byte> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256DoubleLoadUnsafeIndexTest()
        {
            double* value = stackalloc double[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256<double> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int16LoadUnsafeIndexTest()
        {
            short* value = stackalloc short[16 + 1] {
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

            Vector256<short> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int32LoadUnsafeIndexTest()
        {
            int* value = stackalloc int[8 + 1] {
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

            Vector256<int> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256Int64LoadUnsafeIndexTest()
        {
            long* value = stackalloc long[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256<long> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256NIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector256<nint> vector = Vector256.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[8 + 1] {
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

                Vector256<nint> vector = Vector256.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector256<nuint> vector = Vector256.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8 + 1] {
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

                Vector256<nuint> vector = Vector256.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteLoadUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[32 + 1] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
            };

            Vector256<sbyte> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256SingleLoadUnsafeIndexTest()
        {
            float* value = stackalloc float[8 + 1] {
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

            Vector256<float> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt16LoadUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[16 + 1] {
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

            Vector256<ushort> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt32LoadUnsafeIndexTest()
        {
            uint* value = stackalloc uint[8 + 1] {
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

            Vector256<uint> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector256UInt64LoadUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256<ulong> vector = Vector256.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(index + 1), vector.GetElement(index));
            }
        }

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
                Assert.Equal((short)0x0800, vector.GetElement(index));
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
        public unsafe void Vector256ByteStoreTest()
        {
            byte* value = stackalloc byte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256.Create((byte)0x1).Store(value);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256DoubleStoreTest()
        {
            double* value = stackalloc double[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((double)0x1).Store(value);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int16StoreTest()
        {
            short* value = stackalloc short[16] {
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

            Vector256.Create((short)0x1).Store(value);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int32StoreTest()
        {
            int* value = stackalloc int[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((int)0x1).Store(value);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int64StoreTest()
        {
            long* value = stackalloc long[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((long)0x1).Store(value);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256NIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteStoreTest()
        {
            sbyte* value = stackalloc sbyte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256.Create((sbyte)0x1).Store(value);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256SingleStoreTest()
        {
            float* value = stackalloc float[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((float)0x1).Store(value);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt16StoreTest()
        {
            ushort* value = stackalloc ushort[16] {
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

            Vector256.Create((ushort)0x1).Store(value);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt32StoreTest()
        {
            uint* value = stackalloc uint[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((uint)0x1).Store(value);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt64StoreTest()
        {
            ulong* value = stackalloc ulong[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((ulong)0x1).Store(value);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256ByteStoreAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256.Create((byte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<byte>.Count; index++)
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
        public unsafe void Vector256DoubleStoreAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((double)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<double>.Count; index++)
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
        public unsafe void Vector256Int16StoreAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256.Create((short)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<short>.Count; index++)
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
        public unsafe void Vector256Int32StoreAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((int)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<int>.Count; index++)
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
        public unsafe void Vector256Int64StoreAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((long)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<long>.Count; index++)
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
        public unsafe void Vector256NIntStoreAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256.Create((nint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
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
        public unsafe void Vector256NUIntStoreAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256.Create((nuint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
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
        public unsafe void Vector256SByteStoreAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256.Create((sbyte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<sbyte>.Count; index++)
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
        public unsafe void Vector256SingleStoreAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((float)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<float>.Count; index++)
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
        public unsafe void Vector256UInt16StoreAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256.Create((ushort)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<ushort>.Count; index++)
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
        public unsafe void Vector256UInt32StoreAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((uint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<uint>.Count; index++)
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
        public unsafe void Vector256UInt64StoreAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((ulong)0x1).StoreAligned(value);

                for (int index = 0; index < Vector256<ulong>.Count; index++)
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
        public unsafe void Vector256ByteStoreAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256.Create((byte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<byte>.Count; index++)
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
        public unsafe void Vector256DoubleStoreAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((double)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<double>.Count; index++)
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
        public unsafe void Vector256Int16StoreAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256.Create((short)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<short>.Count; index++)
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
        public unsafe void Vector256Int32StoreAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((int)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<int>.Count; index++)
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
        public unsafe void Vector256Int64StoreAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((long)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<long>.Count; index++)
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
        public unsafe void Vector256NIntStoreAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256.Create((nint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<nint>.Count; index++)
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
        public unsafe void Vector256NUIntStoreAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                    value[2] = 2;
                    value[3] = 3;
                    value[4] = 4;
                    value[5] = 5;
                    value[6] = 6;
                    value[7] = 7;
                }

                Vector256.Create((nuint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
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
        public unsafe void Vector256SByteStoreAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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
                value[16] = 16;
                value[17] = 17;
                value[18] = 18;
                value[19] = 19;
                value[20] = 20;
                value[21] = 21;
                value[22] = 22;
                value[23] = 23;
                value[24] = 24;
                value[25] = 25;
                value[26] = 26;
                value[27] = 27;
                value[28] = 28;
                value[29] = 29;
                value[30] = 30;
                value[31] = 31;

                Vector256.Create((sbyte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<sbyte>.Count; index++)
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
        public unsafe void Vector256SingleStoreAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((float)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<float>.Count; index++)
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
        public unsafe void Vector256UInt16StoreAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

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

                Vector256.Create((ushort)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<ushort>.Count; index++)
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
        public unsafe void Vector256UInt32StoreAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector256.Create((uint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<uint>.Count; index++)
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
        public unsafe void Vector256UInt64StoreAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 32, alignment: 32);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector256.Create((ulong)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector256<ulong>.Count; index++)
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
        public unsafe void Vector256ByteStoreUnsafeTest()
        {
            byte* value = stackalloc byte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256.Create((byte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256DoubleStoreUnsafeTest()
        {
            double* value = stackalloc double[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((double)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int16StoreUnsafeTest()
        {
            short* value = stackalloc short[16] {
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

            Vector256.Create((short)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int32StoreUnsafeTest()
        {
            int* value = stackalloc int[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((int)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256Int64StoreUnsafeTest()
        {
            long* value = stackalloc long[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((long)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256NIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4] {
                    0,
                    1,
                    2,
                    3,
                };

                Vector256.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8] {
                    0,
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                };

                Vector256.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteStoreUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[32] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
            };

            Vector256.Create((sbyte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256SingleStoreUnsafeTest()
        {
            float* value = stackalloc float[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((float)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt16StoreUnsafeTest()
        {
            ushort* value = stackalloc ushort[16] {
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

            Vector256.Create((ushort)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt32StoreUnsafeTest()
        {
            uint* value = stackalloc uint[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector256.Create((uint)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt64StoreUnsafeTest()
        {
            ulong* value = stackalloc ulong[4] {
                0,
                1,
                2,
                3,
            };

            Vector256.Create((ulong)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector256ByteStoreUnsafeIndexTest()
        {
            byte* value = stackalloc byte[32 + 1] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
            };

            Vector256.Create((byte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256DoubleStoreUnsafeIndexTest()
        {
            double* value = stackalloc double[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256.Create((double)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256Int16StoreUnsafeIndexTest()
        {
            short* value = stackalloc short[16 + 1] {
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

            Vector256.Create((short)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256Int32StoreUnsafeIndexTest()
        {
            int* value = stackalloc int[8 + 1] {
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

            Vector256.Create((int)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256Int64StoreUnsafeIndexTest()
        {
            long* value = stackalloc long[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256.Create((long)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256NIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector256.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
            else
            {
                nint* value = stackalloc nint[8 + 1] {
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

                Vector256.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256NUIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[4 + 1] {
                    0,
                    1,
                    2,
                    3,
                    4,
                };

                Vector256.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[8 + 1] {
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

                Vector256.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector256<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector256SByteStoreUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[32 + 1] {
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
                17,
                18,
                19,
                20,
                21,
                22,
                23,
                24,
                25,
                26,
                27,
                28,
                29,
                30,
                31,
                32,
            };

            Vector256.Create((sbyte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256SingleStoreUnsafeIndexTest()
        {
            float* value = stackalloc float[8 + 1] {
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

            Vector256.Create((float)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt16StoreUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[16 + 1] {
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

            Vector256.Create((ushort)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt32StoreUnsafeIndexTest()
        {
            uint* value = stackalloc uint[8 + 1] {
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

            Vector256.Create((uint)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector256UInt64StoreUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector256.Create((ulong)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector256<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index + 1]);
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

        [Fact]
        public void Vector256DoubleEqualsNaNTest()
        {
            Vector256<double> nan = Vector256.Create(double.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector256SingleEqualsNaNTest()
        {
            Vector256<float> nan = Vector256.Create(float.NaN);
            Assert.True(nan.Equals(nan));
        }
    }
}
