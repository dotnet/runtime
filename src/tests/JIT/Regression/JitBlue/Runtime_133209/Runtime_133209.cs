// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133209;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Morph folds "CAST(int <- long, LCL_VAR long)" into "LCL_VAR int" naming the same long local.
// Codegen paths that re-materialize such an operand for an exception check must narrow it again;
// on wasm, failing to do so emits an i64 where an i32 is expected and the module does not validate.
public class Runtime_133209
{
    [Theory]
    [InlineData(100, 7L, 14, 2)]
    [InlineData(-100, 7L, -14, -2)]
    [InlineData(int.MinValue, 2L, int.MinValue / 2, 0)]
    [InlineData(7, 0x1_0000_0003L, 2, 1)]
    public static void DivMod_WithNarrowedDivisor(int dividend, long divisor, int quotient, int remainder)
    {
        Assert.Equal(quotient, Divide(dividend, divisor));
        Assert.Equal(remainder, Remainder(dividend, divisor));
    }

    [Theory]
    [InlineData(100u, 7UL, 14u)]
    [InlineData(uint.MaxValue, 0x1_0000_0002UL, uint.MaxValue / 2)]
    public static void UnsignedDivide_WithNarrowedDivisor(uint dividend, ulong divisor, uint quotient)
    {
        Assert.Equal(quotient, UnsignedDivide(dividend, divisor));
    }

    [Fact]
    public static void Divide_WithNarrowedZeroDivisor_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => Divide(1, 0x1_0000_0000L));
        Assert.Throws<DivideByZeroException>(() => Remainder(1, 0x1_0000_0000L));
        Assert.Throws<DivideByZeroException>(() => UnsignedDivide(1, 0x1_0000_0000UL));
    }

    [Fact]
    public static void Divide_MinValueByNarrowedNegativeOne_Throws()
    {
        Assert.Throws<OverflowException>(() => Divide(int.MinValue, -1L));
    }

    [Theory]
    [InlineData(1, 2L, 3)]
    [InlineData(-5, 0x1_0000_0003L, -2)]
    public static void CheckedAdd_WithNarrowedOperand(int a, long b, int expected)
    {
        Assert.Equal(expected, CheckedAdd(a, b));
    }

    [Theory]
    [InlineData(10, 3L, 7)]
    [InlineData(-5, 0x1_0000_0003L, -8)]
    public static void CheckedSubtract_WithNarrowedOperand(int a, long b, int expected)
    {
        Assert.Equal(expected, CheckedSubtract(a, b));
    }

    [Theory]
    [InlineData(6, 7L, 42)]
    [InlineData(-5, 0x1_0000_0003L, -15)]
    public static void CheckedMultiply_WithNarrowedOperand(int a, long b, int expected)
    {
        Assert.Equal(expected, CheckedMultiply(a, b));
    }

    [Theory]
    [InlineData(1u, 2UL, 3u)]
    [InlineData(1u, 0x1_0000_0003UL, 4u)]
    public static void CheckedUnsignedAdd_WithNarrowedOperand(uint a, ulong b, uint expected)
    {
        Assert.Equal(expected, CheckedUnsignedAdd(a, b));
        Assert.Equal(expected, CheckedUnsignedAddReversed(a, b));
    }

    [Fact]
    public static void CheckedArithmetic_WithNarrowedOperand_Overflows()
    {
        Assert.Throws<OverflowException>(() => CheckedAdd(int.MaxValue, 1L));
        Assert.Throws<OverflowException>(() => CheckedSubtract(int.MinValue, 1L));
        Assert.Throws<OverflowException>(() => CheckedMultiply(int.MaxValue, 2L));
        Assert.Throws<OverflowException>(() => CheckedUnsignedAdd(uint.MaxValue, 1UL));
        Assert.Throws<OverflowException>(() => CheckedUnsignedAddReversed(uint.MaxValue, 1UL));
    }

    [Theory]
    [InlineData(0x1_0000_007FL, (byte)0x7F, (sbyte)0x7F)]
    [InlineData(0x1_0000_0001L, (byte)1, (sbyte)1)]
    public static void CheckedNarrowingCast_WithNarrowedOperand(long b, byte expectedByte, sbyte expectedSByte)
    {
        Assert.Equal(expectedByte, CheckedToByte(b));
        Assert.Equal(expectedSByte, CheckedToSByte(b));
        Assert.Equal((uint)expectedByte, CheckedToUInt(b));
    }

    [Fact]
    public static void CheckedNarrowingCast_WithNarrowedOperand_Overflows()
    {
        Assert.Throws<OverflowException>(() => CheckedToByte(0x1_0000_0100L));
        Assert.Throws<OverflowException>(() => CheckedToSByte(0x1_0000_0080L));
        Assert.Throws<OverflowException>(() => CheckedToUInt(-1L));
    }

    [Theory]
    [InlineData(0x1_0000_0002L, 30)]
    [InlineData(0L, 10)]
    public static void ArrayIndex_WithNarrowedIndex(long index, int expected)
    {
        Assert.Equal(expected, ElementAt(new[] { 10, 20, 30, 40 }, index));
    }

    // On wasm32 "nint" is 32 bits, so "(nint)someLong" narrows exactly like "(int)someLong" and the
    // resulting address is re-materialized for the indirection's null check.
    [Fact]
    public static unsafe void Dereference_WithNarrowedAddress()
    {
        // Each access reads back the same width it wrote, so the expected values do not depend on
        // the target's byte order.
        int intValue = 0;
        long intAddress = (long)(nint)(&intValue);

        StoreThroughNarrowedAddress(intAddress, 0x12345678);
        Assert.Equal(0x12345678, intValue);
        Assert.Equal(0x12345678, LoadThroughNarrowedAddress(intAddress));

        long longValue = 0x123456789ABCDEF0L;
        long longAddress = (long)(nint)(&longValue);

        Assert.Equal(longValue, LoadLongThroughNarrowedAddress(longAddress));
    }

    [Fact]
    public static unsafe void BlockOps_WithNarrowedAddresses()
    {
        byte* source = stackalloc byte[16];
        byte* destination = stackalloc byte[16];

        for (int i = 0; i < 16; i++)
        {
            source[i] = (byte)(i + 1);
            destination[i] = 0;
        }

        CopyBlockThroughNarrowedAddresses((long)(nint)destination, (long)(nint)source, 16);
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal((byte)(i + 1), destination[i]);
        }

        InitBlockThroughNarrowedAddress((long)(nint)destination, 0xAB, 16);
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal((byte)0xAB, destination[i]);
        }
    }

    // TYP_SIMD12 (Vector3) load/store re-materialize the address for the trailing lane access.
    [Fact]
    public static unsafe void Vector3_WithNarrowedAddress()
    {
        Vector3 value = default;
        long address = (long)(nint)(&value);
        Vector3 expected = new Vector3(1.5f, 2.5f, 3.5f);

        StoreVector3ThroughNarrowedAddress(address, expected);
        Assert.Equal(expected, value);
        Assert.Equal(expected, LoadVector3ThroughNarrowedAddress(address));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Divide(int a, long b) => a / (int)b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Remainder(int a, long b) => a % (int)b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint UnsignedDivide(uint a, ulong b) => a / (uint)b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedAdd(int a, long b) => checked(a + unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedSubtract(int a, long b) => checked(a - unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CheckedMultiply(int a, long b) => checked(a * unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint CheckedUnsignedAdd(uint a, ulong b) => checked(a + unchecked((uint)b));

    // The narrowed operand in the first position, which the unsigned overflow check re-reads.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint CheckedUnsignedAddReversed(uint a, ulong b) => checked(unchecked((uint)b) + a);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CheckedToByte(long b) => checked((byte)unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static sbyte CheckedToSByte(long b) => checked((sbyte)unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint CheckedToUInt(long b) => checked((uint)unchecked((int)b));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ElementAt(int[] array, long index) => array[unchecked((int)index)];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int LoadThroughNarrowedAddress(long address) => *(int*)(nint)address;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe long LoadLongThroughNarrowedAddress(long address) => *(long*)(nint)address;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void StoreThroughNarrowedAddress(long address, int value) => *(int*)(nint)address = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void CopyBlockThroughNarrowedAddresses(long destination, long source, uint byteCount) =>
        Unsafe.CopyBlock((void*)(nint)destination, (void*)(nint)source, byteCount);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void InitBlockThroughNarrowedAddress(long destination, byte value, uint byteCount) =>
        Unsafe.InitBlock((void*)(nint)destination, value, byteCount);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Vector3 LoadVector3ThroughNarrowedAddress(long address) => *(Vector3*)(nint)address;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void StoreVector3ThroughNarrowedAddress(long address, Vector3 value) =>
        *(Vector3*)(nint)address = value;
}
