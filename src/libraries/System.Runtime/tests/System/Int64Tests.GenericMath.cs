// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/54910", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
    [RequiresPreviewFeaturesAttribute]
    public class Int64Tests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((long)0x0000000000000000, AdditiveIdentityHelper<long, long>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(unchecked((long)0x8000000000000000), MinMaxValueHelper<long>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, MinMaxValueHelper<long>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((long)0x0000000000000001, MultiplicativeIdentityHelper<long, long>.MultiplicativeIdentity);
        }

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), SignedNumberHelper<long>.NegativeOne);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((long)0x0000000000000001, AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), AdditionOperatorsHelper<long, long, long>.op_Addition((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), AdditionOperatorsHelper<long, long, long>.op_Addition(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000000, AdditionOperatorsHelper<long, long, long>.op_Addition(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x0000000000000000));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x0000000000000001));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.LeadingZeroCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.LeadingZeroCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.LeadingZeroCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.PopCount((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.PopCount((long)0x0000000000000001));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.PopCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.PopCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.PopCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.RotateLeft((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, BinaryIntegerHelper<long>.RotateLeft((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BinaryIntegerHelper<long>.RotateLeft((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, BinaryIntegerHelper<long>.RotateLeft(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateLeft(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.RotateRight((long)0x0000000000000000, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), BinaryIntegerHelper<long>.RotateRight((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xBFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateRight((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x4000000000000000, BinaryIntegerHelper<long>.RotateRight(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BinaryIntegerHelper<long>.RotateRight(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((long)0x0000000000000040, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x0000000000000001));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x000000000000003F, BinaryIntegerHelper<long>.TrailingZeroCount(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BinaryIntegerHelper<long>.TrailingZeroCount(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<long>.IsPow2((long)0x0000000000000000));
            Assert.True(BinaryNumberHelper<long>.IsPow2((long)0x0000000000000001));
            Assert.False(BinaryNumberHelper<long>.IsPow2((long)0x7FFFFFFFFFFFFFFF));
            Assert.False(BinaryNumberHelper<long>.IsPow2(unchecked((long)0x8000000000000000)));
            Assert.False(BinaryNumberHelper<long>.IsPow2(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((long)0x0000000000000000, BinaryNumberHelper<long>.Log2((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, BinaryNumberHelper<long>.Log2((long)0x0000000000000001));
            Assert.Equal((long)0x000000000000003E, BinaryNumberHelper<long>.Log2((long)0x7FFFFFFFFFFFFFFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<long>.Log2(unchecked((long)0x8000000000000000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<long>.Log2(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseAnd(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<long, long, long>.op_BitwiseOr(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((long)0x0000000000000001, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000001), BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<long, long, long>.op_ExclusiveOr(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x0000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000000), BitwiseOperatorsHelper<long, long, long>.op_OnesComplement((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, BitwiseOperatorsHelper<long, long, long>.op_OnesComplement(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, BitwiseOperatorsHelper<long, long, long>.op_OnesComplement(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThan((long)0x0000000000000000, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_LessThan((long)0x0000000000000001, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_LessThan((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThan(unchecked((long)0x8000000000000000), 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThan(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThanOrEqual((long)0x0000000000000000, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThanOrEqual((long)0x0000000000000001, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_LessThanOrEqual((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThanOrEqual(unchecked((long)0x8000000000000000), 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_LessThanOrEqual(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThan((long)0x0000000000000000, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThan((long)0x0000000000000001, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_GreaterThan((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThan(unchecked((long)0x8000000000000000), 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThan(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThanOrEqual((long)0x0000000000000000, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_GreaterThanOrEqual((long)0x0000000000000001, 1));
            Assert.True(ComparisonOperatorsHelper<long, long>.op_GreaterThanOrEqual((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThanOrEqual(unchecked((long)0x8000000000000000), 1));
            Assert.False(ComparisonOperatorsHelper<long, long>.op_GreaterThanOrEqual(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), DecrementOperatorsHelper<long>.op_Decrement((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000000, DecrementOperatorsHelper<long>.op_Decrement((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, DecrementOperatorsHelper<long>.op_Decrement((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, DecrementOperatorsHelper<long>.op_Decrement(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), DecrementOperatorsHelper<long>.op_Decrement(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x0000000000000001, 2));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, DivisionOperatorsHelper<long, long, long>.op_Division((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal(unchecked((long)0xC000000000000000), DivisionOperatorsHelper<long, long, long>.op_Division(unchecked((long)0x8000000000000000), 2));
            Assert.Equal((long)0x0000000000000000, DivisionOperatorsHelper<long, long, long>.op_Division(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<long, long>.op_Equality((long)0x0000000000000000, 1));
            Assert.True(EqualityOperatorsHelper<long, long>.op_Equality((long)0x0000000000000001, 1));
            Assert.False(EqualityOperatorsHelper<long, long>.op_Equality((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.False(EqualityOperatorsHelper<long, long>.op_Equality(unchecked((long)0x8000000000000000), 1));
            Assert.False(EqualityOperatorsHelper<long, long>.op_Equality(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<long, long>.op_Inequality((long)0x0000000000000000, 1));
            Assert.False(EqualityOperatorsHelper<long, long>.op_Inequality((long)0x0000000000000001, 1));
            Assert.True(EqualityOperatorsHelper<long, long>.op_Inequality((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.True(EqualityOperatorsHelper<long, long>.op_Inequality(unchecked((long)0x8000000000000000), 1));
            Assert.True(EqualityOperatorsHelper<long, long>.op_Inequality(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((long)0x0000000000000001, IncrementOperatorsHelper<long>.op_Increment((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000002, IncrementOperatorsHelper<long>.op_Increment((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000000), IncrementOperatorsHelper<long>.op_Increment((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000001), IncrementOperatorsHelper<long>.op_Increment(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000000, IncrementOperatorsHelper<long>.op_Increment(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((long)0x0000000000000000, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000001, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x0000000000000001, 2));
            Assert.Equal((long)0x0000000000000001, ModulusOperatorsHelper<long, long, long>.op_Modulus((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((long)0x0000000000000000, ModulusOperatorsHelper<long, long, long>.op_Modulus(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), ModulusOperatorsHelper<long, long, long>.op_Modulus(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((long)0x0000000000000000, MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x0000000000000000, 2));
            Assert.Equal((long)0x0000000000000002, MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x0000000000000001, 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<long, long, long>.op_Multiply((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((long)0x0000000000000000, MultiplyOperatorsHelper<long, long, long>.op_Multiply(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), MultiplyOperatorsHelper<long, long, long>.op_Multiply(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Abs((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Abs((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Abs((long)0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<long>.Abs(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Abs(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Clamp((long)0x0000000000000000, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Clamp((long)0x0000000000000001, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal((long)0x000000000000003F, NumberHelper<long>.Clamp((long)0x7FFFFFFFFFFFFFFF, unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFC0), NumberHelper<long>.Clamp(unchecked((long)0x8000000000000000), unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Clamp(unchecked((long)0xFFFFFFFFFFFFFFFF), unchecked((long)0xFFFFFFFFFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void CreateFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.Create<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberHelper<long>.Create<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberHelper<long>.Create<byte>(0xFF));
        }

        [Fact]
        public static void CreateFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.Create<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.Create<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.Create<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.Create<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberHelper<long>.Create<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.Create<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.Create<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Create<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.Create<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Create<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.Create<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.Create<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.Create<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.Create<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberHelper<long>.Create<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Create<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.Create<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.Create<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.Create<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.Create<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberHelper<long>.Create<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.Create<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Create<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<long>.Create<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<long>.Create<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Create<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<long>.Create<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<long>.Create<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Create<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Create<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.Create<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberHelper<long>.Create<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.Create<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.CreateSaturating<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberHelper<long>.CreateSaturating<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberHelper<long>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberHelper<long>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateSaturating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberHelper<long>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberHelper<long>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberHelper<long>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<byte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<byte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.CreateTruncating<byte>(0x7F));
            Assert.Equal((long)0x0000000000000080, NumberHelper<long>.CreateTruncating<byte>(0x80));
            Assert.Equal((long)0x00000000000000FF, NumberHelper<long>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<short>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<short>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), NumberHelper<long>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<int>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<int>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateTruncating<long>(unchecked(unchecked((long)0x8000000000000000))));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((long)0x000000000000007F, NumberHelper<long>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), NumberHelper<long>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((long)0x0000000000007FFF, NumberHelper<long>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((long)0x0000000000008000, NumberHelper<long>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((long)0x000000000000FFFF, NumberHelper<long>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((long)0x0000000080000000, NumberHelper<long>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((long)0x0000000000000000, NumberHelper<long>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((long)0x0000000000000001, NumberHelper<long>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((long)0x000000007FFFFFFF, NumberHelper<long>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((long)0x0000000080000000, NumberHelper<long>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((long)0x00000000FFFFFFFF, NumberHelper<long>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((long)0x0000000000000000, (long)0x0000000000000000), NumberHelper<long>.DivRem((long)0x0000000000000000, 2));
            Assert.Equal(((long)0x0000000000000000, (long)0x0000000000000001), NumberHelper<long>.DivRem((long)0x0000000000000001, 2));
            Assert.Equal(((long)0x3FFFFFFFFFFFFFFF, (long)0x0000000000000001), NumberHelper<long>.DivRem((long)0x7FFFFFFFFFFFFFFF, 2));
            Assert.Equal((unchecked((long)0xC000000000000000), (long)0x0000000000000000), NumberHelper<long>.DivRem(unchecked((long)0x8000000000000000), 2));
            Assert.Equal(((long)0x0000000000000000, unchecked((long)0xFFFFFFFFFFFFFFFF)), NumberHelper<long>.DivRem(unchecked((long)0xFFFFFFFFFFFFFFFF), 2));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, NumberHelper<long>.Max((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max(unchecked((long)0x8000000000000000), 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Max(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Min((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Min((long)0x0000000000000001, 1));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Min((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0x8000000000000000), NumberHelper<long>.Min(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Min(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal((long)0x0000000000000000, NumberHelper<long>.Sign((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Sign((long)0x0000000000000001));
            Assert.Equal((long)0x0000000000000001, NumberHelper<long>.Sign((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Sign(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), NumberHelper<long>.Sign(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<byte>(0x00, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<byte>(0x01, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((long)0x000000000000007F, result);

            Assert.True(NumberHelper<long>.TryCreate<byte>(0x80, out result));
            Assert.Equal((long)0x0000000000000080, result);

            Assert.True(NumberHelper<long>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((long)0x00000000000000FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((long)0x0000000000007FFF, result);

            Assert.True(NumberHelper<long>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((long)0x0000000000008000, result);

            Assert.True(NumberHelper<long>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((long)0x000000000000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<short>(0x0000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<short>(0x0001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((long)0x0000000000007FFF, result);

            Assert.True(NumberHelper<long>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFF8000), result);

            Assert.True(NumberHelper<long>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((long)0x000000007FFFFFFF, result);

            Assert.True(NumberHelper<long>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFF80000000), result);

            Assert.True(NumberHelper<long>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, result);

            Assert.True(NumberHelper<long>.TryCreate<long>(unchecked(unchecked((long)0x8000000000000000)), out result));
            Assert.Equal(unchecked((long)0x8000000000000000), result);

            Assert.True(NumberHelper<long>.TryCreate<long>(unchecked(unchecked((long)0xFFFFFFFFFFFFFFFF)), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            long result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((long)0x0000000000000000, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((long)0x0000000000000001, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal(unchecked((long)0x8000000000000000), result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
            }
            else
            {
                Assert.True(NumberHelper<long>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((long)0x0000000000000000, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((long)0x0000000000000001, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((long)0x000000007FFFFFFF, result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal(unchecked((long)0xFFFFFFFF80000000), result);

                Assert.True(NumberHelper<long>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((long)0x000000000000007F, result);

            Assert.True(NumberHelper<long>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFF80), result);

            Assert.True(NumberHelper<long>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((long)0x0000000000007FFF, result);

            Assert.True(NumberHelper<long>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((long)0x0000000000008000, result);

            Assert.True(NumberHelper<long>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((long)0x000000000000FFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((long)0x000000007FFFFFFF, result);

            Assert.True(NumberHelper<long>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((long)0x0000000080000000, result);

            Assert.True(NumberHelper<long>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((long)0x00000000FFFFFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            long result;

            Assert.True(NumberHelper<long>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.True(NumberHelper<long>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((long)0x0000000000000001, result);

            Assert.True(NumberHelper<long>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, result);

            Assert.False(NumberHelper<long>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((long)0x0000000000000000, result);

            Assert.False(NumberHelper<long>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((long)0x0000000000000000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            long result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((long)0x0000000000000000, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((long)0x0000000000000001, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((long)0x7FFFFFFFFFFFFFFF, result);

                Assert.False(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((long)0x0000000000000000, result);

                Assert.False(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((long)0x0000000000000000, result);
            }
            else
            {
                Assert.True(NumberHelper<long>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((long)0x0000000000000000, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((long)0x0000000000000001, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((long)0x000000007FFFFFFF, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((long)0x0000000080000000, result);

                Assert.True(NumberHelper<long>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((long)0x00000000FFFFFFFF, result);
            }
        }

        [Fact]

        public static void op_LeftShiftTest()
        {
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, long>.op_LeftShift((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000002, ShiftOperatorsHelper<long, long>.op_LeftShift((long)0x0000000000000001, 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<long, long>.op_LeftShift((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, long>.op_LeftShift(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), ShiftOperatorsHelper<long, long>.op_LeftShift(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, long>.op_RightShift((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, ShiftOperatorsHelper<long, long>.op_RightShift((long)0x0000000000000001, 1));
            Assert.Equal((long)0x3FFFFFFFFFFFFFFF, ShiftOperatorsHelper<long, long>.op_RightShift((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal(unchecked((long)0xC000000000000000), ShiftOperatorsHelper<long, long>.op_RightShift(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), ShiftOperatorsHelper<long, long>.op_RightShift(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x0000000000000000, 1));
            Assert.Equal((long)0x0000000000000000, SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x0000000000000001, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFE, SubtractionOperatorsHelper<long, long, long>.op_Subtraction((long)0x7FFFFFFFFFFFFFFF, 1));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, SubtractionOperatorsHelper<long, long, long>.op_Subtraction(unchecked((long)0x8000000000000000), 1));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFE), SubtractionOperatorsHelper<long, long, long>.op_Subtraction(unchecked((long)0xFFFFFFFFFFFFFFFF), 1));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((long)0x0000000000000000, UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x0000000000000000));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x0000000000000001));
            Assert.Equal(unchecked((long)0x8000000000000001), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation(unchecked((long)0x8000000000000000)));
            Assert.Equal((long)0x0000000000000001, UnaryNegationOperatorsHelper<long, long>.op_UnaryNegation(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((long)0x0000000000000000, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x0000000000000000));
            Assert.Equal((long)0x0000000000000001, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x0000000000000001));
            Assert.Equal((long)0x7FFFFFFFFFFFFFFF, UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus((long)0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((long)0x8000000000000000), UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((long)0xFFFFFFFFFFFFFFFF), UnaryPlusOperatorsHelper<long, long>.op_UnaryPlus(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Valid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParseableHelper<long>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParseableHelper<long>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberHelper<long>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<long>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<long>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParseableHelper<long>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<long>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<long>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Invalid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParseableHelper<long>.TryParse(value, provider, out result));
                Assert.Equal(default(long), result);
                Assert.Throws(exceptionType, () => ParseableHelper<long>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<long>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<long>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(long), result);
                Assert.Throws(exceptionType, () => NumberHelper<long>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParseableHelper<long>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<long>.TryParse(value, style, provider, out result));
            Assert.Equal(default(long), result);
            Assert.Throws(exceptionType, () => NumberHelper<long>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, long expected)
        {
            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParseableHelper<long>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberHelper<long>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<long>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Int64Tests.Parse_Invalid_TestData), MemberType = typeof(Int64Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            long result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParseableHelper<long>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(long), result);
            }

            Assert.Throws(exceptionType, () => NumberHelper<long>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<long>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(long), result);
        }
    }
}
