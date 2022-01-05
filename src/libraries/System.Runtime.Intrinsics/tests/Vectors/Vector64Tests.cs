﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector64Tests
    {
        [Fact]
        public unsafe void Vector64ByteExtractMostSignificantBitsTest()
        {
            Vector64<byte> vector = Vector64.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            );

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector64DoubleExtractMostSignificantBitsTest()
        {
            Vector64<double> vector = Vector64.Create(
                +1.0
            );

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b0u, result);

            vector = Vector64.Create(
                -0.0
            );

            result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1u, result);
        }

        [Fact]
        public unsafe void Vector64Int16ExtractMostSignificantBitsTest()
        {
            Vector64<short> vector = Vector64.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000
            ).AsInt16();

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector64Int32ExtractMostSignificantBitsTest()
        {
            Vector64<int> vector = Vector64.Create(
                0x00000001U,
                0x80000000U
            ).AsInt32();

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector64Int64ExtractMostSignificantBitsTest()
        {
            Vector64<long> vector = Vector64.Create(
                0x0000000000000001UL
            ).AsInt64();

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b0u, result);

            vector = Vector64.Create(
                0x8000000000000000UL
            ).AsInt64();

            result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1u, result);
        }

        [Fact]
        public unsafe void Vector64NIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector64<nint> vector = Vector64.Create(
                    0x0000000000000001UL
                ).AsNInt();

                uint result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b0u, result);

                vector = Vector64.Create(
                    0x8000000000000000UL
                ).AsNInt();

                result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1u, result);
            }
            else
            {
                Vector64<nint> vector = Vector64.Create(
                    0x00000001U,
                    0x80000000U
                ).AsNInt();

                uint result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10u, result);
            }
        }

        [Fact]
        public unsafe void Vector64NUIntExtractMostSignificantBitsTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector64<nuint> vector = Vector64.Create(
                    0x0000000000000001UL
                ).AsNUInt();

                uint result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b0u, result);

                vector = Vector64.Create(
                    0x8000000000000000UL
                ).AsNUInt();

                result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b1u, result);
            }
            else
            {
                Vector64<nuint> vector = Vector64.Create(
                    0x00000001U,
                    0x80000000U
                ).AsNUInt();

                uint result = Vector64.ExtractMostSignificantBits(vector);
                Assert.Equal(0b10u, result);
            }
        }

        [Fact]
        public unsafe void Vector64SByteExtractMostSignificantBitsTest()
        {
            Vector64<sbyte> vector = Vector64.Create(
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80,
                0x01,
                0x80
            ).AsSByte();

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10101010u, result);
        }

        [Fact]
        public unsafe void Vector64SingleExtractMostSignificantBitsTest()
        {
            Vector64<float> vector = Vector64.Create(
                +1.0f,
                -0.0f
            );

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector64UInt16ExtractMostSignificantBitsTest()
        {
            Vector64<ushort> vector = Vector64.Create(
                0x0001,
                0x8000,
                0x0001,
                0x8000
            );

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1010u, result);
        }

        [Fact]
        public unsafe void Vector64UInt32ExtractMostSignificantBitsTest()
        {
            Vector64<uint> vector = Vector64.Create(
                0x00000001U,
                0x80000000U
            );

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b10u, result);
        }

        [Fact]
        public unsafe void Vector64UInt64ExtractMostSignificantBitsTest()
        {
            Vector64<ulong> vector = Vector64.Create(
                0x0000000000000001UL
            ); 

            uint result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b0u, result);

            vector = Vector64.Create(
                0x8000000000000000UL
            );

            result = Vector64.ExtractMostSignificantBits(vector);
            Assert.Equal(0b1u, result);
        }

        [Fact]
        public unsafe void Vector64ByteLoadTest()
        {
            byte* value = stackalloc byte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64<byte> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64DoubleLoadTest()
        {
            double* value = stackalloc double[1] {
                0,
            };

            Vector64<double> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int16LoadTest()
        {
            short* value = stackalloc short[4] {
                0,
                1,
                2,
                3,
            };

            Vector64<short> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int32LoadTest()
        {
            int* value = stackalloc int[2] {
                0,
                1,
            };

            Vector64<int> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int64LoadTest()
        {
            long* value = stackalloc long[1] {
                0,
            };

            Vector64<long> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64NIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1] {
                    0,
                };

                Vector64<nint> vector = Vector64.Load(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector64<nint> vector = Vector64.Load(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntLoadTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1] {
                    0,
                };

                Vector64<nuint> vector = Vector64.Load(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector64<nuint> vector = Vector64.Load(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteLoadTest()
        {
            sbyte* value = stackalloc sbyte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64<sbyte> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64SingleLoadTest()
        {
            float* value = stackalloc float[2] {
                0,
                1,
            };

            Vector64<float> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt16LoadTest()
        {
            ushort* value = stackalloc ushort[4] {
                0,
                1,
                2,
                3,
            };

            Vector64<ushort> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt32LoadTest()
        {
            uint* value = stackalloc uint[2] {
                0,
                1,
            };

            Vector64<uint> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt64LoadTest()
        {
            ulong* value = stackalloc ulong[1] {
                0,
            };

            Vector64<ulong> vector = Vector64.Load(value);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64ByteLoadAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64<byte> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<byte>.Count; index++)
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
        public unsafe void Vector64DoubleLoadAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<double> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<double>.Count; index++)
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
        public unsafe void Vector64Int16LoadAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64<short> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<short>.Count; index++)
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
        public unsafe void Vector64Int32LoadAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<int> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<int>.Count; index++)
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
        public unsafe void Vector64Int64LoadAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<long> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<long>.Count; index++)
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
        public unsafe void Vector64NIntLoadAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64<nint> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
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
        public unsafe void Vector64NUIntLoadAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64<nuint> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
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
        public unsafe void Vector64SByteLoadAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64<sbyte> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<sbyte>.Count; index++)
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
        public unsafe void Vector64SingleLoadAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<float> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<float>.Count; index++)
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
        public unsafe void Vector64UInt16LoadAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64<ushort> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<ushort>.Count; index++)
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
        public unsafe void Vector64UInt32LoadAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<uint> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<uint>.Count; index++)
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
        public unsafe void Vector64UInt64LoadAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<ulong> vector = Vector64.LoadAligned(value);

                for (int index = 0; index < Vector64<ulong>.Count; index++)
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
        public unsafe void Vector64ByteLoadAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64<byte> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<byte>.Count; index++)
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
        public unsafe void Vector64DoubleLoadAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<double> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<double>.Count; index++)
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
        public unsafe void Vector64Int16LoadAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64<short> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<short>.Count; index++)
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
        public unsafe void Vector64Int32LoadAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<int> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<int>.Count; index++)
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
        public unsafe void Vector64Int64LoadAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<long> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<long>.Count; index++)
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
        public unsafe void Vector64NIntLoadAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64<nint> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
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
        public unsafe void Vector64NUIntLoadAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64<nuint> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
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
        public unsafe void Vector64SByteLoadAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64<sbyte> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<sbyte>.Count; index++)
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
        public unsafe void Vector64SingleLoadAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<float> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<float>.Count; index++)
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
        public unsafe void Vector64UInt16LoadAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64<ushort> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<ushort>.Count; index++)
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
        public unsafe void Vector64UInt32LoadAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64<uint> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<uint>.Count; index++)
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
        public unsafe void Vector64UInt64LoadAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64<ulong> vector = Vector64.LoadAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<ulong>.Count; index++)
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
        public unsafe void Vector64ByteLoadUnsafeTest()
        {
            byte* value = stackalloc byte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64<byte> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64DoubleLoadUnsafeTest()
        {
            double* value = stackalloc double[1] {
                0,
            };

            Vector64<double> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int16LoadUnsafeTest()
        {
            short* value = stackalloc short[4] {
                0,
                1,
                2,
                3,
            };

            Vector64<short> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int32LoadUnsafeTest()
        {
            int* value = stackalloc int[2] {
                0,
                1,
            };

            Vector64<int> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int64LoadUnsafeTest()
        {
            long* value = stackalloc long[1] {
                0,
            };

            Vector64<long> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64NIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1] {
                    0,
                };

                Vector64<nint> vector = Vector64.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector64<nint> vector = Vector64.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntLoadUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1] {
                    0,
                };

                Vector64<nuint> vector = Vector64.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector64<nuint> vector = Vector64.LoadUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)index, vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteLoadUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64<sbyte> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64SingleLoadUnsafeTest()
        {
            float* value = stackalloc float[2] {
                0,
                1,
            };

            Vector64<float> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt16LoadUnsafeTest()
        {
            ushort* value = stackalloc ushort[4] {
                0,
                1,
                2,
                3,
            };

            Vector64<ushort> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt32LoadUnsafeTest()
        {
            uint* value = stackalloc uint[2] {
                0,
                1,
            };

            Vector64<uint> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt64LoadUnsafeTest()
        {
            ulong* value = stackalloc ulong[1] {
                0,
            };

            Vector64<ulong> vector = Vector64.LoadUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)index, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64ByteLoadUnsafeIndexTest()
        {
            byte* value = stackalloc byte[8 + 1] {
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

            Vector64<byte> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64DoubleLoadUnsafeIndexTest()
        {
            double* value = stackalloc double[1 + 1] {
                0,
                1,
            };

            Vector64<double> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int16LoadUnsafeIndexTest()
        {
            short* value = stackalloc short[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector64<short> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int32LoadUnsafeIndexTest()
        {
            int* value = stackalloc int[2 + 1] {
                0,
                1,
                2,
            };

            Vector64<int> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64Int64LoadUnsafeIndexTest()
        {
            long* value = stackalloc long[1 + 1] {
                0,
                1,
            };

            Vector64<long> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64NIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1 + 1] {
                    0,
                    1,
                };

                Vector64<nint> vector = Vector64.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nint* value = stackalloc nint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector64<nint> vector = Vector64.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntLoadUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1 + 1] {
                    0,
                    1,
                };

                Vector64<nuint> vector = Vector64.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector64<nuint> vector = Vector64.LoadUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)(index + 1), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteLoadUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[8 + 1] {
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

            Vector64<sbyte> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64SingleLoadUnsafeIndexTest()
        {
            float* value = stackalloc float[2 + 1] {
                0,
                1,
                2,
            };

            Vector64<float> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt16LoadUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector64<ushort> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt32LoadUnsafeIndexTest()
        {
            uint* value = stackalloc uint[2 + 1] {
                0,
                1,
                2,
            };

            Vector64<uint> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64UInt64LoadUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[1 + 1] {
                0,
                1,
            };

            Vector64<ulong> vector = Vector64.LoadUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)(index + 1), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShiftLeftTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShiftLeftTest()
        {
            Vector64<short> vector = Vector64.Create((short)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShiftLeftTest()
        {
            Vector64<int> vector = Vector64.Create((int)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int64ShiftLeftTest()
        {
            Vector64<long> vector = Vector64.Create((long)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64NIntShiftLeftTest()
        {
            Vector64<nint> vector = Vector64.Create((nint)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<nint>.Count; index++)
            {
                Assert.Equal((nint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64NUIntShiftLeftTest()
        {
            Vector64<nuint> vector = Vector64.Create((nuint)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<nuint>.Count; index++)
            {
                Assert.Equal((nuint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShiftLeftTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShiftLeftTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShiftLeftTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt64ShiftLeftTest()
        {
            Vector64<ulong> vector = Vector64.Create((ulong)0x01);
            vector = Vector64.ShiftLeft(vector, 4);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x10, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShiftRightArithmeticTest()
        {
            Vector64<short> vector = Vector64.Create(unchecked((short)0x8000));
            vector = Vector64.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal(unchecked((short)0xF800), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShiftRightArithmeticTest()
        {
            Vector64<int> vector = Vector64.Create(unchecked((int)0x80000000));
            vector = Vector64.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal(unchecked((int)0xF8000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int64ShiftRightArithmeticTest()
        {
            Vector64<long> vector = Vector64.Create(unchecked((long)0x8000000000000000));
            vector = Vector64.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal(unchecked((long)0xF800000000000000), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64NIntShiftRightArithmeticTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector64<nint> vector = Vector64.Create(unchecked((nint)0x8000000000000000));
                vector = Vector64.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector64<nint> vector = Vector64.Create(unchecked((nint)0x80000000));
                vector = Vector64.ShiftRightArithmetic(vector, 4);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0xF8000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector64SByteShiftRightArithmeticTest()
        {
            Vector64<sbyte> vector = Vector64.Create(unchecked((sbyte)0x80));
            vector = Vector64.ShiftRightArithmetic(vector, 4);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal(unchecked((sbyte)0xF8), vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShiftRightLogicalTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)0x80);
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShiftRightLogicalTest()
        {
            Vector64<short> vector = Vector64.Create(unchecked((short)0x8000));
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShiftRightLogicalTest()
        {
            Vector64<int> vector = Vector64.Create(unchecked((int)0x80000000));
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int64ShiftRightLogicalTest()
        {
            Vector64<long> vector = Vector64.Create(unchecked((long)0x8000000000000000));
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64NIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector64<nint> vector = Vector64.Create(unchecked((nint)0x8000000000000000));
                vector = Vector64.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector64<nint> vector = Vector64.Create(unchecked((nint)0x80000000));
                vector = Vector64.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal(unchecked((nint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector64NUIntShiftRightLogicalTest()
        {
            if (Environment.Is64BitProcess)
            {
                Vector64<nuint> vector = Vector64.Create(unchecked((nuint)0x8000000000000000));
                vector = Vector64.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x0800000000000000), vector.GetElement(index));
                }
            }
            else
            {
                Vector64<nuint> vector = Vector64.Create(unchecked((nuint)0x80000000));
                vector = Vector64.ShiftRightLogical(vector, 4);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal(unchecked((nuint)0x08000000), vector.GetElement(index));
                }
            }
        }

        [Fact]
        public void Vector64SByteShiftRightLogicalTest()
        {
            Vector64<sbyte> vector = Vector64.Create(unchecked((sbyte)0x80));
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x08, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShiftRightLogicalTest()
        {
            Vector64<ushort> vector = Vector64.Create(unchecked((ushort)0x8000));
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x0800, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShiftRightLogicalTest()
        {
            Vector64<uint> vector = Vector64.Create(0x80000000);
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0x08000000, vector.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt64ShiftRightLogicalTest()
        {
            Vector64<ulong> vector = Vector64.Create(0x8000000000000000);
            vector = Vector64.ShiftRightLogical(vector, 4);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x0800000000000000, vector.GetElement(index));
            }
        }

        [Fact]
        public unsafe void Vector64ByteStoreTest()
        {
            byte* value = stackalloc byte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64.Create((byte)0x1).Store(value);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64DoubleStoreTest()
        {
            double* value = stackalloc double[1] {
                0,
            };

            Vector64.Create((double)0x1).Store(value);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int16StoreTest()
        {
            short* value = stackalloc short[4] {
                0,
                1,
                2,
                3,
            };

            Vector64.Create((short)0x1).Store(value);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int32StoreTest()
        {
            int* value = stackalloc int[2] {
                0,
                1,
            };

            Vector64.Create((int)0x1).Store(value);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int64StoreTest()
        {
            long* value = stackalloc long[1] {
                0,
            };

            Vector64.Create((long)0x1).Store(value);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64NIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1] {
                    0,
                };

                Vector64.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector64.Create((nint)0x1).Store(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntStoreTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1] {
                    0,
                };

                Vector64.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector64.Create((nuint)0x1).Store(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteStoreTest()
        {
            sbyte* value = stackalloc sbyte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64.Create((sbyte)0x1).Store(value);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64SingleStoreTest()
        {
            float* value = stackalloc float[2] {
                0,
                1,
            };

            Vector64.Create((float)0x1).Store(value);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt16StoreTest()
        {
            ushort* value = stackalloc ushort[4] {
                0,
                1,
                2,
                3,
            };

            Vector64.Create((ushort)0x1).Store(value);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt32StoreTest()
        {
            uint* value = stackalloc uint[2] {
                0,
                1,
            };

            Vector64.Create((uint)0x1).Store(value);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt64StoreTest()
        {
            ulong* value = stackalloc ulong[1] {
                0,
            };

            Vector64.Create((ulong)0x1).Store(value);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64ByteStoreAlignedTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64.Create((byte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<byte>.Count; index++)
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
        public unsafe void Vector64DoubleStoreAlignedTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((double)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<double>.Count; index++)
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
        public unsafe void Vector64Int16StoreAlignedTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64.Create((short)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<short>.Count; index++)
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
        public unsafe void Vector64Int32StoreAlignedTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((int)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<int>.Count; index++)
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
        public unsafe void Vector64Int64StoreAlignedTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((long)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<long>.Count; index++)
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
        public unsafe void Vector64NIntStoreAlignedTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64.Create((nint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
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
        public unsafe void Vector64NUIntStoreAlignedTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64.Create((nuint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
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
        public unsafe void Vector64SByteStoreAlignedTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64.Create((sbyte)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<sbyte>.Count; index++)
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
        public unsafe void Vector64SingleStoreAlignedTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((float)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<float>.Count; index++)
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
        public unsafe void Vector64UInt16StoreAlignedTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64.Create((ushort)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<ushort>.Count; index++)
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
        public unsafe void Vector64UInt32StoreAlignedTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((uint)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<uint>.Count; index++)
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
        public unsafe void Vector64UInt64StoreAlignedTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((ulong)0x1).StoreAligned(value);

                for (int index = 0; index < Vector64<ulong>.Count; index++)
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
        public unsafe void Vector64ByteStoreAlignedNonTemporalTest()
        {
            byte* value = null;

            try
            {
                value = (byte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64.Create((byte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<byte>.Count; index++)
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
        public unsafe void Vector64DoubleStoreAlignedNonTemporalTest()
        {
            double* value = null;

            try
            {
                value = (double*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((double)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<double>.Count; index++)
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
        public unsafe void Vector64Int16StoreAlignedNonTemporalTest()
        {
            short* value = null;

            try
            {
                value = (short*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64.Create((short)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<short>.Count; index++)
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
        public unsafe void Vector64Int32StoreAlignedNonTemporalTest()
        {
            int* value = null;

            try
            {
                value = (int*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((int)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<int>.Count; index++)
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
        public unsafe void Vector64Int64StoreAlignedNonTemporalTest()
        {
            long* value = null;

            try
            {
                value = (long*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((long)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<long>.Count; index++)
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
        public unsafe void Vector64NIntStoreAlignedNonTemporalTest()
        {
            nint* value = null;

            try
            {
                value = (nint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64.Create((nint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<nint>.Count; index++)
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
        public unsafe void Vector64NUIntStoreAlignedNonTemporalTest()
        {
            nuint* value = null;

            try
            {
                value = (nuint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                if (Environment.Is64BitProcess)
                {
                    value[0] = 0;
                }
                else
                {
                    value[0] = 0;
                    value[1] = 1;
                }

                Vector64.Create((nuint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
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
        public unsafe void Vector64SByteStoreAlignedNonTemporalTest()
        {
            sbyte* value = null;

            try
            {
                value = (sbyte*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;
                value[4] = 4;
                value[5] = 5;
                value[6] = 6;
                value[7] = 7;

                Vector64.Create((sbyte)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<sbyte>.Count; index++)
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
        public unsafe void Vector64SingleStoreAlignedNonTemporalTest()
        {
            float* value = null;

            try
            {
                value = (float*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((float)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<float>.Count; index++)
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
        public unsafe void Vector64UInt16StoreAlignedNonTemporalTest()
        {
            ushort* value = null;

            try
            {
                value = (ushort*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;
                value[2] = 2;
                value[3] = 3;

                Vector64.Create((ushort)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<ushort>.Count; index++)
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
        public unsafe void Vector64UInt32StoreAlignedNonTemporalTest()
        {
            uint* value = null;

            try
            {
                value = (uint*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;
                value[1] = 1;

                Vector64.Create((uint)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<uint>.Count; index++)
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
        public unsafe void Vector64UInt64StoreAlignedNonTemporalTest()
        {
            ulong* value = null;

            try
            {
                value = (ulong*)NativeMemory.AlignedAlloc(byteCount: 8, alignment: 8);

                value[0] = 0;

                Vector64.Create((ulong)0x1).StoreAlignedNonTemporal(value);

                for (int index = 0; index < Vector64<ulong>.Count; index++)
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
        public unsafe void Vector64ByteStoreUnsafeTest()
        {
            byte* value = stackalloc byte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64.Create((byte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64DoubleStoreUnsafeTest()
        {
            double* value = stackalloc double[1] {
                0,
            };

            Vector64.Create((double)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int16StoreUnsafeTest()
        {
            short* value = stackalloc short[4] {
                0,
                1,
                2,
                3,
            };

            Vector64.Create((short)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int32StoreUnsafeTest()
        {
            int* value = stackalloc int[2] {
                0,
                1,
            };

            Vector64.Create((int)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64Int64StoreUnsafeTest()
        {
            long* value = stackalloc long[1] {
                0,
            };

            Vector64.Create((long)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64NIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1] {
                    0,
                };

                Vector64.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
            else
            {
                nint* value = stackalloc nint[2] {
                    0,
                    1,
                };

                Vector64.Create((nint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntStoreUnsafeTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1] {
                    0,
                };

                Vector64.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2] {
                    0,
                    1,
                };

                Vector64.Create((nuint)0x1).StoreUnsafe(ref value[0]);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteStoreUnsafeTest()
        {
            sbyte* value = stackalloc sbyte[8] {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            Vector64.Create((sbyte)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64SingleStoreUnsafeTest()
        {
            float* value = stackalloc float[2] {
                0,
                1,
            };

            Vector64.Create((float)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt16StoreUnsafeTest()
        {
            ushort* value = stackalloc ushort[4] {
                0,
                1,
                2,
                3,
            };

            Vector64.Create((ushort)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt32StoreUnsafeTest()
        {
            uint* value = stackalloc uint[2] {
                0,
                1,
            };

            Vector64.Create((uint)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt64StoreUnsafeTest()
        {
            ulong* value = stackalloc ulong[1] {
                0,
            };

            Vector64.Create((ulong)0x1).StoreUnsafe(ref value[0]);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index]);
            }
        }

        [Fact]
        public unsafe void Vector64ByteStoreUnsafeIndexTest()
        {
            byte* value = stackalloc byte[8 + 1] {
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

            Vector64.Create((byte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64DoubleStoreUnsafeIndexTest()
        {
            double* value = stackalloc double[1 + 1] {
                0,
                1,
            };

            Vector64.Create((double)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<double>.Count; index++)
            {
                Assert.Equal((double)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64Int16StoreUnsafeIndexTest()
        {
            short* value = stackalloc short[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector64.Create((short)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64Int32StoreUnsafeIndexTest()
        {
            int* value = stackalloc int[2 + 1] {
                0,
                1,
                2,
            };

            Vector64.Create((int)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64Int64StoreUnsafeIndexTest()
        {
            long* value = stackalloc long[1 + 1] {
                0,
                1,
            };

            Vector64.Create((long)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<long>.Count; index++)
            {
                Assert.Equal((long)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64NIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nint* value = stackalloc nint[1 + 1] {
                    0,
                    1,
                };

                Vector64.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
            else
            {
                nint* value = stackalloc nint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector64.Create((nint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nint>.Count; index++)
                {
                    Assert.Equal((nint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64NUIntStoreUnsafeIndexTest()
        {
            if (Environment.Is64BitProcess)
            {
                nuint* value = stackalloc nuint[1 + 1] {
                    0,
                    1,
                };

                Vector64.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
            else
            {
                nuint* value = stackalloc nuint[2 + 1] {
                    0,
                    1,
                    2,
                };

                Vector64.Create((nuint)0x1).StoreUnsafe(ref value[0], 1);

                for (int index = 0; index < Vector64<nuint>.Count; index++)
                {
                    Assert.Equal((nuint)0x1, value[index + 1]);
                }
            }
        }

        [Fact]
        public unsafe void Vector64SByteStoreUnsafeIndexTest()
        {
            sbyte* value = stackalloc sbyte[8 + 1] {
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

            Vector64.Create((sbyte)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64SingleStoreUnsafeIndexTest()
        {
            float* value = stackalloc float[2 + 1] {
                0,
                1,
                2,
            };

            Vector64.Create((float)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt16StoreUnsafeIndexTest()
        {
            ushort* value = stackalloc ushort[4 + 1] {
                0,
                1,
                2,
                3,
                4,
            };

            Vector64.Create((ushort)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt32StoreUnsafeIndexTest()
        {
            uint* value = stackalloc uint[2 + 1] {
                0,
                1,
                2,
            };

            Vector64.Create((uint)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0x1, value[index + 1]);
            }
        }

        [Fact]
        public unsafe void Vector64UInt64StoreUnsafeIndexTest()
        {
            ulong* value = stackalloc ulong[1 + 1] {
                0,
                1,
            };

            Vector64.Create((ulong)0x1).StoreUnsafe(ref value[0], 1);

            for (int index = 0; index < Vector64<ulong>.Count; index++)
            {
                Assert.Equal((ulong)0x1, value[index + 1]);
            }
        }

        [Fact]
        public void Vector64ByteSumTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)0x01);
            Assert.Equal((byte)8, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64DoubleSumTest()
        {
            Vector64<double> vector = Vector64.Create((double)0x01);
            Assert.Equal(1.0, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64Int16SumTest()
        {
            Vector64<short> vector = Vector64.Create((short)0x01);
            Assert.Equal((short)4, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64Int32SumTest()
        {
            Vector64<int> vector = Vector64.Create((int)0x01);
            Assert.Equal((int)2, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64Int64SumTest()
        {
            Vector64<long> vector = Vector64.Create((long)0x01);
            Assert.Equal((long)1, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64NIntSumTest()
        {
            Vector64<nint> vector = Vector64.Create((nint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nint)1, Vector64.Sum(vector));
            }
            else
            {
                Assert.Equal((nint)2, Vector64.Sum(vector));
            }
        }

        [Fact]
        public void Vector64NUIntSumTest()
        {
            Vector64<nuint> vector = Vector64.Create((nuint)0x01);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((nuint)1, Vector64.Sum(vector));
            }
            else
            {
                Assert.Equal((nuint)2, Vector64.Sum(vector));
            }
        }

        [Fact]
        public void Vector64SByteSumTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)0x01);
            Assert.Equal((sbyte)8, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64SingleSumTest()
        {
            Vector64<float> vector = Vector64.Create((float)0x01);
            Assert.Equal(2.0f, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64UInt16SumTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)0x01);
            Assert.Equal((ushort)4, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64UInt32SumTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)0x01);
            Assert.Equal((uint)2, Vector64.Sum(vector));
        }

        [Fact]
        public void Vector64UInt64SumTest()
        {
            Vector64<ulong> vector = Vector64.Create((ulong)0x01);
            Assert.Equal((ulong)1, Vector64.Sum(vector));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(0, 1, 2, 3, 4, 5, 6, 7, 8)]
        [InlineData(50, 430, int.MaxValue, int.MinValue)]
        public void Vector64Int32IndexerTest(params int[] values)
        {
            var vector = Vector64.Create(values);

            Assert.Equal(vector[0], values[0]);
            Assert.Equal(vector[1], values[1]);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L)]
        [InlineData(50L, 430L, long.MaxValue, long.MinValue)]
        public void Vector64Int64IndexerTest(params long[] values)
        {
            var vector = Vector64.Create(values);

            Assert.Equal(vector[0], values[0]);
        }
    }
}
