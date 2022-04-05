// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    public class UInt16Tests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((ushort)0x0000, AdditiveIdentityHelper<ushort, ushort>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((ushort)0x0000, MinMaxValueHelper<ushort>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((ushort)0xFFFF, MinMaxValueHelper<ushort>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((ushort)0x0001, MultiplicativeIdentityHelper<ushort, ushort>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((ushort)0x0001, NumberBaseHelper<ushort>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((ushort)0x0000, NumberBaseHelper<ushort>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((ushort)0x0001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0002, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x8000, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0000, AdditionOperatorsHelper<ushort, ushort, ushort>.op_Addition((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x0000));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x0001));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.LeadingZeroCount((ushort)0xFFFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.PopCount((ushort)0x0000));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.PopCount((ushort)0x0001));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.PopCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.PopCount((ushort)0x8000));
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.PopCount((ushort)0xFFFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0002, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x0001, 1));
            Assert.Equal((ushort)0xFFFE, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x0001, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFF, BinaryIntegerHelper<ushort>.RotateLeft((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x0000, 1));
            Assert.Equal((ushort)0x8000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x0001, 1));
            Assert.Equal((ushort)0xBFFF, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x4000, BinaryIntegerHelper<ushort>.RotateRight((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFF, BinaryIntegerHelper<ushort>.RotateRight((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((ushort)0x0010, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x0000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x0001));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x7FFF));
            Assert.Equal((ushort)0x000F, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BinaryIntegerHelper<ushort>.TrailingZeroCount((ushort)0xFFFF));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0x0000));
            Assert.True(BinaryNumberHelper<ushort>.IsPow2((ushort)0x0001));
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0x7FFF));
            Assert.True(BinaryNumberHelper<ushort>.IsPow2((ushort)0x8000));
            Assert.False(BinaryNumberHelper<ushort>.IsPow2((ushort)0xFFFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((ushort)0x0000, BinaryNumberHelper<ushort>.Log2((ushort)0x0000));
            Assert.Equal((ushort)0x0000, BinaryNumberHelper<ushort>.Log2((ushort)0x0001));
            Assert.Equal((ushort)0x000E, BinaryNumberHelper<ushort>.Log2((ushort)0x7FFF));
            Assert.Equal((ushort)0x000F, BinaryNumberHelper<ushort>.Log2((ushort)0x8000));
            Assert.Equal((ushort)0x000F, BinaryNumberHelper<ushort>.Log2((ushort)0xFFFF));
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseAnd((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_BitwiseOr((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((ushort)0x0001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8001, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_ExclusiveOr((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((ushort)0xFFFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x0000));
            Assert.Equal((ushort)0xFFFE, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x0001));
            Assert.Equal((ushort)0x8000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x7FFF));
            Assert.Equal((ushort)0x7FFF, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0x8000));
            Assert.Equal((ushort)0x0000, BitwiseOperatorsHelper<ushort, ushort, ushort>.op_OnesComplement((ushort)0xFFFF));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_LessThan((ushort)0x0000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThan((ushort)0x0001, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThan((ushort)0x7FFF, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThan((ushort)0x8000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThan((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_LessThanOrEqual((ushort)0x0000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_LessThanOrEqual((ushort)0x0001, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThanOrEqual((ushort)0x7FFF, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThanOrEqual((ushort)0x8000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_LessThanOrEqual((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThan((ushort)0x0000, (ushort)1));
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThan((ushort)0x0001, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThan((ushort)0x7FFF, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThan((ushort)0x8000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThan((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThanOrEqual((ushort)0x0000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThanOrEqual((ushort)0x0001, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThanOrEqual((ushort)0x7FFF, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThanOrEqual((ushort)0x8000, (ushort)1));
            Assert.True(ComparisonOperatorsHelper<ushort, ushort>.op_GreaterThanOrEqual((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((ushort)0xFFFF, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x0000));
            Assert.Equal((ushort)0x0000, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x0001));
            Assert.Equal((ushort)0x7FFE, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x7FFF));
            Assert.Equal((ushort)0x7FFF, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0x8000));
            Assert.Equal((ushort)0xFFFE, DecrementOperatorsHelper<ushort>.op_Decrement((ushort)0xFFFF));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0x3FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x4000, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0x7FFF, DivisionOperatorsHelper<ushort, ushort, ushort>.op_Division((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<ushort, ushort>.op_Equality((ushort)0x0000, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort>.op_Equality((ushort)0x0001, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort>.op_Equality((ushort)0x7FFF, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort>.op_Equality((ushort)0x8000, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort>.op_Equality((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<ushort, ushort>.op_Inequality((ushort)0x0000, (ushort)1));
            Assert.False(EqualityOperatorsHelper<ushort, ushort>.op_Inequality((ushort)0x0001, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort>.op_Inequality((ushort)0x7FFF, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort>.op_Inequality((ushort)0x8000, (ushort)1));
            Assert.True(EqualityOperatorsHelper<ushort, ushort>.op_Inequality((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((ushort)0x0001, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x0000));
            Assert.Equal((ushort)0x0002, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x0001));
            Assert.Equal((ushort)0x8000, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x7FFF));
            Assert.Equal((ushort)0x8001, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0x8000));
            Assert.Equal((ushort)0x0000, IncrementOperatorsHelper<ushort>.op_Increment((ushort)0xFFFF));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((ushort)0x0000, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x0000, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0x0001, ModulusOperatorsHelper<ushort, ushort, ushort>.op_Modulus((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((ushort)0x0000, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x0000, (ushort)2));
            Assert.Equal((ushort)0x0002, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x0001, (ushort)2));
            Assert.Equal((ushort)0xFFFE, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x7FFF, (ushort)2));
            Assert.Equal((ushort)0x0000, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0x8000, (ushort)2));
            Assert.Equal((ushort)0xFFFE, MultiplyOperatorsHelper<ushort, ushort, ushort>.op_Multiply((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.Abs((ushort)0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Abs((ushort)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.Abs((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.Abs((ushort)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.Abs((ushort)0xFFFF));
        }

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Clamp((ushort)0x0000, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Clamp((ushort)0x0001, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0x7FFF, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0x8000, (ushort)0x0001, (ushort)0x003F));
            Assert.Equal((ushort)0x003F, NumberHelper<ushort>.Clamp((ushort)0xFFFF, (ushort)0x0001, (ushort)0x003F));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateChecked<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberHelper<ushort>.CreateChecked<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberHelper<ushort>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateChecked<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateChecked<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<ushort>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateSaturating<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberHelper<ushort>.CreateSaturating<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberHelper<ushort>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<int>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<byte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<byte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateTruncating<byte>(0x7F));
            Assert.Equal((ushort)0x0080, NumberHelper<ushort>.CreateTruncating<byte>(0x80));
            Assert.Equal((ushort)0x00FF, NumberHelper<ushort>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<short>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<short>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<int>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<int>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((ushort)0x007F, NumberHelper<ushort>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((ushort)0xFF80, NumberHelper<ushort>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((ushort)0x0001, NumberHelper<ushort>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((ushort)0x0000, NumberHelper<ushort>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((ushort)0x0000, (ushort)0x0000), BinaryIntegerHelper<ushort>.DivRem((ushort)0x0000, (ushort)2));
            Assert.Equal(((ushort)0x0000, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0x0001, (ushort)2));
            Assert.Equal(((ushort)0x3FFF, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0x7FFF, (ushort)2));
            Assert.Equal(((ushort)0x4000, (ushort)0x0000), BinaryIntegerHelper<ushort>.DivRem((ushort)0x8000, (ushort)2));
            Assert.Equal(((ushort)0x7FFF, (ushort)0x0001), BinaryIntegerHelper<ushort>.DivRem((ushort)0xFFFF, (ushort)2));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Max((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Max((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFF, NumberHelper<ushort>.Max((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x8000, NumberHelper<ushort>.Max((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFF, NumberHelper<ushort>.Max((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((ushort)0x0000, NumberHelper<ushort>.Min((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0x0001, NumberHelper<ushort>.Min((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<ushort>.Sign((ushort)0x0000));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x0001));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x7FFF));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0x8000));
            Assert.Equal(1, NumberHelper<ushort>.Sign((ushort)0xFFFF));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<byte>(0x00, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<byte>(0x01, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(NumberHelper<ushort>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.True(NumberHelper<ushort>.TryCreate<byte>(0x80, out result));
            Assert.Equal((ushort)0x0080, result);

            Assert.True(NumberHelper<ushort>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((ushort)0x00FF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(NumberHelper<ushort>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.True(NumberHelper<ushort>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((ushort)0x8000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((ushort)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<short>(0x0000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<short>(0x0001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(NumberHelper<ushort>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.False(NumberHelper<ushort>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(NumberHelper<ushort>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(NumberHelper<ushort>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            ushort result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.True(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((ushort)0x0001, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);
            }
            else
            {
                Assert.True(NumberHelper<ushort>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.True(NumberHelper<ushort>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((ushort)0x0001, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(NumberHelper<ushort>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((ushort)0x007F, result);

            Assert.False(NumberHelper<ushort>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.True(NumberHelper<ushort>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((ushort)0x7FFF, result);

            Assert.True(NumberHelper<ushort>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((ushort)0x8000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((ushort)0xFFFF, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(NumberHelper<ushort>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            ushort result;

            Assert.True(NumberHelper<ushort>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.True(NumberHelper<ushort>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((ushort)0x0001, result);

            Assert.False(NumberHelper<ushort>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((ushort)0x0000, result);

            Assert.False(NumberHelper<ushort>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((ushort)0x0000, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            ushort result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.True(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((ushort)0x0001, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);
            }
            else
            {
                Assert.True(NumberHelper<ushort>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.True(NumberHelper<ushort>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((ushort)0x0001, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((ushort)0x0000, result);

                Assert.False(NumberHelper<ushort>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((ushort)0x0000, result);
            }
        }

        [Fact]

        public static void op_LeftShiftTest()
        {
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, ushort>.op_LeftShift((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0002, ShiftOperatorsHelper<ushort, ushort>.op_LeftShift((ushort)0x0001, 1));
            Assert.Equal((ushort)0xFFFE, ShiftOperatorsHelper<ushort, ushort>.op_LeftShift((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, ushort>.op_LeftShift((ushort)0x8000, 1));
            Assert.Equal((ushort)0xFFFE, ShiftOperatorsHelper<ushort, ushort>.op_LeftShift((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, ushort>.op_RightShift((ushort)0x0000, 1));
            Assert.Equal((ushort)0x0000, ShiftOperatorsHelper<ushort, ushort>.op_RightShift((ushort)0x0001, 1));
            Assert.Equal((ushort)0x3FFF, ShiftOperatorsHelper<ushort, ushort>.op_RightShift((ushort)0x7FFF, 1));
            Assert.Equal((ushort)0x4000, ShiftOperatorsHelper<ushort, ushort>.op_RightShift((ushort)0x8000, 1));
            Assert.Equal((ushort)0x7FFF, ShiftOperatorsHelper<ushort, ushort>.op_RightShift((ushort)0xFFFF, 1));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((ushort)0xFFFF, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x0000, (ushort)1));
            Assert.Equal((ushort)0x0000, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x0001, (ushort)1));
            Assert.Equal((ushort)0x7FFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x7FFF, (ushort)1));
            Assert.Equal((ushort)0x7FFF, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0x8000, (ushort)1));
            Assert.Equal((ushort)0xFFFE, SubtractionOperatorsHelper<ushort, ushort, ushort>.op_Subtraction((ushort)0xFFFF, (ushort)1));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((ushort)0x0000, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x0000));
            Assert.Equal((ushort)0xFFFF, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x0001));
            Assert.Equal((ushort)0x8001, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0x8000));
            Assert.Equal((ushort)0x0001, UnaryNegationOperatorsHelper<ushort, ushort>.op_UnaryNegation((ushort)0xFFFF));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((ushort)0x0000, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x0000));
            Assert.Equal((ushort)0x0001, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x0001));
            Assert.Equal((ushort)0x7FFF, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x7FFF));
            Assert.Equal((ushort)0x8000, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0x8000));
            Assert.Equal((ushort)0xFFFF, UnaryPlusOperatorsHelper<ushort, ushort>.op_UnaryPlus((ushort)0xFFFF));
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Valid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<ushort>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<ushort>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberHelper<ushort>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<ushort>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<ushort>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<ushort>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<ushort>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<ushort>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Invalid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<ushort>.TryParse(value, provider, out result));
                Assert.Equal(default(ushort), result);
                Assert.Throws(exceptionType, () => ParsableHelper<ushort>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<ushort>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<ushort>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(ushort), result);
                Assert.Throws(exceptionType, () => NumberHelper<ushort>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<ushort>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<ushort>.TryParse(value, style, provider, out result));
            Assert.Equal(default(ushort), result);
            Assert.Throws(exceptionType, () => NumberHelper<ushort>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, ushort expected)
        {
            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<ushort>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberHelper<ushort>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<ushort>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(UInt16Tests.Parse_Invalid_TestData), MemberType = typeof(UInt16Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            ushort result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<ushort>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(ushort), result);
            }

            Assert.Throws(exceptionType, () => NumberHelper<ushort>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<ushort>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(ushort), result);
        }
    }
}
