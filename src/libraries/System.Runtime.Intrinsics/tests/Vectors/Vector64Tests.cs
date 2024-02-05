// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    public sealed class Vector64Tests
    {
        /// <summary>Verifies that two <see cref="Vector64{Single}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector64<float> expected, Vector64<float> actual, Vector64<float> variance)
        {
            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                AssertExtensions.Equal(expected.GetElement(i), actual.GetElement(i), variance.GetElement(i));
            }
        }

        /// <summary>Verifies that two <see cref="Vector64{Double}" /> values are equal, within the <paramref name="variance" />.</summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="variance">The total variance allowed between the expected and actual results.</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        internal static void AssertEqual(Vector64<double> expected, Vector64<double> actual, Vector64<double> variance)
        {
            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                AssertExtensions.Equal(expected.GetElement(i), actual.GetElement(i), variance.GetElement(i));
            }
        }

        [Fact]
        public unsafe void Vector64IsHardwareAcceleratedTest()
        {
            MethodInfo methodInfo = typeof(Vector64).GetMethod("get_IsHardwareAccelerated");
            Assert.Equal(Vector64.IsHardwareAccelerated, methodInfo.Invoke(null, null));
        }

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
        public void Vector64ByteShuffleOneInputTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<byte> result = Vector64.Shuffle(vector, Vector64.Create((byte)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector64<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShuffleOneInputTest()
        {
            Vector64<short> vector = Vector64.Create((short)1, 2, 3, 4);
            Vector64<short> result = Vector64.Shuffle(vector, Vector64.Create((short)3, 2, 1, 0));

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)(Vector64<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShuffleOneInputTest()
        {
            Vector64<int> vector = Vector64.Create((int)1, 2);
            Vector64<int> result = Vector64.Shuffle(vector, Vector64.Create((int)1, 0));

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)(Vector64<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShuffleOneInputTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<sbyte> result = Vector64.Shuffle(vector, Vector64.Create((sbyte)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector64<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SingleShuffleOneInputTest()
        {
            Vector64<float> vector = Vector64.Create((float)1, 2);
            Vector64<float> result = Vector64.Shuffle(vector, Vector64.Create((int)1, 0));

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)(Vector64<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShuffleOneInputTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)1, 2, 3, 4);
            Vector64<ushort> result = Vector64.Shuffle(vector, Vector64.Create((ushort)3, 2, 1, 0));

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector64<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShuffleOneInputTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)1, 2);
            Vector64<uint> result = Vector64.Shuffle(vector, Vector64.Create((uint)1, 0));

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector64<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShuffleOneInputWithDirectVectorTest()
        {
            Vector64<byte> result = Vector64.Shuffle(Vector64.Create((byte)1, 2, 3, 4, 5, 6, 7, 8), Vector64.Create((byte)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector64<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShuffleOneInputWithDirectVectorTest()
        {
            Vector64<short> result = Vector64.Shuffle(Vector64.Create((short)1, 2, 3, 4), Vector64.Create((short)3, 2, 1, 0));

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)(Vector64<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShuffleOneInputWithDirectVectorTest()
        {
            Vector64<int> result = Vector64.Shuffle(Vector64.Create((int)1, 2), Vector64.Create((int)1, 0));

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)(Vector64<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShuffleOneInputWithDirectVectorTest()
        {
            Vector64<sbyte> result = Vector64.Shuffle(Vector64.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8), Vector64.Create((sbyte)7, 6, 5, 4, 3, 2, 1, 0));

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector64<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SingleShuffleOneInputWithDirectVectorTest()
        {
            Vector64<float> result = Vector64.Shuffle(Vector64.Create((float)1, 2), Vector64.Create((int)1, 0));

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)(Vector64<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShuffleOneInputWithDirectVectorTest()
        {
            Vector64<ushort> result = Vector64.Shuffle(Vector64.Create((ushort)1, 2, 3, 4), Vector64.Create((ushort)3, 2, 1, 0));

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector64<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShuffleOneInputWithDirectVectorTest()
        {
            Vector64<uint> result = Vector64.Shuffle(Vector64.Create((uint)1, 2), Vector64.Create((uint)1, 0));

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector64<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<byte> indices = Vector64.Create((byte)7, 6, 5, 4, 3, 2, 1, 0);
            Vector64<byte> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)(Vector64<byte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<short> vector = Vector64.Create((short)1, 2, 3, 4);
            Vector64<short> indices = Vector64.Create((short)3, 2, 1, 0);
            Vector64<short> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal((short)(Vector64<short>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<int> vector = Vector64.Create((int)1, 2);
            Vector64<int> indices = Vector64.Create((int)1, 0);
            Vector64<int> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)(Vector64<int>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<sbyte> indices = Vector64.Create((sbyte)7, 6, 5, 4, 3, 2, 1, 0);
            Vector64<sbyte> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)(Vector64<sbyte>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SingleShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<float> vector = Vector64.Create((float)1, 2);
            Vector64<int> indices = Vector64.Create((int)1, 0);
            Vector64<float> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)(Vector64<float>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)1, 2, 3, 4);
            Vector64<ushort> indices = Vector64.Create((ushort)3, 2, 1, 0);
            Vector64<ushort> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)(Vector64<ushort>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShuffleOneInputWithLocalIndicesTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)1, 2);
            Vector64<uint> indices = Vector64.Create((uint)1, 0);
            Vector64<uint> result = Vector64.Shuffle(vector, indices);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)(Vector64<uint>.Count - index), result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<byte> result = Vector64.Shuffle(vector, Vector64<byte>.AllBitsSet);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<short> vector = Vector64.Create((short)1, 2, 3, 4);
            Vector64<short> result = Vector64.Shuffle(vector, Vector64<short>.AllBitsSet);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal(0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<int> vector = Vector64.Create((int)1, 2);
            Vector64<int> result = Vector64.Shuffle(vector, Vector64<int>.AllBitsSet);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<sbyte> result = Vector64.Shuffle(vector, Vector64<sbyte>.AllBitsSet);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SingleShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<float> vector = Vector64.Create((float)1, 2);
            Vector64<float> result = Vector64.Shuffle(vector, Vector64<int>.AllBitsSet);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)1, 2, 3, 4);
            Vector64<ushort> result = Vector64.Shuffle(vector, Vector64<ushort>.AllBitsSet);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShuffleOneInputWithAllBitsSetIndicesTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)1, 2);
            Vector64<uint> result = Vector64.Shuffle(vector, Vector64<uint>.AllBitsSet);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)0, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64ByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<byte> vector = Vector64.Create((byte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<byte> result = Vector64.Shuffle(vector, Vector64<byte>.Zero);

            for (int index = 0; index < Vector64<byte>.Count; index++)
            {
                Assert.Equal((byte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<short> vector = Vector64.Create((short)1, 2, 3, 4);
            Vector64<short> result = Vector64.Shuffle(vector, Vector64<short>.Zero);

            for (int index = 0; index < Vector64<short>.Count; index++)
            {
                Assert.Equal(1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64Int32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<int> vector = Vector64.Create((int)1, 2);
            Vector64<int> result = Vector64.Shuffle(vector, Vector64<int>.Zero);

            for (int index = 0; index < Vector64<int>.Count; index++)
            {
                Assert.Equal((int)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SByteShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<sbyte> vector = Vector64.Create((sbyte)1, 2, 3, 4, 5, 6, 7, 8);
            Vector64<sbyte> result = Vector64.Shuffle(vector, Vector64<sbyte>.Zero);

            for (int index = 0; index < Vector64<sbyte>.Count; index++)
            {
                Assert.Equal((sbyte)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64SingleShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<float> vector = Vector64.Create((float)1, 2);
            Vector64<float> result = Vector64.Shuffle(vector, Vector64<int>.Zero);

            for (int index = 0; index < Vector64<float>.Count; index++)
            {
                Assert.Equal((float)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt16ShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<ushort> vector = Vector64.Create((ushort)1, 2, 3, 4);
            Vector64<ushort> result = Vector64.Shuffle(vector, Vector64<ushort>.Zero);

            for (int index = 0; index < Vector64<ushort>.Count; index++)
            {
                Assert.Equal((ushort)1, result.GetElement(index));
            }
        }

        [Fact]
        public void Vector64UInt32ShuffleOneInputWithZeroIndicesTest()
        {
            Vector64<uint> vector = Vector64.Create((uint)1, 2);
            Vector64<uint> result = Vector64.Shuffle(vector, Vector64<uint>.Zero);

            for (int index = 0; index < Vector64<uint>.Count; index++)
            {
                Assert.Equal((uint)1, result.GetElement(index));
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

        [Fact]
        public void Vector64DoubleEqualsNaNTest()
        {
            Vector64<double> nan = Vector64.Create(double.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector64SingleEqualsNaNTest()
        {
            Vector64<float> nan = Vector64.Create(float.NaN);
            Assert.True(nan.Equals(nan));
        }

        [Fact]
        public void Vector64DoubleEqualsNonCanonicalNaNTest()
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
                    Assert.True(Vector64.Create(i).Equals(Vector64.Create(j)));
                    Assert.False(Vector64.Create(i) == Vector64.Create(j));
                }
            }
        }

        [Fact]
        public void Vector64SingleEqualsNonCanonicalNaNTest()
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
                    Assert.True(Vector64.Create(i).Equals(Vector64.Create(j)));
                    Assert.False(Vector64.Create(i) == Vector64.Create(j));
                }
            }
        }

        [Fact]
        public void Vector64SingleCreateFromArrayTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f];
            Vector64<float> vector = Vector64.Create(array);
            Assert.Equal(Vector64.Create(1.0f, 2.0f), vector);
        }

        [Fact]
        public void Vector64SingleCreateFromArrayOffsetTest()
        {
            float[] array = [1.0f, 2.0f, 3.0f];
            Vector64<float> vector = Vector64.Create(array, 1);
            Assert.Equal(Vector64.Create(2.0f, 3.0f), vector);
        }

        [Fact]
        public void Vector64SingleCopyToTest()
        {
            float[] array = new float[2];
            Vector64.Create(2.0f).CopyTo(array);
            Assert.True(array.AsSpan().SequenceEqual([2.0f, 2.0f]));
        }

        [Fact]
        public void Vector64SingleCopyToOffsetTest()
        {
            float[] array = new float[3];
            Vector64.Create(2.0f).CopyTo(array, 1);
            Assert.True(array.AsSpan().SequenceEqual([0.0f, 2.0f, 2.0f]));
        }

        [Fact]
        public void Vector64SByteAbs_MinValue()
        {
            Vector64<sbyte> vector = Vector64.Create(sbyte.MinValue);
            Vector64<sbyte> abs = Vector64.Abs(vector);
            for (int index = 0; index < Vector64<sbyte>.Count; index++)
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
            Assert.True(Vector64<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector64<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
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
            Assert.False(Vector64<T>.IsSupported);

            MethodInfo methodInfo = typeof(Vector64<T>).GetProperty("IsSupported", BindingFlags.Public | BindingFlags.Static).GetMethod;
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
            Assert.Equal(Vector64<T>.One, Vector64.Create(T.One));

            MethodInfo methodInfo = typeof(Vector64<T>).GetProperty("One", BindingFlags.Public | BindingFlags.Static).GetMethod;
            Assert.Equal((Vector64<T>)methodInfo.Invoke(null, null), Vector64.Create(T.One));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.ExpDouble), MemberType = typeof(VectorTestMemberData))]
        public void ExpDoubleTest(double value, double expectedResult, double variance)
        {
            Vector64<double> actualResult = Vector64.Exp(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.ExpSingle), MemberType = typeof(VectorTestMemberData))]
        public void ExpSingleTest(float value, float expectedResult, float variance)
        {
            Vector64<float> actualResult = Vector64.Exp(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogDouble), MemberType = typeof(VectorTestMemberData))]
        public void LogDoubleTest(double value, double expectedResult, double variance)
        {
            Vector64<double> actualResult = Vector64.Log(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.LogSingle), MemberType = typeof(VectorTestMemberData))]
        public void LogSingleTest(float value, float expectedResult, float variance)
        {
            Vector64<float> actualResult = Vector64.Log(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Double), MemberType = typeof(VectorTestMemberData))]
        public void Log2DoubleTest(double value, double expectedResult, double variance)
        {
            Vector64<double> actualResult = Vector64.Log2(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }

        [Theory]
        [MemberData(nameof(VectorTestMemberData.Log2Single), MemberType = typeof(VectorTestMemberData))]
        public void Log2SingleTest(float value, float expectedResult, float variance)
        {
            Vector64<float> actualResult = Vector64.Log2(Vector64.Create(value));
            AssertEqual(Vector64.Create(expectedResult), actualResult, Vector64.Create(variance));
        }
    }
}
