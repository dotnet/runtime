// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Int32Tests_GenericMath
    {
        //
        // IAdditionOperators
        //

        [Fact]
        public static void op_AdditionTest()
        {
            Assert.Equal((int)0x00000001, AdditionOperatorsHelper<int, int, int>.op_Addition((int)0x00000000, 1));
            Assert.Equal((int)0x00000002, AdditionOperatorsHelper<int, int, int>.op_Addition((int)0x00000001, 1));
            Assert.Equal(unchecked((int)0x80000000), AdditionOperatorsHelper<int, int, int>.op_Addition((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000001), AdditionOperatorsHelper<int, int, int>.op_Addition(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000000, AdditionOperatorsHelper<int, int, int>.op_Addition(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_CheckedAdditionTest()
        {
            Assert.Equal((int)0x00000001, AdditionOperatorsHelper<int, int, int>.op_CheckedAddition((int)0x00000000, 1));
            Assert.Equal((int)0x00000002, AdditionOperatorsHelper<int, int, int>.op_CheckedAddition((int)0x00000001, 1));
            Assert.Equal(unchecked((int)0x80000001), AdditionOperatorsHelper<int, int, int>.op_CheckedAddition(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000000, AdditionOperatorsHelper<int, int, int>.op_CheckedAddition(unchecked((int)0xFFFFFFFF), 1));

            Assert.Throws<OverflowException>(() => AdditionOperatorsHelper<int, int, int>.op_CheckedAddition((int)0x7FFFFFFF, 1));
        }

        //
        // IAdditiveIdentity
        //

        [Fact]
        public static void AdditiveIdentityTest()
        {
            Assert.Equal((int)0x00000000, AdditiveIdentityHelper<int, int>.AdditiveIdentity);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemTest()
        {
            Assert.Equal(((int)0x00000000, (int)0x00000000), BinaryIntegerHelper<int>.DivRem((int)0x00000000, 2));
            Assert.Equal(((int)0x00000000, (int)0x00000001), BinaryIntegerHelper<int>.DivRem((int)0x00000001, 2));
            Assert.Equal(((int)0x3FFFFFFF, (int)0x00000001), BinaryIntegerHelper<int>.DivRem((int)0x7FFFFFFF, 2));
            Assert.Equal((unchecked((int)0xC0000000), (int)0x00000000), BinaryIntegerHelper<int>.DivRem(unchecked((int)0x80000000), 2));
            Assert.Equal(((int)0x00000000, unchecked((int)0xFFFFFFFF)), BinaryIntegerHelper<int>.DivRem(unchecked((int)0xFFFFFFFF), 2));
        }

        [Fact]
        public static void LeadingZeroCountTest()
        {
            Assert.Equal((int)0x00000020, BinaryIntegerHelper<int>.LeadingZeroCount((int)0x00000000));
            Assert.Equal((int)0x0000001F, BinaryIntegerHelper<int>.LeadingZeroCount((int)0x00000001));
            Assert.Equal((int)0x00000001, BinaryIntegerHelper<int>.LeadingZeroCount((int)0x7FFFFFFF));
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.LeadingZeroCount(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.LeadingZeroCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void PopCountTest()
        {
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.PopCount((int)0x00000000));
            Assert.Equal((int)0x00000001, BinaryIntegerHelper<int>.PopCount((int)0x00000001));
            Assert.Equal((int)0x0000001F, BinaryIntegerHelper<int>.PopCount((int)0x7FFFFFFF));
            Assert.Equal((int)0x00000001, BinaryIntegerHelper<int>.PopCount(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000020, BinaryIntegerHelper<int>.PopCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void RotateLeftTest()
        {
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.RotateLeft((int)0x00000000, 1));
            Assert.Equal((int)0x00000002, BinaryIntegerHelper<int>.RotateLeft((int)0x00000001, 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), BinaryIntegerHelper<int>.RotateLeft((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000001, BinaryIntegerHelper<int>.RotateLeft(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<int>.RotateLeft(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateRightTest()
        {
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.RotateRight((int)0x00000000, 1));
            Assert.Equal(unchecked((int)0x80000000), BinaryIntegerHelper<int>.RotateRight((int)0x00000001, 1));
            Assert.Equal(unchecked((int)0xBFFFFFFF), BinaryIntegerHelper<int>.RotateRight((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x40000000, BinaryIntegerHelper<int>.RotateRight(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<int>.RotateRight(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void TrailingZeroCountTest()
        {
            Assert.Equal((int)0x00000020, BinaryIntegerHelper<int>.TrailingZeroCount((int)0x00000000));
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.TrailingZeroCount((int)0x00000001));
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.TrailingZeroCount((int)0x7FFFFFFF));
            Assert.Equal((int)0x0000001F, BinaryIntegerHelper<int>.TrailingZeroCount(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000000, BinaryIntegerHelper<int>.TrailingZeroCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void GetByteCountTest()
        {
            Assert.Equal(4, BinaryIntegerHelper<int>.GetByteCount((int)0x00000000));
            Assert.Equal(4, BinaryIntegerHelper<int>.GetByteCount((int)0x00000001));
            Assert.Equal(4, BinaryIntegerHelper<int>.GetByteCount((int)0x7FFFFFFF));
            Assert.Equal(4, BinaryIntegerHelper<int>.GetByteCount(unchecked((int)0x80000000)));
            Assert.Equal(4, BinaryIntegerHelper<int>.GetByteCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void GetShortestBitLengthTest()
        {
            Assert.Equal(0x00, BinaryIntegerHelper<int>.GetShortestBitLength((int)0x00000000));
            Assert.Equal(0x01, BinaryIntegerHelper<int>.GetShortestBitLength((int)0x00000001));
            Assert.Equal(0x1F, BinaryIntegerHelper<int>.GetShortestBitLength((int)0x7FFFFFFF));
            Assert.Equal(0x20, BinaryIntegerHelper<int>.GetShortestBitLength(unchecked((int)0x80000000)));
            Assert.Equal(0x01, BinaryIntegerHelper<int>.GetShortestBitLength(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void TryWriteBigEndianTest()
        {
            Span<byte> destination = stackalloc byte[4];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<int>.TryWriteBigEndian((int)0x00000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteBigEndian((int)0x00000001, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteBigEndian((int)0x7FFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteBigEndian(unchecked((int)0x80000000), destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteBigEndian(unchecked((int)0xFFFFFFFF), destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<int>.TryWriteBigEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        [Fact]
        public static void TryWriteLittleEndianTest()
        {
            Span<byte> destination = stackalloc byte[4];
            int bytesWritten = 0;

            Assert.True(BinaryIntegerHelper<int>.TryWriteLittleEndian((int)0x00000000, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteLittleEndian((int)0x00000001, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x01, 0x00, 0x00, 0x00 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteLittleEndian((int)0x7FFFFFFF, destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteLittleEndian(unchecked((int)0x80000000), destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x80 }, destination.ToArray());

            Assert.True(BinaryIntegerHelper<int>.TryWriteLittleEndian(unchecked((int)0xFFFFFFFF), destination, out bytesWritten));
            Assert.Equal(4, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());

            Assert.False(BinaryIntegerHelper<int>.TryWriteLittleEndian(default, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, destination.ToArray());
        }

        //
        // IBinaryNumber
        //

        [Fact]
        public static void IsPow2Test()
        {
            Assert.False(BinaryNumberHelper<int>.IsPow2((int)0x00000000));
            Assert.True(BinaryNumberHelper<int>.IsPow2((int)0x00000001));
            Assert.False(BinaryNumberHelper<int>.IsPow2((int)0x7FFFFFFF));
            Assert.False(BinaryNumberHelper<int>.IsPow2(unchecked((int)0x80000000)));
            Assert.False(BinaryNumberHelper<int>.IsPow2(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void Log2Test()
        {
            Assert.Equal((int)0x00000000, BinaryNumberHelper<int>.Log2((int)0x00000000));
            Assert.Equal((int)0x00000000, BinaryNumberHelper<int>.Log2((int)0x00000001));
            Assert.Equal((int)0x0000001E, BinaryNumberHelper<int>.Log2((int)0x7FFFFFFF));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<int>.Log2(unchecked((int)0x80000000)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryNumberHelper<int>.Log2(unchecked((int)0xFFFFFFFF)));
        }

        //
        // IBitwiseOperators
        //

        [Fact]
        public static void op_BitwiseAndTest()
        {
            Assert.Equal((int)0x00000000, BitwiseOperatorsHelper<int, int, int>.op_BitwiseAnd((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_BitwiseAnd((int)0x00000001, 1));
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_BitwiseAnd((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000000, BitwiseOperatorsHelper<int, int, int>.op_BitwiseAnd(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_BitwiseAnd(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_BitwiseOrTest()
        {
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_BitwiseOr((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_BitwiseOr((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFF, BitwiseOperatorsHelper<int, int, int>.op_BitwiseOr((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000001), BitwiseOperatorsHelper<int, int, int>.op_BitwiseOr(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), BitwiseOperatorsHelper<int, int, int>.op_BitwiseOr(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_ExclusiveOrTest()
        {
            Assert.Equal((int)0x00000001, BitwiseOperatorsHelper<int, int, int>.op_ExclusiveOr((int)0x00000000, 1));
            Assert.Equal((int)0x00000000, BitwiseOperatorsHelper<int, int, int>.op_ExclusiveOr((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFE, BitwiseOperatorsHelper<int, int, int>.op_ExclusiveOr((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000001), BitwiseOperatorsHelper<int, int, int>.op_ExclusiveOr(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), BitwiseOperatorsHelper<int, int, int>.op_ExclusiveOr(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_OnesComplementTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), BitwiseOperatorsHelper<int, int, int>.op_OnesComplement((int)0x00000000));
            Assert.Equal(unchecked((int)0xFFFFFFFE), BitwiseOperatorsHelper<int, int, int>.op_OnesComplement((int)0x00000001));
            Assert.Equal(unchecked((int)0x80000000), BitwiseOperatorsHelper<int, int, int>.op_OnesComplement((int)0x7FFFFFFF));
            Assert.Equal((int)0x7FFFFFFF, BitwiseOperatorsHelper<int, int, int>.op_OnesComplement(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000000, BitwiseOperatorsHelper<int, int, int>.op_OnesComplement(unchecked((int)0xFFFFFFFF)));
        }

        //
        // IComparisonOperators
        //

        [Fact]
        public static void op_GreaterThanTest()
        {
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThan((int)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThan((int)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_GreaterThan((int)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThan(unchecked((int)0x80000000), 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThan(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_GreaterThanOrEqualTest()
        {
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThanOrEqual((int)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_GreaterThanOrEqual((int)0x00000001, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_GreaterThanOrEqual((int)0x7FFFFFFF, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThanOrEqual(unchecked((int)0x80000000), 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_GreaterThanOrEqual(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_LessThanTest()
        {
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThan((int)0x00000000, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_LessThan((int)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_LessThan((int)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThan(unchecked((int)0x80000000), 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThan(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_LessThanOrEqualTest()
        {
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThanOrEqual((int)0x00000000, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThanOrEqual((int)0x00000001, 1));
            Assert.False(ComparisonOperatorsHelper<int, int>.op_LessThanOrEqual((int)0x7FFFFFFF, 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThanOrEqual(unchecked((int)0x80000000), 1));
            Assert.True(ComparisonOperatorsHelper<int, int>.op_LessThanOrEqual(unchecked((int)0xFFFFFFFF), 1));
        }

        //
        // IDecrementOperators
        //

        [Fact]
        public static void op_DecrementTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), DecrementOperatorsHelper<int>.op_Decrement((int)0x00000000));
            Assert.Equal((int)0x00000000, DecrementOperatorsHelper<int>.op_Decrement((int)0x00000001));
            Assert.Equal((int)0x7FFFFFFE, DecrementOperatorsHelper<int>.op_Decrement((int)0x7FFFFFFF));
            Assert.Equal((int)0x7FFFFFFF, DecrementOperatorsHelper<int>.op_Decrement(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((int)0xFFFFFFFE), DecrementOperatorsHelper<int>.op_Decrement(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedDecrementTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), DecrementOperatorsHelper<int>.op_CheckedDecrement((int)0x00000000));
            Assert.Equal((int)0x00000000, DecrementOperatorsHelper<int>.op_CheckedDecrement((int)0x00000001));
            Assert.Equal((int)0x7FFFFFFE, DecrementOperatorsHelper<int>.op_CheckedDecrement((int)0x7FFFFFFF));
            Assert.Equal(unchecked((int)0xFFFFFFFE), DecrementOperatorsHelper<int>.op_CheckedDecrement(unchecked((int)0xFFFFFFFF)));

            Assert.Throws<OverflowException>(() => DecrementOperatorsHelper<int>.op_CheckedDecrement(unchecked((int)0x80000000)));
        }

        //
        // IDivisionOperators
        //

        [Fact]
        public static void op_DivisionTest()
        {
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_Division((int)0x00000000, 2));
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_Division((int)0x00000001, 2));
            Assert.Equal((int)0x3FFFFFFF, DivisionOperatorsHelper<int, int, int>.op_Division((int)0x7FFFFFFF, 2));
            Assert.Equal(unchecked((int)0xC0000000), DivisionOperatorsHelper<int, int, int>.op_Division(unchecked((int)0x80000000), 2));
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_Division(unchecked((int)0xFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<int, int, int>.op_Division((int)0x00000001, 0));
        }

        [Fact]
        public static void op_CheckedDivisionTest()
        {
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_CheckedDivision((int)0x00000000, 2));
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_CheckedDivision((int)0x00000001, 2));
            Assert.Equal((int)0x3FFFFFFF, DivisionOperatorsHelper<int, int, int>.op_CheckedDivision((int)0x7FFFFFFF, 2));
            Assert.Equal(unchecked((int)0xC0000000), DivisionOperatorsHelper<int, int, int>.op_CheckedDivision(unchecked((int)0x80000000), 2));
            Assert.Equal((int)0x00000000, DivisionOperatorsHelper<int, int, int>.op_CheckedDivision(unchecked((int)0xFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => DivisionOperatorsHelper<int, int, int>.op_CheckedDivision((int)0x00000001, 0));
        }

        //
        // IEqualityOperators
        //

        [Fact]
        public static void op_EqualityTest()
        {
            Assert.False(EqualityOperatorsHelper<int, int>.op_Equality((int)0x00000000, 1));
            Assert.True(EqualityOperatorsHelper<int, int>.op_Equality((int)0x00000001, 1));
            Assert.False(EqualityOperatorsHelper<int, int>.op_Equality((int)0x7FFFFFFF, 1));
            Assert.False(EqualityOperatorsHelper<int, int>.op_Equality(unchecked((int)0x80000000), 1));
            Assert.False(EqualityOperatorsHelper<int, int>.op_Equality(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_InequalityTest()
        {
            Assert.True(EqualityOperatorsHelper<int, int>.op_Inequality((int)0x00000000, 1));
            Assert.False(EqualityOperatorsHelper<int, int>.op_Inequality((int)0x00000001, 1));
            Assert.True(EqualityOperatorsHelper<int, int>.op_Inequality((int)0x7FFFFFFF, 1));
            Assert.True(EqualityOperatorsHelper<int, int>.op_Inequality(unchecked((int)0x80000000), 1));
            Assert.True(EqualityOperatorsHelper<int, int>.op_Inequality(unchecked((int)0xFFFFFFFF), 1));
        }

        //
        // IIncrementOperators
        //

        [Fact]
        public static void op_IncrementTest()
        {
            Assert.Equal((int)0x00000001, IncrementOperatorsHelper<int>.op_Increment((int)0x00000000));
            Assert.Equal((int)0x00000002, IncrementOperatorsHelper<int>.op_Increment((int)0x00000001));
            Assert.Equal(unchecked((int)0x80000000), IncrementOperatorsHelper<int>.op_Increment((int)0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000001), IncrementOperatorsHelper<int>.op_Increment(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000000, IncrementOperatorsHelper<int>.op_Increment(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedIncrementTest()
        {
            Assert.Equal((int)0x00000001, IncrementOperatorsHelper<int>.op_CheckedIncrement((int)0x00000000));
            Assert.Equal((int)0x00000002, IncrementOperatorsHelper<int>.op_CheckedIncrement((int)0x00000001));
            Assert.Equal(unchecked((int)0x80000001), IncrementOperatorsHelper<int>.op_CheckedIncrement(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000000, IncrementOperatorsHelper<int>.op_CheckedIncrement(unchecked((int)0xFFFFFFFF)));

            Assert.Throws<OverflowException>(() => IncrementOperatorsHelper<int>.op_CheckedIncrement((int)0x7FFFFFFF));
        }

        //
        // IMinMaxValue
        //

        [Fact]
        public static void MaxValueTest()
        {
            Assert.Equal((int)0x7FFFFFFF, MinMaxValueHelper<int>.MaxValue);
        }

        [Fact]
        public static void MinValueTest()
        {
            Assert.Equal(unchecked((int)0x80000000), MinMaxValueHelper<int>.MinValue);
        }

        //
        // IModulusOperators
        //

        [Fact]
        public static void op_ModulusTest()
        {
            Assert.Equal((int)0x00000000, ModulusOperatorsHelper<int, int, int>.op_Modulus((int)0x00000000, 2));
            Assert.Equal((int)0x00000001, ModulusOperatorsHelper<int, int, int>.op_Modulus((int)0x00000001, 2));
            Assert.Equal((int)0x00000001, ModulusOperatorsHelper<int, int, int>.op_Modulus((int)0x7FFFFFFF, 2));
            Assert.Equal((int)0x00000000, ModulusOperatorsHelper<int, int, int>.op_Modulus(unchecked((int)0x80000000), 2));
            Assert.Equal(unchecked((int)0xFFFFFFFF), ModulusOperatorsHelper<int, int, int>.op_Modulus(unchecked((int)0xFFFFFFFF), 2));

            Assert.Throws<DivideByZeroException>(() => ModulusOperatorsHelper<int, int, int>.op_Modulus((int)0x00000001, 0));
        }

        //
        // IMultiplicativeIdentity
        //

        [Fact]
        public static void MultiplicativeIdentityTest()
        {
            Assert.Equal((int)0x00000001, MultiplicativeIdentityHelper<int, int>.MultiplicativeIdentity);
        }

        //
        // IMultiplyOperators
        //

        [Fact]
        public static void op_MultiplyTest()
        {
            Assert.Equal((int)0x00000000, MultiplyOperatorsHelper<int, int, int>.op_Multiply((int)0x00000000, 2));
            Assert.Equal((int)0x00000002, MultiplyOperatorsHelper<int, int, int>.op_Multiply((int)0x00000001, 2));
            Assert.Equal(unchecked((int)0xFFFFFFFE), MultiplyOperatorsHelper<int, int, int>.op_Multiply((int)0x7FFFFFFF, 2));
            Assert.Equal((int)0x00000000, MultiplyOperatorsHelper<int, int, int>.op_Multiply(unchecked((int)0x80000000), 2));
            Assert.Equal(unchecked((int)0xFFFFFFFE), MultiplyOperatorsHelper<int, int, int>.op_Multiply(unchecked((int)0xFFFFFFFF), 2));
        }

        [Fact]
        public static void op_CheckedMultiplyTest()
        {
            Assert.Equal((int)0x00000000, MultiplyOperatorsHelper<int, int, int>.op_CheckedMultiply((int)0x00000000, 2));
            Assert.Equal((int)0x00000002, MultiplyOperatorsHelper<int, int, int>.op_CheckedMultiply((int)0x00000001, 2));
            Assert.Equal(unchecked((int)0xFFFFFFFE), MultiplyOperatorsHelper<int, int, int>.op_CheckedMultiply(unchecked((int)0xFFFFFFFF), 2));

            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<int, int, int>.op_CheckedMultiply((int)0x7FFFFFFF, 2));
            Assert.Throws<OverflowException>(() => MultiplyOperatorsHelper<int, int, int>.op_CheckedMultiply(unchecked((int)0x80000000), 2));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampTest()
        {
            Assert.Equal((int)0x00000000, NumberHelper<int>.Clamp((int)0x00000000, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Clamp((int)0x00000001, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((int)0x0000003F, NumberHelper<int>.Clamp((int)0x7FFFFFFF, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((int)0xFFFFFFC0), NumberHelper<int>.Clamp(unchecked((int)0x80000000), unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberHelper<int>.Clamp(unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void MaxTest()
        {
            Assert.Equal((int)0x00000001, NumberHelper<int>.Max((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Max((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFF, NumberHelper<int>.Max((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Max(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Max(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxNumberTest()
        {
            Assert.Equal((int)0x00000001, NumberHelper<int>.MaxNumber((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.MaxNumber((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFF, NumberHelper<int>.MaxNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.MaxNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.MaxNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinTest()
        {
            Assert.Equal((int)0x00000000, NumberHelper<int>.Min((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Min((int)0x00000001, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.Min((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000000), NumberHelper<int>.Min(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberHelper<int>.Min(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinNumberTest()
        {
            Assert.Equal((int)0x00000000, NumberHelper<int>.MinNumber((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.MinNumber((int)0x00000001, 1));
            Assert.Equal((int)0x00000001, NumberHelper<int>.MinNumber((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000000), NumberHelper<int>.MinNumber(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberHelper<int>.MinNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void SignTest()
        {
            Assert.Equal(0, NumberHelper<int>.Sign((int)0x00000000));
            Assert.Equal(1, NumberHelper<int>.Sign((int)0x00000001));
            Assert.Equal(1, NumberHelper<int>.Sign((int)0x7FFFFFFF));
            Assert.Equal(-1, NumberHelper<int>.Sign(unchecked((int)0x80000000)));
            Assert.Equal(-1, NumberHelper<int>.Sign(unchecked((int)0xFFFFFFFF)));
        }

        //
        // INumberBase
        //

        [Fact]
        public static void OneTest()
        {
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.One);
        }

        [Fact]
        public static void RadixTest()
        {
            Assert.Equal(2, NumberBaseHelper<int>.Radix);
        }

        [Fact]
        public static void ZeroTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.Zero);
        }

        [Fact]
        public static void AbsTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.Abs((int)0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.Abs((int)0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.Abs((int)0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.Abs(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.Abs(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<byte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<byte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateChecked<byte>(0x7F));
            Assert.Equal((int)0x00000080, NumberBaseHelper<int>.CreateChecked<byte>(0x80));
            Assert.Equal((int)0x000000FF, NumberBaseHelper<int>.CreateChecked<byte>(0xFF));
        }

        [Fact]
        public static void CreateCheckedFromCharTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<char>((char)0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<char>((char)0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateChecked<char>((char)0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateChecked<char>((char)0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateChecked<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromDecimalTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<decimal>(-0.0m));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<decimal>(+0.0m));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<decimal>(+1.0m));

            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<decimal>(-1.0m));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<decimal>(decimal.MinValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromDoubleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<double>(+0.0));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<double>(-0.0));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<double>(+double.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<double>(-double.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<double>(+1.0));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<double>(-1.0));

            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateChecked<double>(+2147483647.0));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateChecked<double>(-2147483648.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(+2147483648.0));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(-2147483649.0));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(double.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(double.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(double.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromHalfTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<Half>(Half.Zero));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<Half>(Half.NegativeZero));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<Half>(+Half.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<Half>(-Half.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<Half>(Half.One));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<Half>(Half.NegativeOne));

            Assert.Equal((int)0x0000_FFE0, NumberBaseHelper<int>.CreateChecked<Half>(Half.MaxValue));
            Assert.Equal(unchecked((int)0xFFFF_0020), NumberBaseHelper<int>.CreateChecked<Half>(Half.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<Half>(Half.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<short>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<short>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateChecked<short>(0x7FFF));
            Assert.Equal(unchecked((int)0xFFFF8000), NumberBaseHelper<int>.CreateChecked<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<int>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<int>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateChecked<int>(unchecked(unchecked((int)0x80000000))));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<int>(unchecked(unchecked((int)0xFFFFFFFF))));
        }

        [Fact]
        public static void CreateCheckedFromInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<long>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<long>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateCheckedFromInt128Test()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<Int128>(Int128.Zero));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<Int128>(Int128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<Int128>(Int128.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateCheckedFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<nint>((nint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<nint>((nint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateCheckedFromNFloatTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<NFloat>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<NFloat>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<NFloat>(+NFloat.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<NFloat>(-NFloat.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<NFloat>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateChecked<NFloat>((NFloat)(+2147483647.0)));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateChecked<NFloat>((NFloat)(-2147483648.0)));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>((NFloat)(+2147483648.0)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>((NFloat)(-2147483649.0)));
            }
            else
            {
                Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateChecked<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateChecked<NFloat>(-2147483648.0f));

                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(+2147483647.0f));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(-2147483904.0f));
            }

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(NFloat.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(NFloat.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(NFloat.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromSByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<sbyte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<sbyte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateChecked<sbyte>(0x7F));
            Assert.Equal(unchecked((int)0xFFFFFF80), NumberBaseHelper<int>.CreateChecked<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateChecked<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateCheckedFromSingleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<float>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<float>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<float>(+float.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<float>(-float.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<float>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateChecked<float>(-1.0f));

            Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateChecked<float>(+2147483520.0f));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateChecked<float>(-2147483648.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(+2147483648.0f));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(-2147483904.0f));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(float.MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(float.MinValue));

            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(float.PositiveInfinity));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateCheckedFromUInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<ushort>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<ushort>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateChecked<ushort>(0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateChecked<ushort>(0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateChecked<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<uint>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<uint>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<uint>(0x7FFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<uint>(0x80000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<ulong>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<ulong>(0x0000000000000001));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<ulong>(0x8000000000000000));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateCheckedFromUInt128Test()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateChecked<UInt128>(UInt128.Zero));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateChecked<UInt128>(UInt128.One));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateCheckedFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateChecked<nuint>((nuint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateChecked<nuint>((nuint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<nuint>((nuint)0x7FFFFFFF));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nuint>((nuint)0x80000000));
                Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateSaturatingFromByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<byte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<byte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateSaturating<byte>(0x7F));
            Assert.Equal((int)0x00000080, NumberBaseHelper<int>.CreateSaturating<byte>(0x80));
            Assert.Equal((int)0x000000FF, NumberBaseHelper<int>.CreateSaturating<byte>(0xFF));
        }

        [Fact]
        public static void CreateSaturatingFromCharTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<char>((char)0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<char>((char)0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateSaturating<char>((char)0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateSaturating<char>((char)0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateSaturating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromDecimalTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<decimal>(-0.0m));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<decimal>(+0.0m));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<decimal>(+1.0m));

            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<decimal>(-1.0m));

            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromDoubleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<double>(+0.0));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<double>(-0.0));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<double>(+double.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<double>(-double.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<double>(+1.0));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<double>(-1.0));

            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<double>(+2147483647.0));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<double>(-2147483648.0));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<double>(+2147483648.0));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<double>(-2147483649.0));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<double>(double.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<double>(double.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromHalfTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<Half>(Half.Zero));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<Half>(Half.NegativeZero));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<Half>(+Half.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<Half>(-Half.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<Half>(Half.One));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<Half>(Half.NegativeOne));

            Assert.Equal((int)0x0000_FFE0, NumberBaseHelper<int>.CreateSaturating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((int)0xFFFF_0020), NumberBaseHelper<int>.CreateSaturating<Half>(Half.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<short>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<short>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateSaturating<short>(0x7FFF));
            Assert.Equal(unchecked((int)0xFFFF8000), NumberBaseHelper<int>.CreateSaturating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<int>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<int>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateSaturating<int>(unchecked(unchecked((int)0x80000000))));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<int>(unchecked(unchecked((int)0xFFFFFFFF))));
        }

        [Fact]
        public static void CreateSaturatingFromInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<long>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<long>(0x0000000000000001));
            Assert.Equal(unchecked((int)0x7FFFFFFF), NumberBaseHelper<int>.CreateSaturating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateSaturating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateSaturatingFromInt128Test()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<Int128>(Int128.Zero));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<Int128>(Int128.One));
            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateSaturatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((int)0x7FFFFFFF), NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<nint>((nint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<nint>((nint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateSaturatingFromNFloatTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<NFloat>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<NFloat>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<NFloat>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<NFloat>((NFloat)(+2147483647.0)));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>((NFloat)(-2147483648.0)));

                Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<NFloat>((NFloat)(+2147483648.0)));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>((NFloat)(-2147483649.0)));
            }
            else
            {
                Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateSaturating<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>(-2147483648.0f));

                Assert.Equal(unchecked((int)0x7FFF_FF80), NumberBaseHelper<int>.CreateSaturating<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>(-2147483904.0f));
            }

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromSByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<sbyte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<sbyte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateSaturating<sbyte>(0x7F));
            Assert.Equal(unchecked((int)0xFFFFFF80), NumberBaseHelper<int>.CreateSaturating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateSaturating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateSaturatingFromSingleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<float>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<float>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<float>(+float.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<float>(-float.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<float>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateSaturating<float>(-1.0f));

            Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateSaturating<float>(+2147483520.0f));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<float>(-2147483648.0f));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<float>(+2147483648.0f));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<float>(-2147483904.0f));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<float>(float.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<float>(float.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateSaturating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateSaturating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateSaturatingFromUInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<ushort>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<ushort>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateSaturating<ushort>(0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateSaturating<ushort>(0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateSaturating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<uint>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<uint>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<uint>(0x7FFFFFFF));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<uint>(0x80000000));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<ulong>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<ulong>(0x0000000000000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<ulong>(0x8000000000000000));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateSaturatingFromUInt128Test()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateSaturating<UInt128>(UInt128.Zero));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateSaturating<UInt128>(UInt128.One));
            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateSaturating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateSaturatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateSaturating<nuint>((nuint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateSaturating<nuint>((nuint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>((nuint)0x80000000));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateSaturating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void CreateTruncatingFromByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<byte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<byte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateTruncating<byte>(0x7F));
            Assert.Equal((int)0x00000080, NumberBaseHelper<int>.CreateTruncating<byte>(0x80));
            Assert.Equal((int)0x000000FF, NumberBaseHelper<int>.CreateTruncating<byte>(0xFF));
        }

        [Fact]
        public static void CreateTruncatingFromCharTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<char>((char)0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<char>((char)0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateTruncating<char>((char)0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateTruncating<char>((char)0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateTruncating<char>((char)0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromDecimalTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<decimal>(-0.0m));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<decimal>(+0.0m));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<decimal>(+1.0m));

            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<decimal>(-1.0m));

            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<decimal>(decimal.MinValue));
            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<decimal>(decimal.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromDoubleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<double>(+0.0));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<double>(-0.0));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<double>(+double.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<double>(-double.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<double>(+1.0));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<double>(-1.0));

            Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateTruncating<double>(+2147483647.0));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<double>(-2147483648.0));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<double>(+2147483648.0));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<double>(-2147483649.0));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<double>(double.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<double>(double.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<double>(double.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<double>(double.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromHalfTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<Half>(Half.Zero));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<Half>(Half.NegativeZero));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<Half>(+Half.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<Half>(-Half.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<Half>(Half.One));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<Half>(Half.NegativeOne));

            Assert.Equal((int)0x0000_FFE0, NumberBaseHelper<int>.CreateTruncating<Half>(Half.MaxValue));
            Assert.Equal(unchecked((int)0xFFFF_0020), NumberBaseHelper<int>.CreateTruncating<Half>(Half.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<Half>(Half.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<Half>(Half.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<short>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<short>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateTruncating<short>(0x7FFF));
            Assert.Equal(unchecked((int)0xFFFF8000), NumberBaseHelper<int>.CreateTruncating<short>(unchecked((short)0x8000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<short>(unchecked((short)0xFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<int>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<int>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateTruncating<int>(0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateTruncating<int>(unchecked(unchecked((int)0x80000000))));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<int>(unchecked(unchecked((int)0xFFFFFFFF))));
        }

        [Fact]
        public static void CreateTruncatingFromInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<long>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<long>(0x0000000000000001));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<long>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<long>(unchecked((long)0x8000000000000000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<long>(unchecked((long)0xFFFFFFFFFFFFFFFF)));
        }

        [Fact]
        public static void CreateTruncatingFromInt128Test()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<Int128>(Int128.Zero));
            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<Int128>(Int128.One));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<Int128>(Int128.MaxValue));
            Assert.Equal(unchecked((int)0x0000_0000), NumberBaseHelper<int>.CreateTruncating<Int128>(Int128.MinValue));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<Int128>(Int128.NegativeOne));
        }

        [Fact]
        public static void CreateTruncatingFromIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0x0000000000000001)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0x8000000000000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nint>((nint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<nint>((nint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateTruncating<nint>((nint)0x7FFFFFFF));
                Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0x80000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nint>(unchecked((nint)0xFFFFFFFF)));
            }
        }

        [Fact]
        public static void CreateTruncatingFromNFloatTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<NFloat>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<NFloat>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<NFloat>(+NFloat.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<NFloat>(-NFloat.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<NFloat>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<NFloat>(-1.0f));

            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x7FFF_FFFF, NumberBaseHelper<int>.CreateTruncating<NFloat>((NFloat)(+2147483647.0)));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>((NFloat)(-2147483648.0)));

                Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<NFloat>((NFloat)(+2147483648.0)));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>((NFloat)(-2147483649.0)));
            }
            else
            {
                Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateTruncating<NFloat>(+2147483520.0f));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>(-2147483648.0f));

                Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<NFloat>(+2147483647.0f));
                Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>(-2147483904.0f));
            }

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<NFloat>(NFloat.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>(NFloat.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<NFloat>(NFloat.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<NFloat>(NFloat.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromSByteTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<sbyte>(0x00));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<sbyte>(0x01));
            Assert.Equal((int)0x0000007F, NumberBaseHelper<int>.CreateTruncating<sbyte>(0x7F));
            Assert.Equal(unchecked((int)0xFFFFFF80), NumberBaseHelper<int>.CreateTruncating<sbyte>(unchecked((sbyte)0x80)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<sbyte>(unchecked((sbyte)0xFF)));
        }

        [Fact]
        public static void CreateTruncatingFromSingleTest()
        {
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<float>(+0.0f));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<float>(-0.0f));

            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<float>(+float.Epsilon));
            Assert.Equal((int)0x0000_0000, NumberBaseHelper<int>.CreateTruncating<float>(-float.Epsilon));

            Assert.Equal((int)0x0000_0001, NumberBaseHelper<int>.CreateTruncating<float>(+1.0f));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<float>(-1.0f));

            Assert.Equal((int)0x7FFF_FF80, NumberBaseHelper<int>.CreateTruncating<float>(+2147483520.0f));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<float>(-2147483648.0f));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<float>(+2147483648.0f));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<float>(-2147483904.0f));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<float>(float.MaxValue));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<float>(float.MinValue));

            Assert.Equal(unchecked((int)0x7FFF_FFFF), NumberBaseHelper<int>.CreateTruncating<float>(float.PositiveInfinity));
            Assert.Equal(unchecked((int)0x8000_0000), NumberBaseHelper<int>.CreateTruncating<float>(float.NegativeInfinity));
        }

        [Fact]
        public static void CreateTruncatingFromUInt16Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<ushort>(0x0000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<ushort>(0x0001));
            Assert.Equal((int)0x00007FFF, NumberBaseHelper<int>.CreateTruncating<ushort>(0x7FFF));
            Assert.Equal((int)0x00008000, NumberBaseHelper<int>.CreateTruncating<ushort>(0x8000));
            Assert.Equal((int)0x0000FFFF, NumberBaseHelper<int>.CreateTruncating<ushort>(0xFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt32Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<uint>(0x00000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<uint>(0x00000001));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateTruncating<uint>(0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateTruncating<uint>(0x80000000));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<uint>(0xFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt64Test()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<ulong>(0x0000000000000000));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<ulong>(0x0000000000000001));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<ulong>(0x7FFFFFFFFFFFFFFF));
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<ulong>(0x8000000000000000));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<ulong>(0xFFFFFFFFFFFFFFFF));
        }

        [Fact]
        public static void CreateTruncatingFromUInt128Test()
        {
            Assert.Equal(unchecked((int)0x0000_0000), NumberBaseHelper<int>.CreateTruncating<UInt128>(UInt128.Zero));
            Assert.Equal(unchecked((int)0x0000_0001), NumberBaseHelper<int>.CreateTruncating<UInt128>(UInt128.One));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValue));
            Assert.Equal(unchecked((int)0x0000_0000), NumberBaseHelper<int>.CreateTruncating<UInt128>(UInt128Tests_GenericMath.Int128MaxValuePlusOne));
            Assert.Equal(unchecked((int)0xFFFF_FFFF), NumberBaseHelper<int>.CreateTruncating<UInt128>(UInt128.MaxValue));
        }

        [Fact]
        public static void CreateTruncatingFromUIntPtrTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000000)));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<nuint>(unchecked((nuint)0x0000000000000001)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nuint>(unchecked((nuint)0x7FFFFFFFFFFFFFFF)));
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nuint>(unchecked((nuint)0x8000000000000000)));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nuint>(unchecked((nuint)0xFFFFFFFFFFFFFFFF)));
            }
            else
            {
                Assert.Equal((int)0x00000000, NumberBaseHelper<int>.CreateTruncating<nuint>((nuint)0x00000000));
                Assert.Equal((int)0x00000001, NumberBaseHelper<int>.CreateTruncating<nuint>((nuint)0x00000001));
                Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.CreateTruncating<nuint>((nuint)0x7FFFFFFF));
                Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.CreateTruncating<nuint>((nuint)0x80000000));
                Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.CreateTruncating<nuint>((nuint)0xFFFFFFFF));
            }
        }

        [Fact]
        public static void IsCanonicalTest()
        {
            Assert.True(NumberBaseHelper<int>.IsCanonical((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsCanonical((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsCanonical((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsCanonical(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsCanonical(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsComplexNumberTest()
        {
            Assert.False(NumberBaseHelper<int>.IsComplexNumber((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsComplexNumber((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsComplexNumber((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsComplexNumber(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsComplexNumber(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsEvenIntegerTest()
        {
            Assert.True(NumberBaseHelper<int>.IsEvenInteger((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsEvenInteger((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsEvenInteger((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsEvenInteger(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsEvenInteger(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsFiniteTest()
        {
            Assert.True(NumberBaseHelper<int>.IsFinite((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsFinite((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsFinite((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsFinite(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsFinite(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsImaginaryNumberTest()
        {
            Assert.False(NumberBaseHelper<int>.IsImaginaryNumber((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsImaginaryNumber((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsImaginaryNumber((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsImaginaryNumber(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsImaginaryNumber(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsInfinityTest()
        {
            Assert.False(NumberBaseHelper<int>.IsInfinity((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsInfinity((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsInfinity((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsInfinity(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsInfinity(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsIntegerTest()
        {
            Assert.True(NumberBaseHelper<int>.IsInteger((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsInteger((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsInteger((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsInteger(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsInteger(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsNaNTest()
        {
            Assert.False(NumberBaseHelper<int>.IsNaN((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsNaN((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsNaN((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsNaN(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsNaN(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsNegativeTest()
        {
            Assert.False(NumberBaseHelper<int>.IsNegative((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsNegative((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsNegative((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsNegative(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsNegative(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsNegativeInfinityTest()
        {
            Assert.False(NumberBaseHelper<int>.IsNegativeInfinity((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsNegativeInfinity((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsNegativeInfinity((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsNegativeInfinity(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsNegativeInfinity(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsNormalTest()
        {
            Assert.False(NumberBaseHelper<int>.IsNormal((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsNormal((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsNormal((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsNormal(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsNormal(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsOddIntegerTest()
        {
            Assert.False(NumberBaseHelper<int>.IsOddInteger((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsOddInteger((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsOddInteger((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsOddInteger(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsOddInteger(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsPositiveTest()
        {
            Assert.True(NumberBaseHelper<int>.IsPositive((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsPositive((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsPositive((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsPositive(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsPositive(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsPositiveInfinityTest()
        {
            Assert.False(NumberBaseHelper<int>.IsPositiveInfinity((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsPositiveInfinity((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsPositiveInfinity((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsPositiveInfinity(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsPositiveInfinity(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsRealNumberTest()
        {
            Assert.True(NumberBaseHelper<int>.IsRealNumber((int)0x00000000));
            Assert.True(NumberBaseHelper<int>.IsRealNumber((int)0x00000001));
            Assert.True(NumberBaseHelper<int>.IsRealNumber((int)0x7FFFFFFF));
            Assert.True(NumberBaseHelper<int>.IsRealNumber(unchecked((int)0x80000000)));
            Assert.True(NumberBaseHelper<int>.IsRealNumber(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsSubnormalTest()
        {
            Assert.False(NumberBaseHelper<int>.IsSubnormal((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsSubnormal((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsSubnormal((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsSubnormal(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsSubnormal(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void IsZeroTest()
        {
            Assert.True(NumberBaseHelper<int>.IsZero((int)0x00000000));
            Assert.False(NumberBaseHelper<int>.IsZero((int)0x00000001));
            Assert.False(NumberBaseHelper<int>.IsZero((int)0x7FFFFFFF));
            Assert.False(NumberBaseHelper<int>.IsZero(unchecked((int)0x80000000)));
            Assert.False(NumberBaseHelper<int>.IsZero(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void MaxMagnitudeTest()
        {
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitude((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitude((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.MaxMagnitude((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.MaxMagnitude(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitude(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxMagnitudeNumberTest()
        {
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitudeNumber((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitudeNumber((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFF, NumberBaseHelper<int>.MaxMagnitudeNumber((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0x80000000), NumberBaseHelper<int>.MaxMagnitudeNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MaxMagnitudeNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinMagnitudeTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.MinMagnitude((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitude((int)0x00000001, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitude((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitude(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.MinMagnitude(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinMagnitudeNumberTest()
        {
            Assert.Equal((int)0x00000000, NumberBaseHelper<int>.MinMagnitudeNumber((int)0x00000000, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitudeNumber((int)0x00000001, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitudeNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000001, NumberBaseHelper<int>.MinMagnitudeNumber(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), NumberBaseHelper<int>.MinMagnitudeNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        //
        // IShiftOperators
        //

        [Fact]
        public static void op_LeftShiftTest()
        {
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_LeftShift((int)0x00000000, 1));
            Assert.Equal((int)0x00000002, ShiftOperatorsHelper<int, int>.op_LeftShift((int)0x00000001, 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), ShiftOperatorsHelper<int, int>.op_LeftShift((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_LeftShift(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), ShiftOperatorsHelper<int, int>.op_LeftShift(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_RightShiftTest()
        {
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_RightShift((int)0x00000000, 1));
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_RightShift((int)0x00000001, 1));
            Assert.Equal((int)0x3FFFFFFF, ShiftOperatorsHelper<int, int>.op_RightShift((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0xC0000000), ShiftOperatorsHelper<int, int>.op_RightShift(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFF), ShiftOperatorsHelper<int, int>.op_RightShift(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_UnsignedRightShiftTest()
        {
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_UnsignedRightShift((int)0x00000000, 1));
            Assert.Equal((int)0x00000000, ShiftOperatorsHelper<int, int>.op_UnsignedRightShift((int)0x00000001, 1));
            Assert.Equal((int)0x3FFFFFFF, ShiftOperatorsHelper<int, int>.op_UnsignedRightShift((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x40000000, ShiftOperatorsHelper<int, int>.op_UnsignedRightShift(unchecked((int)0x80000000), 1));
            Assert.Equal((int)0x7FFFFFFF, ShiftOperatorsHelper<int, int>.op_UnsignedRightShift(unchecked((int)0xFFFFFFFF), 1));
        }

        //
        // ISignedNumber
        //

        [Fact]
        public static void NegativeOneTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), SignedNumberHelper<int>.NegativeOne);
        }

        //
        // ISubtractionOperators
        //

        [Fact]
        public static void op_SubtractionTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), SubtractionOperatorsHelper<int, int, int>.op_Subtraction((int)0x00000000, 1));
            Assert.Equal((int)0x00000000, SubtractionOperatorsHelper<int, int, int>.op_Subtraction((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFE, SubtractionOperatorsHelper<int, int, int>.op_Subtraction((int)0x7FFFFFFF, 1));
            Assert.Equal((int)0x7FFFFFFF, SubtractionOperatorsHelper<int, int, int>.op_Subtraction(unchecked((int)0x80000000), 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), SubtractionOperatorsHelper<int, int, int>.op_Subtraction(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void op_CheckedSubtractionTest()
        {
            Assert.Equal(unchecked((int)0xFFFFFFFF), SubtractionOperatorsHelper<int, int, int>.op_CheckedSubtraction((int)0x00000000, 1));
            Assert.Equal((int)0x00000000, SubtractionOperatorsHelper<int, int, int>.op_CheckedSubtraction((int)0x00000001, 1));
            Assert.Equal((int)0x7FFFFFFE, SubtractionOperatorsHelper<int, int, int>.op_CheckedSubtraction((int)0x7FFFFFFF, 1));
            Assert.Equal(unchecked((int)0xFFFFFFFE), SubtractionOperatorsHelper<int, int, int>.op_CheckedSubtraction(unchecked((int)0xFFFFFFFF), 1));

            Assert.Throws<OverflowException>(() => SubtractionOperatorsHelper<int, int, int>.op_CheckedSubtraction(unchecked((int)0x80000000), 1));
        }

        //
        // IUnaryNegationOperators
        //

        [Fact]
        public static void op_UnaryNegationTest()
        {
            Assert.Equal((int)0x00000000, UnaryNegationOperatorsHelper<int, int>.op_UnaryNegation((int)0x00000000));
            Assert.Equal(unchecked((int)0xFFFFFFFF), UnaryNegationOperatorsHelper<int, int>.op_UnaryNegation((int)0x00000001));
            Assert.Equal(unchecked((int)0x80000001), UnaryNegationOperatorsHelper<int, int>.op_UnaryNegation((int)0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), UnaryNegationOperatorsHelper<int, int>.op_UnaryNegation(unchecked((int)0x80000000)));
            Assert.Equal((int)0x00000001, UnaryNegationOperatorsHelper<int, int>.op_UnaryNegation(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void op_CheckedUnaryNegationTest()
        {
            Assert.Equal((int)0x00000000, UnaryNegationOperatorsHelper<int, int>.op_CheckedUnaryNegation((int)0x00000000));
            Assert.Equal(unchecked((int)0xFFFFFFFF), UnaryNegationOperatorsHelper<int, int>.op_CheckedUnaryNegation((int)0x00000001));
            Assert.Equal(unchecked((int)0x80000001), UnaryNegationOperatorsHelper<int, int>.op_CheckedUnaryNegation((int)0x7FFFFFFF));
            Assert.Equal((int)0x00000001, UnaryNegationOperatorsHelper<int, int>.op_CheckedUnaryNegation(unchecked((int)0xFFFFFFFF)));

            Assert.Throws<OverflowException>(() => UnaryNegationOperatorsHelper<int, int>.op_CheckedUnaryNegation(unchecked((int)0x80000000)));
        }

        //
        // IUnaryPlusOperators
        //

        [Fact]
        public static void op_UnaryPlusTest()
        {
            Assert.Equal((int)0x00000000, UnaryPlusOperatorsHelper<int, int>.op_UnaryPlus((int)0x00000000));
            Assert.Equal((int)0x00000001, UnaryPlusOperatorsHelper<int, int>.op_UnaryPlus((int)0x00000001));
            Assert.Equal((int)0x7FFFFFFF, UnaryPlusOperatorsHelper<int, int>.op_UnaryPlus((int)0x7FFFFFFF));
            Assert.Equal(unchecked((int)0x80000000), UnaryPlusOperatorsHelper<int, int>.op_UnaryPlus(unchecked((int)0x80000000)));
            Assert.Equal(unchecked((int)0xFFFFFFFF), UnaryPlusOperatorsHelper<int, int>.op_UnaryPlus(unchecked((int)0xFFFFFFFF)));
        }

        //
        // IParsable and ISpanParsable
        //

        [Theory]
        [MemberData(nameof(Int32Tests.Parse_Valid_TestData), MemberType = typeof(Int32Tests))]
        public static void ParseValidStringTest(string value, NumberStyles style, IFormatProvider provider, int expected)
        {
            int result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(ParsableHelper<int>.TryParse(value, provider, out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, ParsableHelper<int>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Equal(expected, NumberBaseHelper<int>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.True(NumberBaseHelper<int>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(expected, result);
                Assert.Equal(expected, NumberBaseHelper<int>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Equal(expected, ParsableHelper<int>.Parse(value, provider));
            }

            // Full overloads
            Assert.True(NumberBaseHelper<int>.TryParse(value, style, provider, out result));
            Assert.Equal(expected, result);
            Assert.Equal(expected, NumberBaseHelper<int>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int32Tests.Parse_Invalid_TestData), MemberType = typeof(Int32Tests))]
        public static void ParseInvalidStringTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            int result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(ParsableHelper<int>.TryParse(value, provider, out result));
                Assert.Equal(default(int), result);
                Assert.Throws(exceptionType, () => ParsableHelper<int>.Parse(value, provider));
            }

            // Default provider
            if (provider is null)
            {
                Assert.Throws(exceptionType, () => NumberBaseHelper<int>.Parse(value, style, provider));

                // Substitute default NumberFormatInfo
                Assert.False(NumberBaseHelper<int>.TryParse(value, style, new NumberFormatInfo(), out result));
                Assert.Equal(default(int), result);
                Assert.Throws(exceptionType, () => NumberBaseHelper<int>.Parse(value, style, new NumberFormatInfo()));
            }

            // Default style
            if (style == NumberStyles.Integer)
            {
                Assert.Throws(exceptionType, () => ParsableHelper<int>.Parse(value, provider));
            }

            // Full overloads
            Assert.False(NumberBaseHelper<int>.TryParse(value, style, provider, out result));
            Assert.Equal(default(int), result);
            Assert.Throws(exceptionType, () => NumberBaseHelper<int>.Parse(value, style, provider));
        }

        [Theory]
        [MemberData(nameof(Int32Tests.Parse_ValidWithOffsetCount_TestData), MemberType = typeof(Int32Tests))]
        public static void ParseValidSpanTest(string value, int offset, int count, NumberStyles style, IFormatProvider provider, int expected)
        {
            int result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.True(SpanParsableHelper<int>.TryParse(value.AsSpan(offset, count), provider, out result));
                Assert.Equal(expected, result);
            }

            Assert.Equal(expected, NumberBaseHelper<int>.Parse(value.AsSpan(offset, count), style, provider));

            Assert.True(NumberBaseHelper<int>.TryParse(value.AsSpan(offset, count), style, provider, out result));
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(Int32Tests.Parse_Invalid_TestData), MemberType = typeof(Int32Tests))]
        public static void ParseInvalidSpanTest(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
        {
            if (value is null)
            {
                return;
            }

            int result;

            // Default style and provider
            if ((style == NumberStyles.Integer) && (provider is null))
            {
                Assert.False(SpanParsableHelper<int>.TryParse(value.AsSpan(), provider, out result));
                Assert.Equal(default(int), result);
            }

            Assert.Throws(exceptionType, () => NumberBaseHelper<int>.Parse(value.AsSpan(), style, provider));

            Assert.False(NumberBaseHelper<int>.TryParse(value.AsSpan(), style, provider, out result));
            Assert.Equal(default(int), result);
        }
    }
}
