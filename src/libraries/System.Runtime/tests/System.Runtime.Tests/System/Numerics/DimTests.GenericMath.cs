// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class DimTests_GenericMath
    {
        private const float MinNormalSingle = 1.17549435E-38f;

        private const float MaxSubnormalSingle = 1.17549421E-38f;

        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFF_FFFF), BinaryNumberHelper<BinaryIntegerWrapper<int>>.AllBitsSet);
            Assert.Equal((BinaryIntegerWrapper<int>)0, ~BinaryNumberHelper<BinaryIntegerWrapper<int>>.AllBitsSet);
        }

        [Fact]
        public static void AllBitsSetSingleTest()
        {
            Assert.Equal(0xFFFF_FFFF, BitConverter.SingleToUInt32Bits(BinaryNumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.AllBitsSet.Value));
            Assert.Equal(0U, ~BitConverter.SingleToUInt32Bits(BinaryNumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.AllBitsSet.Value));
        }

        [Fact]
        public static void AllBitsSetUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFF_FFFF, BinaryNumberHelper<BinaryIntegerWrapper<uint>>.AllBitsSet);
            Assert.Equal((BinaryIntegerWrapper<uint>)0U, ~BinaryNumberHelper<BinaryIntegerWrapper<uint>>.AllBitsSet);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemInt32Test()
        {
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x00000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x00000001, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x3FFFFFFF, (BinaryIntegerWrapper<int>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x7FFFFFFF, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)unchecked((int)0xC0000000), (BinaryIntegerWrapper<int>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem(unchecked((int)0x80000000), 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF)), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem(unchecked((int)0xFFFFFFFF), 2));
        }

        [Fact]
        public static void DivRemUInt32Test()
        {
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x00000000, (BinaryIntegerWrapper<uint>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x00000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x00000000, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x00000001, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x3FFFFFFF, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x7FFFFFFF, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x40000000, (BinaryIntegerWrapper<uint>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x80000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x7FFFFFFF, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0xFFFFFFFF, 2));
        }

        [Fact]
        public static void LeadingZeroCountInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000020, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x00000000));
            Assert.Equal((BinaryIntegerWrapper<int>)0x0000001F, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x00000001));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x7FFFFFFF));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount(unchecked((int)0x80000000)));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void LeadingZeroCountUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000020, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x00000000));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000001F, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x00000001));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x7FFFFFFF));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x80000000));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void RotateLeftInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000002, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFE), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateLeftUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000002, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFE, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void RotateRightInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xBFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x40000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateRightUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xBFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x40000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0xFFFFFFFF, 1));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x00000000, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x00000001, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)0x0000003F, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x7FFFFFFF, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFC0), NumberHelper<BinaryIntegerWrapper<int>>.Clamp(unchecked((int)0x80000000), unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.Clamp(unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void ClampSingleTest()
        {
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.NegativeInfinity, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.MinValue, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(-1.0f, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(-MinNormalSingle, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(-MaxSubnormalSingle, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(-float.Epsilon, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(-0.0f, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.NaN, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.NaN, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(0.0f, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.Epsilon, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(MaxSubnormalSingle, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(MinNormalSingle, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(1.0f, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)63.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.MaxValue, 1.0f, 63.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)63.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Clamp(float.PositiveInfinity, 1.0f, 63.0f));
        }

        [Fact]
        public static void ClampUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x00000000, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x00000001, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x7FFFFFFF, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x80000000, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0xFFFFFFFF, 0x0001, 0x003F));
        }

        [Fact]
        public static void MaxInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxSingleTest()
        {
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.NegativeInfinity, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.MinValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(-1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(-MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(-MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(-float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(-0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.NaN, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.NaN, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.MaxValue, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.MaxValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.PositiveInfinity, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Max(float.PositiveInfinity, 1.0f));
        }

        [Fact]
        public static void MaxUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MaxNumberInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxNumberSingleTest()
        {
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.NegativeInfinity, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.MinValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(-1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(-MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(-MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(-float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(-0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.NaN, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.MaxValue, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.MaxValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.PositiveInfinity, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MaxNumber(float.PositiveInfinity, 1.0f));
        }

        [Fact]
        public static void MaxNumberUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), NumberHelper<BinaryIntegerWrapper<int>>.Min(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.Min(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinSingleTest()
        {
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.NegativeInfinity, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.NegativeInfinity, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.MinValue, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.MinValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-1.0f), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(-1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-MinNormalSingle), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(-MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-MaxSubnormalSingle), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(-MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-float.Epsilon), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(-float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-0.0f), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(-0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.NaN, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.NaN, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)0.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.Epsilon, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)MaxSubnormalSingle, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)MinNormalSingle, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.MaxValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Min(float.PositiveInfinity, 1.0f));
        }

        [Fact]
        public static void MinUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinNumberInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), NumberHelper<BinaryIntegerWrapper<int>>.MinNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.MinNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinNumberSingleTest()
        {
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.NegativeInfinity, NumberHelper< BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.NegativeInfinity, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.MinValue, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.MinValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-1.0f), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(-1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-MinNormalSingle), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(-MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-MaxSubnormalSingle), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(-MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-float.Epsilon), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(-float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)(-0.0f), NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(-0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.NaN, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)0.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(0.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)float.Epsilon, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.Epsilon, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)MaxSubnormalSingle, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(MaxSubnormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)MinNormalSingle, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(MinNormalSingle, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(1.0f, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.MaxValue, 1.0f));
            AssertBitwiseEqual((BinaryFloatingPointIeee754Wrapper<float>)1.0f, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.MinNumber(float.PositiveInfinity, 1.0f));
        }

        [Fact]
        public static void MinNumberUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void SignInt32Test()
        {
            Assert.Equal(0, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x00000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x00000001));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x7FFFFFFF));
            Assert.Equal(-1, NumberHelper<BinaryIntegerWrapper<int>>.Sign(unchecked((int)0x80000000)));
            Assert.Equal(-1, NumberHelper<BinaryIntegerWrapper<int>>.Sign(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void SignSingleTest()
        {
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.NegativeInfinity));
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.MinValue));
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(-1.0f));
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(-MinNormalSingle));
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(-MaxSubnormalSingle));
            Assert.Equal(-1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(-float.Epsilon));

            Assert.Equal(0, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(-0.0f));
            Assert.Equal(0, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(0.0f));

            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.Epsilon));
            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(MaxSubnormalSingle));
            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(MinNormalSingle));
            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(1.0f));
            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.MaxValue));
            Assert.Equal(1, NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.PositiveInfinity));

            Assert.Throws<ArithmeticException>(() => NumberHelper<BinaryFloatingPointIeee754Wrapper<float>>.Sign(float.NaN));
        }

        [Fact]
        public static void SignUInt32Test()
        {
            Assert.Equal(0, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x00000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x00000001));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x7FFFFFFF));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x80000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0xFFFFFFFF));
        }

        public struct BinaryIntegerWrapper<T> : IBinaryInteger<BinaryIntegerWrapper<T>>
            where T : IBinaryInteger<T>
        {
            public T Value;

            public BinaryIntegerWrapper(T value)
            {
                Value = value;
            }

            public static implicit operator BinaryIntegerWrapper<T>(T value) => new BinaryIntegerWrapper<T>(value);

            public static implicit operator T(BinaryIntegerWrapper<T> value) => value.Value;

            // Required Generic Math Surface Area

            public static BinaryIntegerWrapper<T> AdditiveIdentity => T.AdditiveIdentity;

            public static BinaryIntegerWrapper<T> MultiplicativeIdentity => T.MultiplicativeIdentity;

            public static BinaryIntegerWrapper<T> One => T.One;

            public static int Radix => T.Radix;

            public static BinaryIntegerWrapper<T> Zero => T.Zero;

            public static BinaryIntegerWrapper<T> Abs(BinaryIntegerWrapper<T> value) => T.Abs(value);
            public static bool IsCanonical(BinaryIntegerWrapper<T> value) => T.IsCanonical(value);
            public static bool IsComplexNumber(BinaryIntegerWrapper<T> value) => T.IsComplexNumber(value);
            public static bool IsEvenInteger(BinaryIntegerWrapper<T> value) => T.IsEvenInteger(value);
            public static bool IsFinite(BinaryIntegerWrapper<T> value) => T.IsFinite(value);
            public static bool IsImaginaryNumber(BinaryIntegerWrapper<T> value) => T.IsImaginaryNumber(value);
            public static bool IsInfinity(BinaryIntegerWrapper<T> value) => T.IsInfinity(value);
            public static bool IsInteger(BinaryIntegerWrapper<T> value) => T.IsInteger(value);
            public static bool IsNaN(BinaryIntegerWrapper<T> value) => T.IsNaN(value);
            public static bool IsNegative(BinaryIntegerWrapper<T> value) => T.IsNegative(value);
            public static bool IsNegativeInfinity(BinaryIntegerWrapper<T> value) => T.IsNegativeInfinity(value);
            public static bool IsNormal(BinaryIntegerWrapper<T> value) => T.IsNormal(value);
            public static bool IsOddInteger(BinaryIntegerWrapper<T> value) => T.IsOddInteger(value);
            public static bool IsPositive(BinaryIntegerWrapper<T> value) => T.IsPositive(value);
            public static bool IsPositiveInfinity(BinaryIntegerWrapper<T> value) => T.IsPositiveInfinity(value);
            public static bool IsPow2(BinaryIntegerWrapper<T> value) => T.IsPow2(value);
            public static bool IsRealNumber(BinaryIntegerWrapper<T> value) => T.IsRealNumber(value);
            public static bool IsSubnormal(BinaryIntegerWrapper<T> value) => T.IsSubnormal(value);
            public static bool IsZero(BinaryIntegerWrapper<T> value) => T.IsZero(value);
            public static BinaryIntegerWrapper<T> Log2(BinaryIntegerWrapper<T> value) => T.Log2(value);
            public static BinaryIntegerWrapper<T> MaxMagnitude(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MaxMagnitude(x, y);
            public static BinaryIntegerWrapper<T> MaxMagnitudeNumber(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MaxMagnitudeNumber(x, y);
            public static BinaryIntegerWrapper<T> MinMagnitude(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MinMagnitude(x, y);
            public static BinaryIntegerWrapper<T> MinMagnitudeNumber(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MinMagnitudeNumber(x, y);
            public static BinaryIntegerWrapper<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerWrapper<T> Parse(string s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerWrapper<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerWrapper<T> Parse(string s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerWrapper<T> PopCount(BinaryIntegerWrapper<T> value) => T.PopCount(value);
            public static BinaryIntegerWrapper<T> TrailingZeroCount(BinaryIntegerWrapper<T> value) => T.TrailingZeroCount(value);
            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerWrapper<T> value)
            {
                var succeeded = T.TryReadBigEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }
            public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerWrapper<T> value)
            {
                var succeeded = T.TryReadLittleEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }
            public int CompareTo(object? obj)
            {
                if (obj is not BinaryIntegerWrapper<T> other)
                {
                    return (obj is null) ? 1 : throw new ArgumentException();
                }
                return CompareTo(other);
            }
            public int CompareTo(BinaryIntegerWrapper<T> other) => Value.CompareTo(other.Value);
            public override bool Equals([NotNullWhen(true)] object? obj) => (obj is BinaryIntegerWrapper<T> other) && Equals(other);
            public bool Equals(BinaryIntegerWrapper<T> other) => Value.Equals(other.Value);
            public int GetByteCount() => Value.GetByteCount();
            public override int GetHashCode() => Value.GetHashCode();
            public int GetShortestBitLength() => Value.GetShortestBitLength();
            public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);
            public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteBigEndian(destination, out bytesWritten);
            public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteLittleEndian(destination, out bytesWritten);

            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromChecked<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromChecked(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromSaturating<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromSaturating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromTruncating<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromTruncating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToChecked<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToChecked(value.Value, out result);
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToSaturating<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToSaturating(value.Value, out result);
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToTruncating<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToTruncating(value.Value, out result);

            public static BinaryIntegerWrapper<T> operator +(BinaryIntegerWrapper<T> value) => +value.Value;
            public static BinaryIntegerWrapper<T> operator +(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value + right.Value;
            public static BinaryIntegerWrapper<T> operator -(BinaryIntegerWrapper<T> value) => -value.Value;
            public static BinaryIntegerWrapper<T> operator -(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value - right.Value;
            public static BinaryIntegerWrapper<T> operator ~(BinaryIntegerWrapper<T> value) => ~value.Value;
            public static BinaryIntegerWrapper<T> operator ++(BinaryIntegerWrapper<T> value) => value.Value++;
            public static BinaryIntegerWrapper<T> operator --(BinaryIntegerWrapper<T> value) => value.Value--;
            public static BinaryIntegerWrapper<T> operator *(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value * right.Value;
            public static BinaryIntegerWrapper<T> operator /(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value / right.Value;
            public static BinaryIntegerWrapper<T> operator %(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value % right.Value;
            public static BinaryIntegerWrapper<T> operator &(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value & right.Value;
            public static BinaryIntegerWrapper<T> operator |(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value | right.Value;
            public static BinaryIntegerWrapper<T> operator ^(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value ^ right.Value;
            public static BinaryIntegerWrapper<T> operator <<(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value << shiftAmount;
            public static BinaryIntegerWrapper<T> operator >>(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value >> shiftAmount;
            public static bool operator ==(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value == right.Value;
            public static bool operator !=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value != right.Value;
            public static bool operator <(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value < right.Value;
            public static bool operator >(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value > right.Value;
            public static bool operator <=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value <= right.Value;
            public static bool operator >=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value >= right.Value;
            public static BinaryIntegerWrapper<T> operator >>>(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value >>> shiftAmount;
        }

        public struct BinaryFloatingPointIeee754Wrapper<T> : IBinaryFloatingPointIeee754<BinaryFloatingPointIeee754Wrapper<T>>
            where T : IBinaryFloatingPointIeee754<T>
        {
            public T Value;

            public BinaryFloatingPointIeee754Wrapper(T value)
            {
                Value = value;
            }

            public static implicit operator BinaryFloatingPointIeee754Wrapper<T>(T value) => new BinaryFloatingPointIeee754Wrapper<T>(value);

            public static implicit operator T(BinaryFloatingPointIeee754Wrapper<T> value) => value.Value;

            // Required Generic Math Surface Area

            public static BinaryFloatingPointIeee754Wrapper<T> AdditiveIdentity => T.AdditiveIdentity;

            public static BinaryFloatingPointIeee754Wrapper<T> E => T.E;

            public static BinaryFloatingPointIeee754Wrapper<T> Epsilon => T.Epsilon;

            public static BinaryFloatingPointIeee754Wrapper<T> MultiplicativeIdentity => T.MultiplicativeIdentity;

            public static BinaryFloatingPointIeee754Wrapper<T> NaN => T.NaN;

            public static BinaryFloatingPointIeee754Wrapper<T> NegativeInfinity => T.NegativeInfinity;

            public static BinaryFloatingPointIeee754Wrapper<T> NegativeOne => T.NegativeOne;

            public static BinaryFloatingPointIeee754Wrapper<T> NegativeZero => T.NegativeZero;

            public static BinaryFloatingPointIeee754Wrapper<T> One => T.One;

            public static BinaryFloatingPointIeee754Wrapper<T> Pi => T.Pi;

            public static BinaryFloatingPointIeee754Wrapper<T> PositiveInfinity => T.PositiveInfinity;

            public static int Radix => T.Radix;

            public static BinaryFloatingPointIeee754Wrapper<T> Tau => T.Tau;

            public static BinaryFloatingPointIeee754Wrapper<T> Zero => T.Zero;

            public static BinaryFloatingPointIeee754Wrapper<T> Abs(BinaryFloatingPointIeee754Wrapper<T> value) => T.Abs(value);
            public static BinaryFloatingPointIeee754Wrapper<T> Acos(BinaryFloatingPointIeee754Wrapper<T> x) => T.Acos(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Acosh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Acosh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> AcosPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.AcosPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Asin(BinaryFloatingPointIeee754Wrapper<T> x) => T.Asin(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Asinh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Asinh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> AsinPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.AsinPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Atan(BinaryFloatingPointIeee754Wrapper<T> x) => T.Atan(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Atan2(BinaryFloatingPointIeee754Wrapper<T> y, BinaryFloatingPointIeee754Wrapper<T> x) => T.Atan2(y, x);
            public static BinaryFloatingPointIeee754Wrapper<T> Atan2Pi(BinaryFloatingPointIeee754Wrapper<T> y, BinaryFloatingPointIeee754Wrapper<T> x) => T.Atan2Pi(y, x);
            public static BinaryFloatingPointIeee754Wrapper<T> Atanh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Atanh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> AtanPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.AtanPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> BitDecrement(BinaryFloatingPointIeee754Wrapper<T> x) => T.BitDecrement(x);
            public static BinaryFloatingPointIeee754Wrapper<T> BitIncrement(BinaryFloatingPointIeee754Wrapper<T> x) => T.BitIncrement(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Cbrt(BinaryFloatingPointIeee754Wrapper<T> x) => T.Cbrt(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Cos(BinaryFloatingPointIeee754Wrapper<T> x) => T.Cos(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Cosh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Cosh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> CosPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.CosPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Exp(BinaryFloatingPointIeee754Wrapper<T> x) => T.Exp(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Exp2(BinaryFloatingPointIeee754Wrapper<T> x) => T.Exp2(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Exp10(BinaryFloatingPointIeee754Wrapper<T> x) => T.Exp10(x);
            public static BinaryFloatingPointIeee754Wrapper<T> FusedMultiplyAdd(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right, BinaryFloatingPointIeee754Wrapper<T> addend) => T.FusedMultiplyAdd(left, right, addend);
            public static BinaryFloatingPointIeee754Wrapper<T> Hypot(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.Hypot(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> Ieee754Remainder(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => T.Ieee754Remainder(left, right);
            public static int ILogB(BinaryFloatingPointIeee754Wrapper<T> x) => T.ILogB(x);
            public static bool IsCanonical(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsCanonical(value);
            public static bool IsComplexNumber(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsComplexNumber(value);
            public static bool IsEvenInteger(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsEvenInteger(value);
            public static bool IsFinite(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsFinite(value);
            public static bool IsImaginaryNumber(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsImaginaryNumber(value);
            public static bool IsInfinity(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsInfinity(value);
            public static bool IsInteger(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsInteger(value);
            public static bool IsNaN(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsNaN(value);
            public static bool IsNegative(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsNegative(value);
            public static bool IsNegativeInfinity(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsNegativeInfinity(value);
            public static bool IsNormal(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsNormal(value);
            public static bool IsOddInteger(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsOddInteger(value);
            public static bool IsPositive(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsPositive(value);
            public static bool IsPositiveInfinity(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsPositiveInfinity(value);
            public static bool IsPow2(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsPow2(value);
            public static bool IsRealNumber(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsRealNumber(value);
            public static bool IsSubnormal(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsSubnormal(value);
            public static bool IsZero(BinaryFloatingPointIeee754Wrapper<T> value) => T.IsZero(value);
            public static BinaryFloatingPointIeee754Wrapper<T> Log(BinaryFloatingPointIeee754Wrapper<T> x) => T.Log(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Log(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> newBase) => T.Log(x, newBase);
            public static BinaryFloatingPointIeee754Wrapper<T> Log2(BinaryFloatingPointIeee754Wrapper<T> x) => BinaryNumberHelper<T>.Log2(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Log10(BinaryFloatingPointIeee754Wrapper<T> x) => T.Log10(x);
            public static BinaryFloatingPointIeee754Wrapper<T> MaxMagnitude(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.MaxMagnitude(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> MaxMagnitudeNumber(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.MaxMagnitudeNumber(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> MinMagnitude(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.MinMagnitude(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> MinMagnitudeNumber(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.MinMagnitudeNumber(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> Pow(BinaryFloatingPointIeee754Wrapper<T> x, BinaryFloatingPointIeee754Wrapper<T> y) => T.Pow(x, y);
            public static BinaryFloatingPointIeee754Wrapper<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryFloatingPointIeee754Wrapper<T> Parse(string s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryFloatingPointIeee754Wrapper<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryFloatingPointIeee754Wrapper<T> Parse(string s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryFloatingPointIeee754Wrapper<T> RootN(BinaryFloatingPointIeee754Wrapper<T> x, int n) => T.RootN(x, n);
            public static BinaryFloatingPointIeee754Wrapper<T> Round(BinaryFloatingPointIeee754Wrapper<T> x, int digits, MidpointRounding mode) => T.Round(x, digits, mode);
            public static BinaryFloatingPointIeee754Wrapper<T> ScaleB(BinaryFloatingPointIeee754Wrapper<T> x, int n) => T.ScaleB(x, n);
            public static BinaryFloatingPointIeee754Wrapper<T> Sin(BinaryFloatingPointIeee754Wrapper<T> x) => T.Sin(x);
            public static (BinaryFloatingPointIeee754Wrapper<T> Sin, BinaryFloatingPointIeee754Wrapper<T> Cos) SinCos(BinaryFloatingPointIeee754Wrapper<T> x) => T.SinCos(x);
            public static (BinaryFloatingPointIeee754Wrapper<T> SinPi, BinaryFloatingPointIeee754Wrapper<T> CosPi) SinCosPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.SinCosPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Sinh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Sinh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> SinPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.SinPi(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Sqrt(BinaryFloatingPointIeee754Wrapper<T> x) => T.Sqrt(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Tan(BinaryFloatingPointIeee754Wrapper<T> x) => T.Tan(x);
            public static BinaryFloatingPointIeee754Wrapper<T> Tanh(BinaryFloatingPointIeee754Wrapper<T> x) => T.Tanh(x);
            public static BinaryFloatingPointIeee754Wrapper<T> TanPi(BinaryFloatingPointIeee754Wrapper<T> x) => T.TanPi(x);
            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public int CompareTo(object? obj)
            {
                if (obj is not BinaryFloatingPointIeee754Wrapper<T> other)
                {
                    return (obj is null) ? 1 : throw new ArgumentException();
                }
                return CompareTo(other);
            }
            public int CompareTo(BinaryFloatingPointIeee754Wrapper<T> other) => Value.CompareTo(other.Value);
            public override bool Equals([NotNullWhen(true)] object? obj) => (obj is BinaryFloatingPointIeee754Wrapper<T> other) && Equals(other);
            public bool Equals(BinaryFloatingPointIeee754Wrapper<T> other) => Value.Equals(other.Value);
            public int GetExponentByteCount() => Value.GetExponentByteCount();
            public int GetExponentShortestBitLength() => Value.GetExponentShortestBitLength();
            public override int GetHashCode() => Value.GetHashCode();
            public int GetSignificandBitLength() => Value.GetSignificandBitLength();
            public int GetSignificandByteCount() => Value.GetSignificandByteCount();
            public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);
            public bool TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteExponentBigEndian(destination, out bytesWritten);
            public bool TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteExponentLittleEndian(destination, out bytesWritten);
            public bool TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteSignificandBigEndian(destination, out bytesWritten);
            public bool TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteSignificandLittleEndian(destination, out bytesWritten);

            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertFromChecked<TOther>(TOther value, out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                bool succeeded = T.TryConvertFromChecked(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertFromSaturating<TOther>(TOther value, out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                bool succeeded = T.TryConvertFromSaturating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertFromTruncating<TOther>(TOther value, out BinaryFloatingPointIeee754Wrapper<T> result)
            {
                bool succeeded = T.TryConvertFromTruncating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertToChecked<TOther>(BinaryFloatingPointIeee754Wrapper<T> value, out TOther result) => T.TryConvertToChecked(value.Value, out result);
            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertToSaturating<TOther>(BinaryFloatingPointIeee754Wrapper<T> value, out TOther result) => T.TryConvertToSaturating(value.Value, out result);
            static bool INumberBase<BinaryFloatingPointIeee754Wrapper<T>>.TryConvertToTruncating<TOther>(BinaryFloatingPointIeee754Wrapper<T> value, out TOther result) => T.TryConvertToTruncating(value.Value, out result);

            public static BinaryFloatingPointIeee754Wrapper<T> operator +(BinaryFloatingPointIeee754Wrapper<T> value) => +value.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator +(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value + right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator -(BinaryFloatingPointIeee754Wrapper<T> value) => -value.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator -(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value - right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator ~(BinaryFloatingPointIeee754Wrapper<T> value) => ~value.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator ++(BinaryFloatingPointIeee754Wrapper<T> value) => value.Value++;
            public static BinaryFloatingPointIeee754Wrapper<T> operator --(BinaryFloatingPointIeee754Wrapper<T> value) => value.Value--;
            public static BinaryFloatingPointIeee754Wrapper<T> operator *(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value * right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator /(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value / right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator %(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value % right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator &(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value & right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator |(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value | right.Value;
            public static BinaryFloatingPointIeee754Wrapper<T> operator ^(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value ^ right.Value;
            public static bool operator ==(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value == right.Value;
            public static bool operator !=(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value != right.Value;
            public static bool operator <(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value < right.Value;
            public static bool operator >(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value > right.Value;
            public static bool operator <=(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value <= right.Value;
            public static bool operator >=(BinaryFloatingPointIeee754Wrapper<T> left, BinaryFloatingPointIeee754Wrapper<T> right) => left.Value >= right.Value;
        }

        private static void AssertBitwiseEqual(BinaryFloatingPointIeee754Wrapper<float> expected, BinaryFloatingPointIeee754Wrapper<float> actual)
        {
            uint expectedBits = BitConverter.SingleToUInt32Bits(expected.Value);
            uint actualBits = BitConverter.SingleToUInt32Bits(actual.Value);

            if (expectedBits == actualBits)
            {
                return;
            }

            if (float.IsNaN(expected.Value) && float.IsNaN(actual.Value))
            {
                return;
            }

            throw Xunit.Sdk.EqualException.ForMismatchedValues(expected.Value, actual.Value);
        }
    }
}
