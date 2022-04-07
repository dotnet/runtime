// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Versioning;
using Xunit;

namespace System.Tests
{
    public class ByteTests_GenericMath
    {
        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((byte)0x00, AdditiveIdentityHelper<byte, byte>.AdditiveIdentity);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal((byte)0x00, MinMaxValueHelper<byte>.MinValue);
        }

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((byte)0xFF, MinMaxValueHelper<byte>.MaxValue);
        }

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((byte)0x01, MultiplicativeIdentityHelper<byte, byte>.MultiplicativeIdentity);
        }

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((byte)0x01, NumberBaseHelper<byte>.One);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((byte)0x00, NumberBaseHelper<byte>.Zero);
        }

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((byte)0x01, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x00, (byte)1));
            Assert.Equal((byte)0x02, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x01, (byte)1));
            Assert.Equal((byte)0x80, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0x80, (byte)1));
            Assert.Equal((byte)0x00, AdditionOperatorsHelper<byte, byte, byte>.op_Addition((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x00));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x01));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x7F));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0x80));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.LeadingZeroCount((byte)0xFF));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.PopCount((byte)0x00));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.PopCount((byte)0x01));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.PopCount((byte)0x7F));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.PopCount((byte)0x80));
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.PopCount((byte)0xFF));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.RotateLeft((byte)0x00, 1));
            Assert.Equal((byte)0x02, BinaryIntegerHelper<byte>.RotateLeft((byte)0x01, 1));
            Assert.Equal((byte)0xFE, BinaryIntegerHelper<byte>.RotateLeft((byte)0x7F, 1));
            Assert.Equal((byte)0x01, BinaryIntegerHelper<byte>.RotateLeft((byte)0x80, 1));
            Assert.Equal((byte)0xFF, BinaryIntegerHelper<byte>.RotateLeft((byte)0xFF, 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.RotateRight((byte)0x00, 1));
            Assert.Equal((byte)0x80, BinaryIntegerHelper<byte>.RotateRight((byte)0x01, 1));
            Assert.Equal((byte)0xBF, BinaryIntegerHelper<byte>.RotateRight((byte)0x7F, 1));
            Assert.Equal((byte)0x40, BinaryIntegerHelper<byte>.RotateRight((byte)0x80, 1));
            Assert.Equal((byte)0xFF, BinaryIntegerHelper<byte>.RotateRight((byte)0xFF, 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((byte)0x08, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x00));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x01));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x7F));
            Assert.Equal((byte)0x07, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0x80));
            Assert.Equal((byte)0x00, BinaryIntegerHelper<byte>.TrailingZeroCount((byte)0xFF));
        }

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0x00));
            Assert.True(BinaryNumberHelper<byte>.IsPow2((byte)0x01));
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0x7F));
            Assert.True(BinaryNumberHelper<byte>.IsPow2((byte)0x80));
            Assert.False(BinaryNumberHelper<byte>.IsPow2((byte)0xFF));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((byte)0x00, BinaryNumberHelper<byte>.Log2((byte)0x00));
            Assert.Equal((byte)0x00, BinaryNumberHelper<byte>.Log2((byte)0x01));
            Assert.Equal((byte)0x06, BinaryNumberHelper<byte>.Log2((byte)0x7F));
            Assert.Equal((byte)0x07, BinaryNumberHelper<byte>.Log2((byte)0x80));
            Assert.Equal((byte)0x07, BinaryNumberHelper<byte>.Log2((byte)0xFF));
        }

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseAnd((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, BitwiseOperatorsHelper<byte, byte, byte>.op_BitwiseOr((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((byte)0x01, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x00, (byte)1));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7E, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x81, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFE, BitwiseOperatorsHelper<byte, byte, byte>.op_ExclusiveOr((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal((byte)0xFF, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x00));
            Assert.Equal((byte)0xFE, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x01));
            Assert.Equal((byte)0x80, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x7F));
            Assert.Equal((byte)0x7F, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0x80));
            Assert.Equal((byte)0x00, BitwiseOperatorsHelper<byte, byte, byte>.op_OnesComplement((byte)0xFF));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_LessThan((byte)0x00, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThan((byte)0x01, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThan((byte)0x7F, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThan((byte)0x80, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThan((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_LessThanOrEqual((byte)0x00, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_LessThanOrEqual((byte)0x01, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThanOrEqual((byte)0x7F, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThanOrEqual((byte)0x80, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_LessThanOrEqual((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_GreaterThan((byte)0x00, (byte)1));
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_GreaterThan((byte)0x01, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThan((byte)0x7F, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThan((byte)0x80, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThan((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<byte, byte>.op_GreaterThanOrEqual((byte)0x00, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThanOrEqual((byte)0x01, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThanOrEqual((byte)0x7F, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThanOrEqual((byte)0x80, (byte)1));
            Assert.True(ComparisonOperatorsHelper<byte, byte>.op_GreaterThanOrEqual((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal((byte)0xFF, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x00));
            Assert.Equal((byte)0x00, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x01));
            Assert.Equal((byte)0x7E, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x7F));
            Assert.Equal((byte)0x7F, DecrementOperatorsHelper<byte>.op_Decrement((byte)0x80));
            Assert.Equal((byte)0xFE, DecrementOperatorsHelper<byte>.op_Decrement((byte)0xFF));
        }

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x00, (byte)2));
            Assert.Equal((byte)0x00, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x01, (byte)2));
            Assert.Equal((byte)0x3F, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x40, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0x80, (byte)2));
            Assert.Equal((byte)0x7F, DivisionOperatorsHelper<byte, byte, byte>.op_Division((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<byte, byte>.op_Equality((byte)0x00, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte>.op_Equality((byte)0x01, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte>.op_Equality((byte)0x7F, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte>.op_Equality((byte)0x80, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte>.op_Equality((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<byte, byte>.op_Inequality((byte)0x00, (byte)1));
            Assert.False(EqualityOperatorsHelper<byte, byte>.op_Inequality((byte)0x01, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte>.op_Inequality((byte)0x7F, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte>.op_Inequality((byte)0x80, (byte)1));
            Assert.True(EqualityOperatorsHelper<byte, byte>.op_Inequality((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((byte)0x01, IncrementOperatorsHelper<byte>.op_Increment((byte)0x00));
            Assert.Equal((byte)0x02, IncrementOperatorsHelper<byte>.op_Increment((byte)0x01));
            Assert.Equal((byte)0x80, IncrementOperatorsHelper<byte>.op_Increment((byte)0x7F));
            Assert.Equal((byte)0x81, IncrementOperatorsHelper<byte>.op_Increment((byte)0x80));
            Assert.Equal((byte)0x00, IncrementOperatorsHelper<byte>.op_Increment((byte)0xFF));
        }

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((byte)0x00, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x00, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x01, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x00, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0x80, (byte)2));
            Assert.Equal((byte)0x01, ModulusOperatorsHelper<byte, byte, byte>.op_Modulus((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((byte)0x00, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x00, (byte)2));
            Assert.Equal((byte)0x02, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x01, (byte)2));
            Assert.Equal((byte)0xFE, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x7F, (byte)2));
            Assert.Equal((byte)0x00, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0x80, (byte)2));
            Assert.Equal((byte)0xFE, MultiplyOperatorsHelper<byte, byte, byte>.op_Multiply((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.Abs((byte)0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Abs((byte)0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.Abs((byte)0x7F));
            Assert.Equal((byte)0x80, NumberHelper<byte>.Abs((byte)0x80));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.Abs((byte)0xFF));
        }

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((byte)0x01, NumberHelper<byte>.Clamp((byte)0x00, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Clamp((byte)0x01, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0x7F, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0x80, (byte)0x01, (byte)0x3F));
            Assert.Equal((byte)0x3F, NumberHelper<byte>.Clamp((byte)0xFF, (byte)0x01, (byte)0x3F));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<byte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateChecked<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberHelper<byte>.CreateChecked<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<char>((char)0x0001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<char>((char)0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<char>((char)0x8000));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<short>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<short>(0x0001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<short>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<int>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<int>(unchecked((int)0x80000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<nint>((nint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateChecked<sbyte>(0x7F));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<ushort>(0x0001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ushort>(0x7FFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ushort>(0x8000));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<uint>(0x00000001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberHelper<byte>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<byte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateSaturating<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberHelper<byte>.CreateSaturating<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<short>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<short>(0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<short>(0x7FFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<int>(0x00000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<int>(unchecked((int)0x80000000)));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<byte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<byte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateTruncating<byte>(0x7F));
            Assert.Equal((byte)0x80, NumberHelper<byte>.CreateTruncating<byte>(0x80));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<short>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<short>(0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<short>(0x7FFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<int>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<int>(0x00000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<int>(unchecked((int)0x80000000)));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<int>(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal((byte)0x80, NumberHelper<byte>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<uint>(0x80000000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((byte)0x01, NumberHelper<byte>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((byte)0x00, NumberHelper<byte>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal((byte)0xFF, NumberHelper<byte>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((byte)0x00, (byte)0x00), BinaryIntegerHelper<byte>.DivRem((byte)0x00, (byte)2));
            Assert.Equal(((byte)0x00, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0x01, (byte)2));
            Assert.Equal(((byte)0x3F, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0x7F, (byte)2));
            Assert.Equal(((byte)0x40, (byte)0x00), BinaryIntegerHelper<byte>.DivRem((byte)0x80, (byte)2));
            Assert.Equal(((byte)0x7F, (byte)0x01), BinaryIntegerHelper<byte>.DivRem((byte)0xFF, (byte)2));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((byte)0x01, NumberHelper<byte>.Max((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Max((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7F, NumberHelper<byte>.Max((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x80, NumberHelper<byte>.Max((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFF, NumberHelper<byte>.Max((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((byte)0x00, NumberHelper<byte>.Min((byte)0x00, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x01, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0x80, (byte)1));
            Assert.Equal((byte)0x01, NumberHelper<byte>.Min((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<byte>.Sign((byte)0x00));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x01));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x7F));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0x80));
            Assert.Equal(1, NumberHelper<byte>.Sign((byte)0xFF));
        }

        [Fact]
        public static void TryCreateFromByteTest()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<byte>(0x00, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<byte>(0x01, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(NumberHelper<byte>.TryCreate<byte>(0x7F, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.True(NumberHelper<byte>.TryCreate<byte>(0x80, out result));
            Assert.Equal((byte)0x80, result);

            Assert.True(NumberHelper<byte>.TryCreate<byte>(0xFF, out result));
            Assert.Equal((byte)0xFF, result);
        }

        [Fact]
        public static void TryCreateFromCharTest()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<char>((char)0x0000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<char>((char)0x0001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<char>((char)0x7FFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<char>((char)0x8000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<char>((char)0xFFFF, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromInt16Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<short>(0x0000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<short>(0x0001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<short>(0x7FFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<short>(unchecked((short)0x8000), out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<short>(unchecked((short)0xFFFF), out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromInt32Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<int>(0x00000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<int>(0x00000001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<int>(0x7FFFFFFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<int>(unchecked((int)0x80000000), out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<int>(unchecked((int)0xFFFFFFFF), out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromInt64Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<long>(0x0000000000000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<long>(0x0000000000000001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<long>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<long>(unchecked((long)0x8000000000000000), out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<long>(unchecked((long)0xFFFFFFFFFFFFFFFF), out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromIntPtrTest()
        {
            byte result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0x0000000000000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.True(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0x0000000000000001), out result));
                Assert.Equal((byte)0x01, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0x8000000000000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);
            }
            else
            {
                Assert.True(NumberHelper<byte>.TryCreate<nint>((nint)0x00000000, out result));
                Assert.Equal((byte)0x00, result);

                Assert.True(NumberHelper<byte>.TryCreate<nint>((nint)0x00000001, out result));
                Assert.Equal((byte)0x01, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>((nint)0x7FFFFFFF, out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0x80000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nint>(unchecked((nint)0xFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);
            }
        }

        [Fact]
        public static void TryCreateFromSByteTest()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<sbyte>(0x00, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<sbyte>(0x01, out result));
            Assert.Equal((byte)0x01, result);

            Assert.True(NumberHelper<byte>.TryCreate<sbyte>(0x7F, out result));
            Assert.Equal((byte)0x7F, result);

            Assert.False(NumberHelper<byte>.TryCreate<sbyte>(unchecked((sbyte)0x80), out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<sbyte>(unchecked((sbyte)0xFF), out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromUInt16Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<ushort>(0x0000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<ushort>(0x0001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<ushort>(0x7FFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<ushort>(0x8000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<ushort>(0xFFFF, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromUInt32Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<uint>(0x00000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<uint>(0x00000001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<uint>(0x7FFFFFFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<uint>(0x80000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<uint>(0xFFFFFFFF, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromUInt64Test()
        {
            byte result;

            Assert.True(NumberHelper<byte>.TryCreate<ulong>(0x0000000000000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.True(NumberHelper<byte>.TryCreate<ulong>(0x0000000000000001, out result));
            Assert.Equal((byte)0x01, result);

            Assert.False(NumberHelper<byte>.TryCreate<ulong>(0x7FFFFFFFFFFFFFFF, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<ulong>(0x8000000000000000, out result));
            Assert.Equal((byte)0x00, result);

            Assert.False(NumberHelper<byte>.TryCreate<ulong>(0xFFFFFFFFFFFFFFFF, out result));
            Assert.Equal((byte)0x00, result);
        }

        [Fact]
        public static void TryCreateFromUIntPtrTest()
        {
            byte result;

            if (Environment.Is64BitProcess)
            {
                Assert.True(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0x0000000000000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.True(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0x0000000000000001), out result));
                Assert.Equal((byte)0x01, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0x8000000000000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);
            }
            else
            {
                Assert.True(NumberHelper<byte>.TryCreate<nuint>((nuint)0x00000000, out result));
                Assert.Equal((byte)0x00, result);

                Assert.True(NumberHelper<byte>.TryCreate<nuint>((nuint)0x00000001, out result));
                Assert.Equal((byte)0x01, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>((nuint)0x7FFFFFFF, out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0x80000000), out result));
                Assert.Equal((byte)0x00, result);

                Assert.False(NumberHelper<byte>.TryCreate<nuint>(unchecked((nuint)0xFFFFFFFF), out result));
                Assert.Equal((byte)0x00, result);
            }
        }

        [Fact]

        public static void op_LeftShiftTest()
        {
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, byte>.op_LeftShift((byte)0x00, 1));
            Assert.Equal((byte)0x02, ShiftOperatorsHelper<byte, byte>.op_LeftShift((byte)0x01, 1));
            Assert.Equal((byte)0xFE, ShiftOperatorsHelper<byte, byte>.op_LeftShift((byte)0x7F, 1));
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, byte>.op_LeftShift((byte)0x80, 1));
            Assert.Equal((byte)0xFE, ShiftOperatorsHelper<byte, byte>.op_LeftShift((byte)0xFF, 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, byte>.op_RightShift((byte)0x00, 1));
            Assert.Equal((byte)0x00, ShiftOperatorsHelper<byte, byte>.op_RightShift((byte)0x01, 1));
            Assert.Equal((byte)0x3F, ShiftOperatorsHelper<byte, byte>.op_RightShift((byte)0x7F, 1));
            Assert.Equal((byte)0x40, ShiftOperatorsHelper<byte, byte>.op_RightShift((byte)0x80, 1));
            Assert.Equal((byte)0x7F, ShiftOperatorsHelper<byte, byte>.op_RightShift((byte)0xFF, 1));
        }

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal((byte)0xFF, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x00, (byte)1));
            Assert.Equal((byte)0x00, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x01, (byte)1));
            Assert.Equal((byte)0x7E, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x7F, (byte)1));
            Assert.Equal((byte)0x7F, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0x80, (byte)1));
            Assert.Equal((byte)0xFE, SubtractionOperatorsHelper<byte, byte, byte>.op_Subtraction((byte)0xFF, (byte)1));
        }

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((byte)0x00, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x00));
            Assert.Equal((byte)0xFF, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x01));
            Assert.Equal((byte)0x81, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x7F));
            Assert.Equal((byte)0x80, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0x80));
            Assert.Equal((byte)0x01, UnaryNegationOperatorsHelper<byte, byte>.op_UnaryNegation((byte)0xFF));
        }

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((byte)0x00, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x00));
            Assert.Equal((byte)0x01, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x01));
            Assert.Equal((byte)0x7F, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x7F));
            Assert.Equal((byte)0x80, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0x80));
            Assert.Equal((byte)0xFF, UnaryPlusOperatorsHelper<byte, byte>.op_UnaryPlus((byte)0xFF));
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Valid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<byte>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<byte>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberHelper<byte>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberHelper<byte>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberHelper<byte>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<byte>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberHelper<byte>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberHelper<byte>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Invalid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<byte>.TryParse(value, provider, out result));
                Assert.Equal(default(byte), result);
                Assert.Throws(exceptionType, () => ParsableHelper<byte>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberHelper<byte>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberHelper<byte>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(byte), result);
                Assert.Throws(exceptionType, () => NumberHelper<byte>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<byte>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberHelper<byte>.TryParse(value, style, provider, out result));
            Assert.Equal(default(byte), result);
            Assert.Throws(exceptionType, () => NumberHelper<byte>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(ByteTests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, byte expected)
        {
            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<byte>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberHelper<byte>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberHelper<byte>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(ByteTests.Parse_Invalid_TestData), MemberType = typeof(ByteTests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            byte result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<byte>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(byte), result);
            }

            Assert.Throws(exceptionType, () => NumberHelper<byte>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberHelper<byte>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(byte), result);
        }
    }
}
