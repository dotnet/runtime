// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

namespace System.Numerics.Tests
{
    public static class BitOperationsTests
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(0b1, true)]
        [InlineData(0b10, true)]
        [InlineData(0b100, true)]
        [InlineData(0b1000, true)]
        [InlineData(0b10000, true)]
        [InlineData(0b100000, true)]
        [InlineData(0b1000000, true)]
        [InlineData(-0b1000000, false)]
        [InlineData(0b1000001, false)]
        [InlineData(0b1010001, false)]
        [InlineData(0b1111111, false)]
        [InlineData(-1, false)]
        [InlineData(int.MaxValue, false)]
        [InlineData(int.MinValue, false)]
        public static void BitOps_IsPow2_int(int n, bool expected)
        {
            bool actual = BitOperations.IsPow2(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0u, false)]
        [InlineData(0b1u, true)]
        [InlineData(0b10u, true)]
        [InlineData(0b100u, true)]
        [InlineData(0b1000u, true)]
        [InlineData(0b10000u, true)]
        [InlineData(0b100000u, true)]
        [InlineData(0b1000000u, true)]
        [InlineData(0b1000001u, false)]
        [InlineData(0b1010001u, false)]
        [InlineData(0b1111111u, false)]
        [InlineData(uint.MaxValue, false)]
        [InlineData(unchecked((uint)int.MinValue), true)]
        public static void BitOps_IsPow2_uint(uint n, bool expected)
        {
            bool actual = BitOperations.IsPow2(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0L, false)]
        [InlineData(0b1L, true)]
        [InlineData(0b10L, true)]
        [InlineData(0b100L, true)]
        [InlineData(0b1000L, true)]
        [InlineData(0b10000L, true)]
        [InlineData(0b100000L, true)]
        [InlineData(0b1000000L, true)]
        [InlineData(-0b1000000L, false)]
        [InlineData(0b1000001L, false)]
        [InlineData(0b1010001L, false)]
        [InlineData(0b1111111L, false)]
        [InlineData(-1L, false)]
        [InlineData(long.MaxValue, false)]
        [InlineData(long.MinValue, false)]
        public static void BitOps_IsPow2_long(long n, bool expected)
        {
            bool actual = BitOperations.IsPow2(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0ul, false)]
        [InlineData(0b1ul, true)]
        [InlineData(0b10ul, true)]
        [InlineData(0b100ul, true)]
        [InlineData(0b1000ul, true)]
        [InlineData(0b10000ul, true)]
        [InlineData(0b100000ul, true)]
        [InlineData(0b1000000ul, true)]
        [InlineData(0b1000001ul, false)]
        [InlineData(0b1010001ul, false)]
        [InlineData(0b1111111ul, false)]
        [InlineData(ulong.MaxValue, false)]
        [InlineData(unchecked((ulong)long.MinValue), true)]
        public static void BitOps_IsPow2_ulong(ulong n, bool expected)
        {
            bool actual = BitOperations.IsPow2(n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0, false)]
        [InlineData(0b1, true)]
        [InlineData(0b10, true)]
        [InlineData(0b100, true)]
        [InlineData(0b1000, true)]
        [InlineData(0b10000, true)]
        [InlineData(0b100000, true)]
        [InlineData(0b1000000, true)]
        [InlineData(-0b1000000, false)]
        [InlineData(0b1000001, false)]
        [InlineData(0b1010001, false)]
        [InlineData(0b1111111, false)]
        [InlineData(-1, false)]
        [InlineData(int.MaxValue, false)]
        [InlineData(int.MinValue, false)]
        public static void BitOps_IsPow2_nint_32(int n, bool expected)
        {
            bool actual = BitOperations.IsPow2((nint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0, false)]
        [InlineData(0b1, true)]
        [InlineData(0b10, true)]
        [InlineData(0b100, true)]
        [InlineData(0b1000, true)]
        [InlineData(0b10000, true)]
        [InlineData(0b100000, true)]
        [InlineData(0b1000000, true)]
        [InlineData(-0b1000000, false)]
        [InlineData(0b1000001, false)]
        [InlineData(0b1010001, false)]
        [InlineData(0b1111111, false)]
        [InlineData(-1, false)]
        [InlineData(long.MaxValue, false)]
        [InlineData(long.MinValue, false)]
        public static void BitOps_IsPow2_nint_64(long n, bool expected)
        {
            bool actual = BitOperations.IsPow2((nint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0u, false)]
        [InlineData(0b1u, true)]
        [InlineData(0b10u, true)]
        [InlineData(0b100u, true)]
        [InlineData(0b1000u, true)]
        [InlineData(0b10000u, true)]
        [InlineData(0b100000u, true)]
        [InlineData(0b1000000u, true)]
        [InlineData(0b1000001u, false)]
        [InlineData(0b1010001u, false)]
        [InlineData(0b1111111u, false)]
        [InlineData(unchecked((uint)int.MinValue), true)]
        [InlineData(uint.MaxValue, false)]
        public static void BitOps_IsPow2_nuint_32(uint n, bool expected)
        {
            bool actual = BitOperations.IsPow2((nuint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0u, false)]
        [InlineData(0b1u, true)]
        [InlineData(0b10u, true)]
        [InlineData(0b100u, true)]
        [InlineData(0b1000u, true)]
        [InlineData(0b10000u, true)]
        [InlineData(0b100000u, true)]
        [InlineData(0b1000000u, true)]
        [InlineData(0b1000001u, false)]
        [InlineData(0b1010001u, false)]
        [InlineData(0b1111111u, false)]
        [InlineData(unchecked((uint)int.MinValue), true)]
        [InlineData(ulong.MaxValue, false)]
        public static void BitOps_IsPow2_nuint_64(ulong n, bool expected)
        {
            bool actual = BitOperations.IsPow2((nuint) n);
            Assert.Equal(expected, actual);
        }


        [Theory]
        [InlineData(0u, 32)]
        [InlineData(0b1u, 31)]
        [InlineData(0b10u, 30)]
        [InlineData(0b100u, 29)]
        [InlineData(0b1000u, 28)]
        [InlineData(0b10000u, 27)]
        [InlineData(0b100000u, 26)]
        [InlineData(0b1000000u, 25)]
        [InlineData(byte.MaxValue << 17, 32 - 8 - 17)]
        [InlineData(byte.MaxValue << 9, 32 - 8 - 9)]
        [InlineData(ushort.MaxValue << 11, 32 - 16 - 11)]
        [InlineData(ushort.MaxValue << 2, 32 - 16 - 2)]
        [InlineData(5 << 7, 32 - 3 - 7)]
        [InlineData(uint.MaxValue, 0)]
        public static void BitOps_LeadingZeroCount_uint(uint n, int expected)
        {
            int actual = BitOperations.LeadingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0ul, 64)]
        [InlineData(0b1ul, 63)]
        [InlineData(0b10ul, 62)]
        [InlineData(0b100ul, 61)]
        [InlineData(0b1000ul, 60)]
        [InlineData(0b10000ul, 59)]
        [InlineData(0b100000ul, 58)]
        [InlineData(0b1000000ul, 57)]
        [InlineData((ulong)byte.MaxValue << 41, 64 - 8 - 41)]
        [InlineData((ulong)byte.MaxValue << 53, 64 - 8 - 53)]
        [InlineData((ulong)ushort.MaxValue << 31, 64 - 16 - 31)]
        [InlineData((ulong)ushort.MaxValue << 15, 64 - 16 - 15)]
        [InlineData(ulong.MaxValue >> 5, 5)]
        [InlineData(1ul << 63, 0)]
        [InlineData(1ul << 62, 1)]
        [InlineData(ulong.MaxValue, 0)]
        public static void BitOps_LeadingZeroCount_ulong(ulong n, int expected)
        {
            int actual = BitOperations.LeadingZeroCount(n);
            Assert.Equal(expected, actual);
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0u, 32)]
        [InlineData(0b1u, 31)]
        [InlineData(0b10u, 30)]
        [InlineData(0b100u, 29)]
        [InlineData(0b1000u, 28)]
        [InlineData(0b10000u, 27)]
        [InlineData(0b100000u, 26)]
        [InlineData(0b1000000u, 25)]
        [InlineData(byte.MaxValue << 17, 32 - 8 - 17)]
        [InlineData(byte.MaxValue << 9, 32 - 8 - 9)]
        [InlineData(ushort.MaxValue << 11, 32 - 16 - 11)]
        [InlineData(ushort.MaxValue << 2, 32 - 16 - 2)]
        [InlineData(5 << 7, 32 - 3 - 7)]
        [InlineData(uint.MaxValue, 0)]
        public static void BitOps_LeadingZeroCount_nuint_32(uint n, int expected)
        {
            int actual = BitOperations.LeadingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0ul, 64)]
        [InlineData(0b1ul, 63)]
        [InlineData(0b10ul, 62)]
        [InlineData(0b100ul, 61)]
        [InlineData(0b1000ul, 60)]
        [InlineData(0b10000ul, 59)]
        [InlineData(0b100000ul, 58)]
        [InlineData(0b1000000ul, 57)]
        [InlineData((ulong)byte.MaxValue << 41, 64 - 8 - 41)]
        [InlineData((ulong)byte.MaxValue << 53, 64 - 8 - 53)]
        [InlineData((ulong)ushort.MaxValue << 31, 64 - 16 - 31)]
        [InlineData((ulong)ushort.MaxValue << 15, 64 - 16 - 15)]
        [InlineData(ulong.MaxValue >> 5, 5)]
        [InlineData(1ul << 63, 0)]
        [InlineData(1ul << 62, 1)]
        [InlineData(ulong.MaxValue, 0)]
        public static void BitOps_LeadingZeroCount_nuint_64(ulong n, int expected)
        {
            int actual = BitOperations.LeadingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0u, 32)]
        [InlineData(0b1u, 0)]
        [InlineData(0b10u, 1)]
        [InlineData(0b100u, 2)]
        [InlineData(0b1000u, 3)]
        [InlineData(0b10000u, 4)]
        [InlineData(0b100000u, 5)]
        [InlineData(0b1000000u, 6)]
        [InlineData((uint)byte.MaxValue << 24, 24)]
        [InlineData((uint)byte.MaxValue << 22, 22)]
        [InlineData((uint)ushort.MaxValue << 16, 16)]
        [InlineData((uint)ushort.MaxValue << 19, 19)]
        [InlineData(uint.MaxValue << 5, 5)]
        [InlineData(3u << 27, 27)]
        [InlineData(uint.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_uint(uint n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 32)]
        [InlineData(0b1, 0)]
        [InlineData(0b10, 1)]
        [InlineData(0b100, 2)]
        [InlineData(0b1000, 3)]
        [InlineData(0b10000, 4)]
        [InlineData(0b100000, 5)]
        [InlineData(0b1000000, 6)]
        [InlineData(byte.MaxValue << 24, 24)]
        [InlineData(byte.MaxValue << 22, 22)]
        [InlineData(ushort.MaxValue << 16, 16)]
        [InlineData(ushort.MaxValue << 19, 19)]
        [InlineData(int.MaxValue << 5, 5)]
        [InlineData(3 << 27, 27)]
        [InlineData(int.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_int(int n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0ul, 64)]
        [InlineData(0b1ul, 0)]
        [InlineData(0b10ul, 1)]
        [InlineData(0b100ul, 2)]
        [InlineData(0b1000ul, 3)]
        [InlineData(0b10000ul, 4)]
        [InlineData(0b100000ul, 5)]
        [InlineData(0b1000000ul, 6)]
        [InlineData((ulong)byte.MaxValue << 40, 40)]
        [InlineData((ulong)byte.MaxValue << 57, 57)]
        [InlineData((ulong)ushort.MaxValue << 31, 31)]
        [InlineData((ulong)ushort.MaxValue << 15, 15)]
        [InlineData(ulong.MaxValue << 5, 5)]
        [InlineData(3ul << 59, 59)]
        [InlineData(5ul << 63, 63)]
        [InlineData(ulong.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_ulong(ulong n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0L, 64)]
        [InlineData(0b1L, 0)]
        [InlineData(0b10L, 1)]
        [InlineData(0b100L, 2)]
        [InlineData(0b1000L, 3)]
        [InlineData(0b10000L, 4)]
        [InlineData(0b100000L, 5)]
        [InlineData(0b1000000L, 6)]
        [InlineData((long)byte.MaxValue << 40, 40)]
        [InlineData((long)byte.MaxValue << 57, 57)]
        [InlineData((long)ushort.MaxValue << 31, 31)]
        [InlineData((long)ushort.MaxValue << 15, 15)]
        [InlineData(long.MaxValue << 5, 5)]
        [InlineData(3L << 59, 59)]
        [InlineData(5L << 63, 63)]
        [InlineData(long.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_long(long n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount(n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0u, 32)]
        [InlineData(0b1u, 0)]
        [InlineData(0b10u, 1)]
        [InlineData(0b100u, 2)]
        [InlineData(0b1000u, 3)]
        [InlineData(0b10000u, 4)]
        [InlineData(0b100000u, 5)]
        [InlineData(0b1000000u, 6)]
        [InlineData((uint)byte.MaxValue << 24, 24)]
        [InlineData((uint)byte.MaxValue << 22, 22)]
        [InlineData((uint)ushort.MaxValue << 16, 16)]
        [InlineData((uint)ushort.MaxValue << 19, 19)]
        [InlineData(uint.MaxValue << 5, 5)]
        [InlineData(3u << 27, 27)]
        [InlineData(uint.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_nuint_32(uint n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount((nuint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0ul, 64)]
        [InlineData(0b1ul, 0)]
        [InlineData(0b10ul, 1)]
        [InlineData(0b100ul, 2)]
        [InlineData(0b1000ul, 3)]
        [InlineData(0b10000ul, 4)]
        [InlineData(0b100000ul, 5)]
        [InlineData(0b1000000ul, 6)]
        [InlineData((ulong)byte.MaxValue << 40, 40)]
        [InlineData((ulong)byte.MaxValue << 57, 57)]
        [InlineData((ulong)ushort.MaxValue << 31, 31)]
        [InlineData((ulong)ushort.MaxValue << 15, 15)]
        [InlineData(ulong.MaxValue << 5, 5)]
        [InlineData(3ul << 59, 59)]
        [InlineData(5ul << 63, 63)]
        [InlineData(ulong.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_nuint_64(ulong n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount((nuint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0, 32)]
        [InlineData(0b1, 0)]
        [InlineData(0b10, 1)]
        [InlineData(0b100, 2)]
        [InlineData(0b1000, 3)]
        [InlineData(0b10000, 4)]
        [InlineData(0b100000, 5)]
        [InlineData(0b1000000, 6)]
        [InlineData(byte.MaxValue << 24, 24)]
        [InlineData(byte.MaxValue << 22, 22)]
        [InlineData(ushort.MaxValue << 16, 16)]
        [InlineData(ushort.MaxValue << 19, 19)]
        [InlineData(int.MaxValue << 5, 5)]
        [InlineData(3 << 27, 27)]
        [InlineData(int.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_nint_32(int n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount((nint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0L, 64)]
        [InlineData(0b1L, 0)]
        [InlineData(0b10L, 1)]
        [InlineData(0b100L, 2)]
        [InlineData(0b1000L, 3)]
        [InlineData(0b10000L, 4)]
        [InlineData(0b100000L, 5)]
        [InlineData(0b1000000L, 6)]
        [InlineData((long)byte.MaxValue << 40, 40)]
        [InlineData((long)byte.MaxValue << 57, 57)]
        [InlineData((long)ushort.MaxValue << 31, 31)]
        [InlineData((long)ushort.MaxValue << 15, 15)]
        [InlineData(long.MaxValue << 5, 5)]
        [InlineData(3L << 59, 59)]
        [InlineData(5L << 63, 63)]
        [InlineData(long.MaxValue, 0)]
        public static void BitOps_TrailingZeroCount_nint_64(long n, int expected)
        {
            int actual = BitOperations.TrailingZeroCount((nint) n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2 - 1)]
        [InlineData(4, 2)]
        [InlineData(5, 3 - 1)]
        [InlineData(6, 3 - 1)]
        [InlineData(7, 3 - 1)]
        [InlineData(8, 3)]
        [InlineData(9, 4 - 1)]
        [InlineData(byte.MaxValue, 8 - 1)]
        [InlineData(ushort.MaxValue, 16 - 1)]
        [InlineData(uint.MaxValue, 32 - 1)]
        public static void BitOps_Log2_uint(uint n, int expected)
        {
            int actual = BitOperations.Log2(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2 - 1)]
        [InlineData(4, 2)]
        [InlineData(5, 3 - 1)]
        [InlineData(6, 3 - 1)]
        [InlineData(7, 3 - 1)]
        [InlineData(8, 3)]
        [InlineData(9, 4 - 1)]
        [InlineData(byte.MaxValue, 8 - 1)]
        [InlineData(ushort.MaxValue, 16 - 1)]
        [InlineData(uint.MaxValue, 32 - 1)]
        [InlineData(ulong.MaxValue, 64 - 1)]
        public static void BitOps_Log2_ulong(ulong n, int expected)
        {
            int actual = BitOperations.Log2(n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2 - 1)]
        [InlineData(4, 2)]
        [InlineData(5, 3 - 1)]
        [InlineData(6, 3 - 1)]
        [InlineData(7, 3 - 1)]
        [InlineData(8, 3)]
        [InlineData(9, 4 - 1)]
        [InlineData(byte.MaxValue, 8 - 1)]
        [InlineData(ushort.MaxValue, 16 - 1)]
        [InlineData(uint.MaxValue, 32 - 1)]
        public static void BitOps_Log2_nuint_32(uint n, int expected)
        {
            int actual = BitOperations.Log2((nuint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2 - 1)]
        [InlineData(4, 2)]
        [InlineData(5, 3 - 1)]
        [InlineData(6, 3 - 1)]
        [InlineData(7, 3 - 1)]
        [InlineData(8, 3)]
        [InlineData(9, 4 - 1)]
        [InlineData(byte.MaxValue, 8 - 1)]
        [InlineData(ushort.MaxValue, 16 - 1)]
        [InlineData(uint.MaxValue, 32 - 1)]
        [InlineData(ulong.MaxValue, 64 - 1)]
        public static void BitOps_Log2_nuint_64(ulong n, int expected)
        {
            int actual = BitOperations.Log2((nuint) n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0b001, 1)]
        [InlineData(0b010, 1)]
        [InlineData(0b011, 2)]
        [InlineData(0b100, 1)]
        [InlineData(0b101, 2)]
        [InlineData(0b110, 2)]
        [InlineData(0b111, 3)]
        [InlineData(0b1101, 3)]
        [InlineData(0b1111, 4)]
        [InlineData(0b10111, 4)]
        [InlineData(0b11111, 5)]
        [InlineData(0b110111, 5)]
        [InlineData(0b111111, 6)]
        [InlineData(0b1111110, 6)]
        [InlineData(byte.MinValue, 0)] // 0
        [InlineData(byte.MaxValue, 8)] // 255
        [InlineData(unchecked((uint)sbyte.MinValue), 25)] // 4294967168
        [InlineData(sbyte.MaxValue, 7)] // 127
        [InlineData(ushort.MaxValue >> 3, 16 - 3)] // 8191
        [InlineData(ushort.MaxValue, 16)] // 65535
        [InlineData(unchecked((uint)short.MinValue), 32 - 15)] // 4294934528
        [InlineData(short.MaxValue, 15)] // 32767
        [InlineData(unchecked((uint)int.MinValue), 1)] // 2147483648
        [InlineData(unchecked((uint)int.MaxValue), 31)] // 4294967168
        [InlineData(uint.MaxValue >> 5, 32 - 5)] // 134217727
        [InlineData(uint.MaxValue << 11, 32 - 11)] // 4294965248
        [InlineData(uint.MaxValue, 32)] // 4294967295
        public static void BitOps_PopCount_uint(uint n, int expected)
        {
            int actual = BitOperations.PopCount(n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0b001, 1)]
        [InlineData(0b010, 1)]
        [InlineData(0b011, 2)]
        [InlineData(0b100, 1)]
        [InlineData(0b101, 2)]
        [InlineData(0b110, 2)]
        [InlineData(0b111, 3)]
        [InlineData(0b1101, 3)]
        [InlineData(0b1111, 4)]
        [InlineData(0b10111, 4)]
        [InlineData(0b11111, 5)]
        [InlineData(0b110111, 5)]
        [InlineData(0b111111, 6)]
        [InlineData(0b1111110, 6)]
        [InlineData(0b1111111, 7)]
        [InlineData(byte.MinValue, 0)] // 0
        [InlineData(byte.MaxValue, 8)] // 255
        [InlineData(unchecked((ulong)sbyte.MinValue), 57)] // 18446744073709551488
        [InlineData(sbyte.MaxValue, 7)] // 127
        [InlineData(ushort.MaxValue, 16)] // 65535
        [InlineData(unchecked((ulong)short.MinValue), 49)] // 18446744073709518848
        [InlineData(short.MaxValue, 15)] // 32767
        [InlineData(unchecked((ulong)int.MinValue), 64 - 31)] // 18446744071562067968
        [InlineData(int.MaxValue, 31)] // 2147483647
        [InlineData(ulong.MaxValue >> 9, 64 - 9)] // 36028797018963967
        [InlineData(ulong.MaxValue << 11, 64 - 11)] // 18446744073709549568
        [InlineData(ulong.MaxValue, 64)]
        public static void BitOps_PopCount_ulong(ulong n, int expected)
        {
            int actual = BitOperations.PopCount(n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0b001, 1)]
        [InlineData(0b010, 1)]
        [InlineData(0b011, 2)]
        [InlineData(0b100, 1)]
        [InlineData(0b101, 2)]
        [InlineData(0b110, 2)]
        [InlineData(0b111, 3)]
        [InlineData(0b1101, 3)]
        [InlineData(0b1111, 4)]
        [InlineData(0b10111, 4)]
        [InlineData(0b11111, 5)]
        [InlineData(0b110111, 5)]
        [InlineData(0b111111, 6)]
        [InlineData(0b1111110, 6)]
        public static void BitOps_PopCount_nuint_32(uint n, int expected)
        {
            int actual = BitOperations.PopCount((nuint) n);
            Assert.Equal(expected, actual);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0b001, 1)]
        [InlineData(0b010, 1)]
        [InlineData(0b011, 2)]
        [InlineData(0b100, 1)]
        [InlineData(0b101, 2)]
        [InlineData(0b110, 2)]
        [InlineData(0b111, 3)]
        [InlineData(0b1101, 3)]
        [InlineData(0b1111, 4)]
        [InlineData(0b10111, 4)]
        [InlineData(0b11111, 5)]
        [InlineData(0b110111, 5)]
        [InlineData(0b111111, 6)]
        [InlineData(0b1111110, 6)]
        [InlineData(0b1111111, 7)]
        [InlineData(byte.MinValue, 0)] // 0
        [InlineData(byte.MaxValue, 8)] // 255
        [InlineData(unchecked((ulong)sbyte.MinValue), 57)] // 18446744073709551488
        [InlineData(sbyte.MaxValue, 7)] // 127
        [InlineData(ushort.MaxValue, 16)] // 65535
        [InlineData(unchecked((ulong)short.MinValue), 49)] // 18446744073709518848
        [InlineData(short.MaxValue, 15)] // 32767
        [InlineData(unchecked((ulong)int.MinValue), 64 - 31)] // 18446744071562067968
        [InlineData(int.MaxValue, 31)] // 2147483647
        [InlineData(ulong.MaxValue >> 9, 64 - 9)] // 36028797018963967
        [InlineData(ulong.MaxValue << 11, 64 - 11)] // 18446744073709549568
        [InlineData(ulong.MaxValue, 64)]
        public static void BitOps_PopCount_nuint_64(ulong n, int expected)
        {
            int actual = BitOperations.PopCount((nuint)n);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0b00000000_00000000_00000000_00000001u, int.MaxValue, 0b10000000_00000000_00000000_00000000u)] // % 32 = 31
        [InlineData(0b01000000_00000001_00000000_00000001u, 3, 0b00000000_00001000_00000000_00001010u)]
        [InlineData(0b01000000_00000001_00000000_00000001u, 2, 0b00000000_00000100_00000000_00000101u)]
        [InlineData(0b01010101_01010101_01010101_01010101u, 1, 0b10101010_10101010_10101010_10101010u)]
        [InlineData(0b01010101_11111111_01010101_01010101u, 0, 0b01010101_11111111_01010101_01010101u)]
        [InlineData(0b00000000_00000000_00000000_00000001u, -1, 0b10000000_00000000_00000000_00000000u)]
        [InlineData(0b00000000_00000000_00000000_00000001u, -2, 0b01000000_00000000_00000000_00000000u)]
        [InlineData(0b00000000_00000000_00000000_00000001u, -3, 0b00100000_00000000_00000000_00000000u)]
        [InlineData(0b01010101_11111111_01010101_01010101u, int.MinValue, 0b01010101_11111111_01010101_01010101u)] // % 32 = 0
        public static void BitOps_RotateLeft_uint(uint n, int offset, uint expected)
        {
            Assert.Equal(expected, BitOperations.RotateLeft(n, offset));
        }

        [Fact]
        public static void BitOps_RotateLeft_nuint()
        {
            unchecked
            {
                if (Environment.Is64BitProcess)
                {
                    nuint value = (nuint)0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul;
                    Assert.Equal((nuint)0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul,
                        BitOperations.RotateLeft(value, 1));
                    Assert.Equal((nuint)0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul,
                        BitOperations.RotateLeft(value, 2));
                    Assert.Equal((nuint)0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul,
                        BitOperations.RotateLeft(value, 3));
                    Assert.Equal(value, BitOperations.RotateLeft(value, int.MinValue)); // % 64 = 0
                    Assert.Equal(BitOperations.RotateLeft(value, 63),
                        BitOperations.RotateLeft(value, int.MaxValue)); // % 64 = 63
                }
                else
                {
                    Assert.Equal((nuint)0b10000000_00000000_00000000_00000000u,
                        BitOperations.RotateLeft((nuint)0b00000000_00000000_00000000_00000001u,
                            int.MaxValue)); // % 32 = 31
                    Assert.Equal((nuint)0b00000000_00001000_00000000_00001010u,
                        BitOperations.RotateLeft((nuint)0b01000000_00000001_00000000_00000001u, 3));
                    Assert.Equal((nuint)0b00000000_00000100_00000000_00000101u,
                        BitOperations.RotateLeft((nuint)0b01000000_00000001_00000000_00000001u, 2));
                    Assert.Equal((nuint)0b10101010_10101010_10101010_10101010u,
                        BitOperations.RotateLeft((nuint)0b01010101_01010101_01010101_01010101u, 1));
                    Assert.Equal((nuint)0b01010101_11111111_01010101_01010101u,
                        BitOperations.RotateLeft((nuint)0b01010101_11111111_01010101_01010101u, 0));
                    Assert.Equal((nuint)0b10000000_00000000_00000000_00000000u,
                        BitOperations.RotateLeft((nuint)0b00000000_00000000_00000000_00000001u, -1));
                    Assert.Equal((nuint)0b01000000_00000000_00000000_00000000u,
                        BitOperations.RotateLeft((nuint)0b00000000_00000000_00000000_00000001u, -2));
                    Assert.Equal((nuint)0b00100000_00000000_00000000_00000000u,
                        BitOperations.RotateLeft((nuint)0b00000000_00000000_00000000_00000001u, -3));
                    Assert.Equal((nuint)0b01010101_11111111_01010101_01010101u,
                        BitOperations.RotateLeft((nuint)0b01010101_11111111_01010101_01010101u,
                            int.MinValue)); // % 32 = 0
                }
            }
        }

        [Fact]
        public static void BitOps_RotateLeft_ulong()
        {
            ulong value = 0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul;
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul, BitOperations.RotateLeft(value, 1));
            Assert.Equal(0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul, BitOperations.RotateLeft(value, 2));
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul, BitOperations.RotateLeft(value, 3));
            Assert.Equal(value, BitOperations.RotateLeft(value, int.MinValue)); // % 64 = 0
            Assert.Equal(BitOperations.RotateLeft(value, 63), BitOperations.RotateLeft(value, int.MaxValue)); // % 64 = 63
        }

        [Theory]
        [InlineData(0b10000000_00000000_00000000_00000000u, int.MaxValue, 0b00000000_00000000_00000000_00000001u)] // % 32 = 31
        [InlineData(0b00000000_00001000_00000000_00001010u, 3, 0b01000000_00000001_00000000_00000001u)]
        [InlineData(0b00000000_00000100_00000000_00000101u, 2, 0b01000000_00000001_00000000_00000001u)]
        [InlineData(0b01010101_01010101_01010101_01010101u, 1, 0b10101010_10101010_10101010_10101010u)]
        [InlineData(0b01010101_11111111_01010101_01010101u, 0, 0b01010101_11111111_01010101_01010101u)]
        [InlineData(0b10000000_00000000_00000000_00000000u, -1, 0b00000000_00000000_00000000_00000001u)]
        [InlineData(0b00000000_00000000_00000000_00000001u, -2, 0b00000000_00000000_00000000_00000100u)]
        [InlineData(0b01000000_00000000_00000000_00000000u, -3, 0b00000000_00000000_00000000_00000010u)]
        [InlineData(0b01010101_11111111_01010101_01010101u, int.MinValue, 0b01010101_11111111_01010101_01010101u)] // % 32 = 0
        public static void BitOps_RotateRight_uint(uint n, int offset, uint expected)
        {
            Assert.Equal(expected, BitOperations.RotateRight(n, offset));
        }

        [Fact]
        public static void BitOps_RotateRight_ulong()
        {
            ulong value = 0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul;
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul, BitOperations.RotateRight(value, 1));
            Assert.Equal(0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul, BitOperations.RotateRight(value, 2));
            Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul, BitOperations.RotateRight(value, 3));
            Assert.Equal(value, BitOperations.RotateRight(value, int.MinValue)); // % 64 = 0
            Assert.Equal(BitOperations.RotateLeft(value, 63), BitOperations.RotateRight(value, int.MaxValue)); // % 64 = 63
        }

        [Fact]
        public static void BitOps_RotateRight_nuint()
        {
            unchecked
            {
                if (Environment.Is64BitProcess)
                {
                    const ulong value = 0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul;
                    Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul,
                        BitOperations.RotateRight(value, 1));
                    Assert.Equal(0b01010101_01010101_01010101_01010101_01010101_01010101_01010101_01010101ul,
                        BitOperations.RotateRight(value, 2));
                    Assert.Equal(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010ul,
                        BitOperations.RotateRight(value, 3));
                    Assert.Equal(value, BitOperations.RotateRight(value, int.MinValue)); // % 64 = 0
                    Assert.Equal(BitOperations.RotateLeft(value, 63),
                        BitOperations.RotateRight(value, int.MaxValue)); // % 64 = 63
                }
                else
                {
                    Assert.Equal((nuint)0b00000000_00000000_00000000_00000001u,
                        BitOperations.RotateRight((nuint)0b10000000_00000000_00000000_00000000u,
                            int.MaxValue)); // % 32 = 31
                    Assert.Equal((nuint)0b01000000_00000001_00000000_00000001u,
                        BitOperations.RotateRight((nuint)0b00000000_00001000_00000000_00001010u, 3));
                    Assert.Equal((nuint)0b01000000_00000001_00000000_00000001u,
                        BitOperations.RotateRight((nuint)0b00000000_00000100_00000000_00000101u, 2));
                    Assert.Equal((nuint)0b10101010_10101010_10101010_10101010u,
                        BitOperations.RotateRight((nuint)0b01010101_01010101_01010101_01010101u, 1));
                    Assert.Equal((nuint)0b01010101_11111111_01010101_01010101u,
                        BitOperations.RotateRight((nuint)0b01010101_11111111_01010101_01010101u, 0));
                    Assert.Equal((nuint)0b00000000_00000000_00000000_00000001u,
                        BitOperations.RotateRight((nuint)0b10000000_00000000_00000000_00000000u, -1));
                    Assert.Equal((nuint)0b00000000_00000000_00000001_00000000u,
                        BitOperations.RotateRight((nuint)0b00000000_00000000_00000000_01000000u, -2));
                    Assert.Equal((nuint)0b00000000_00000000_00000000_00000010u,
                        BitOperations.RotateRight((nuint)0b01000000_00000000_00000000_00000000u, -3));
                    Assert.Equal((nuint)0b01010101_11111111_01010101_01010101u,
                        BitOperations.RotateRight((nuint)0b01010101_11111111_01010101_01010101u,
                            int.MinValue)); // % 32 = 0
                }
            }
        }

        [Theory]
        [InlineData(0u, 0u)]
        [InlineData(1u, 1u)]
        [InlineData(2u, 2u)]
        [InlineData(0x0096u, 0x0100u)]
        [InlineData(0x05CDu, 0x0800u)]
        [InlineData(0x0932u, 0x1000u)]
        [InlineData(0x0004_C911u, 0x0008_0000u)]
        [InlineData(0x00E0_A2E2u, 0x0100_0000u)]
        [InlineData(0x0988_0713u, 0x1000_0000u)]
        [InlineData(0x30A4_9649u, 0x4000_0000u)]
        [InlineData(0x7FFF_FFFFu, 0x8000_0000u)]
        [InlineData(0x8000_0000u, 0x8000_0000u)]
        [InlineData(0x8000_0001u, 0ul)]
        [InlineData(0xFFFF_FFFFu, 0ul)]
        public static void BitOps_RoundUpToPow2_uint(uint value, uint expected)
        {
            Assert.Equal(expected, BitOperations.RoundUpToPowerOf2(value));
        }

        [Theory]
        [InlineData(0ul, 0ul)]
        [InlineData(1ul, 1ul)]
        [InlineData(2ul, 2ul)]
        [InlineData(0x0096ul, 0x0100ul)]
        [InlineData(0x05cdul, 0x0800ul)]
        [InlineData(0x0932ul, 0x1000ul)]
        [InlineData(0x0004_c911ul, 0x0008_0000ul)]
        [InlineData(0x00e0_a2b2ul, 0x0100_0000ul)]
        [InlineData(0x0988_0713ul, 0x1000_0000ul)]
        [InlineData(0x30a4_9649ul, 0x4000_0000ul)]
        [InlineData(0x7FFF_FFFFul, 0x8000_0000ul)]
        [InlineData(0x8000_0000ul, 0x8000_0000ul)]
        [InlineData(0x8000_0001ul, 0x1_0000_0000ul)]
        [InlineData(0xFFFF_FFFFul, 0x1_0000_0000ul)]
        [InlineData(0x0000_0003_343B_0D81ul, 0x0000_0004_0000_0000ul)]
        [InlineData(0x0000_0D87_5EE2_8F19ul, 0x0000_1000_0000_0000ul)]
        [InlineData(0x0006_2A08_4A7A_3A2Dul, 0x0008_0000_0000_0000ul)]
        [InlineData(0x0101_BF76_4398_F791ul, 0x0200_0000_0000_0000ul)]
        [InlineData(0x7FFF_FFFF_FFFF_FFFFul, 0x8000_0000_0000_0000ul)]
        [InlineData(0x8000_0000_0000_0000ul, 0x8000_0000_0000_0000ul)]
        [InlineData(0x8000_0000_0000_0001ul, 0ul)]
        [InlineData(0xFFFF_FFFF_FFFF_FFFFul, 0ul)]
        public static void BitOps_RoundUpToPow2_ulong(ulong value, ulong expected)
        {
            Assert.Equal(expected, BitOperations.RoundUpToPowerOf2(value));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
        [InlineData(0u, 0u)]
        [InlineData(1u, 1u)]
        [InlineData(2u, 2u)]
        [InlineData(0x0096u, 0x0100u)]
        [InlineData(0x05CDu, 0x0800u)]
        [InlineData(0x0932u, 0x1000u)]
        [InlineData(0x0004_C911u, 0x0008_0000u)]
        [InlineData(0x00E0_A2E2u, 0x0100_0000u)]
        [InlineData(0x0988_0713u, 0x1000_0000u)]
        [InlineData(0x30A4_9649u, 0x4000_0000u)]
        [InlineData(0x7FFF_FFFFu, 0x8000_0000u)]
        [InlineData(0x8000_0000u, 0x8000_0000u)]
        [InlineData(0x8000_0001u, 0ul)]
        [InlineData(0xFFFF_FFFFu, 0ul)]
        public static void BitOps_RoundUpToPow2_nuint_32(uint value, uint expected)
        {
            Assert.Equal(expected, BitOperations.RoundUpToPowerOf2((nuint) value));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [InlineData(0ul, 0ul)]
        [InlineData(1ul, 1ul)]
        [InlineData(2ul, 2ul)]
        [InlineData(0x0096ul, 0x0100ul)]
        [InlineData(0x05cdul, 0x0800ul)]
        [InlineData(0x0932ul, 0x1000ul)]
        [InlineData(0x0004_c911ul, 0x0008_0000ul)]
        [InlineData(0x00e0_a2b2ul, 0x0100_0000ul)]
        [InlineData(0x0988_0713ul, 0x1000_0000ul)]
        [InlineData(0x30a4_9649ul, 0x4000_0000ul)]
        [InlineData(0x7FFF_FFFFul, 0x8000_0000ul)]
        [InlineData(0x8000_0000ul, 0x8000_0000ul)]
        [InlineData(0x8000_0001ul, 0x1_0000_0000ul)]
        [InlineData(0xFFFF_FFFFul, 0x1_0000_0000ul)]
        [InlineData(0x0000_0003_343B_0D81ul, 0x0000_0004_0000_0000ul)]
        [InlineData(0x0000_0D87_5EE2_8F19ul, 0x0000_1000_0000_0000ul)]
        [InlineData(0x0006_2A08_4A7A_3A2Dul, 0x0008_0000_0000_0000ul)]
        [InlineData(0x0101_BF76_4398_F791ul, 0x0200_0000_0000_0000ul)]
        [InlineData(0x7FFF_FFFF_FFFF_FFFFul, 0x8000_0000_0000_0000ul)]
        [InlineData(0x8000_0000_0000_0000ul, 0x8000_0000_0000_0000ul)]
        [InlineData(0x8000_0000_0000_0001ul, 0ul)]
        [InlineData(0xFFFF_FFFF_FFFF_FFFFul, 0ul)]
        public static void BitOps_RoundUpToPow2_nuint_64(ulong value, ulong expected)
        {
            Assert.Equal(expected, BitOperations.RoundUpToPowerOf2((nuint) value));
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 120, 4215344322)]
        [InlineData(0, byte.MaxValue, 2910671697)]
        [InlineData(123, byte.MaxValue, 1164749927)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/76830", TestPlatforms.tvOS)]
        public static void BitOps_Crc32C_byte(uint crc, byte data, uint expected)
        {
            uint obtained = BitOperations.Crc32C(crc, data);
            Assert.Equal(expected, obtained);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 120, 575477567)]
        [InlineData(0, ushort.MaxValue, 245266386)]
        [InlineData(123, ushort.MaxValue, 406112372)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/76830", TestPlatforms.tvOS)]
        public static void BitOps_Crc32C_ushort(uint crc, ushort data, uint expected)
        {
            uint obtained = BitOperations.Crc32C(crc, data);
            Assert.Equal(expected, obtained);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 120, 1671666103)]
        [InlineData(0, uint.MaxValue, 3080238136)]
        [InlineData(123, uint.MaxValue, 3055133878)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/76830", TestPlatforms.tvOS)]
        public static void BitOps_Crc32C_uint(uint crc, uint data, uint expected)
        {
            uint obtained = BitOperations.Crc32C(crc, data);
            Assert.Equal(expected, obtained);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 120, 3511526341)]
        [InlineData(0, ulong.MaxValue, 3293575501)]
        [InlineData(123, ulong.MaxValue, 3460750817)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/76830", TestPlatforms.tvOS)]
        public static void BitOps_Crc32C_ulong(uint crc, ulong data, uint expected)
        {
            uint obtained = BitOperations.Crc32C(crc, data);
            Assert.Equal(expected, obtained);
        }
    }
}
