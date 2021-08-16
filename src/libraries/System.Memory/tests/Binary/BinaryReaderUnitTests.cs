// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Binary.Tests
{
    public class BinaryReaderUnitTests
    {
        [Fact]
        public void SpanRead()
        {
            ulong value; // [11 22 33 44 55 66 77 88]
            if (BitConverter.IsLittleEndian)
            {
                value = 0x8877665544332211;
            }
            else
            {
                value = 0x1122334455667788;
            }
            Span<byte> span;
            unsafe
            {
                span = new Span<byte>(&value, 8);
            }

            Assert.Equal<byte>(0x11, MemoryMarshal.Read<byte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte byteValue));
            Assert.Equal(0x11, byteValue);

            Assert.Equal<sbyte>(0x11, MemoryMarshal.Read<sbyte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte sbyteValue));
            Assert.Equal(0x11, byteValue);

            Assert.Equal<ushort>(0x1122, ReadUInt16BigEndian(span));
            Assert.True(TryReadUInt16BigEndian(span, out ushort ushortValue));
            Assert.Equal(0x1122, ushortValue);

            Assert.Equal<ushort>(0x2211, ReadUInt16LittleEndian(span));
            Assert.True(TryReadUInt16LittleEndian(span, out ushortValue));
            Assert.Equal(0x2211, ushortValue);

            Assert.Equal<short>(0x1122, ReadInt16BigEndian(span));
            Assert.True(TryReadInt16BigEndian(span, out short shortValue));
            Assert.Equal(0x1122, shortValue);

            Assert.Equal<short>(0x2211, ReadInt16LittleEndian(span));
            Assert.True(TryReadInt16LittleEndian(span, out shortValue));
            Assert.Equal(0x2211, ushortValue);

            Assert.Equal<uint>(0x11223344, ReadUInt32BigEndian(span));
            Assert.True(TryReadUInt32BigEndian(span, out uint uintValue));
            Assert.Equal<uint>(0x11223344, uintValue);

            Assert.Equal<uint>(0x44332211, ReadUInt32LittleEndian(span));
            Assert.True(TryReadUInt32LittleEndian(span, out uintValue));
            Assert.Equal<uint>(0x44332211, uintValue);

            Assert.Equal<int>(0x11223344, ReadInt32BigEndian(span));
            Assert.True(TryReadInt32BigEndian(span, out int intValue));
            Assert.Equal<int>(0x11223344, intValue);

            Assert.Equal<int>(0x44332211, ReadInt32LittleEndian(span));
            Assert.True(TryReadInt32LittleEndian(span, out intValue));
            Assert.Equal<int>(0x44332211, intValue);

            Assert.Equal<ulong>(0x1122334455667788, ReadUInt64BigEndian(span));
            Assert.True(TryReadUInt64BigEndian(span, out ulong ulongValue));
            Assert.Equal<ulong>(0x1122334455667788, ulongValue);

            Assert.Equal<ulong>(0x8877665544332211, ReadUInt64LittleEndian(span));
            Assert.True(TryReadUInt64LittleEndian(span, out ulongValue));
            Assert.Equal<ulong>(0x8877665544332211, ulongValue);

            Assert.Equal<long>(0x1122334455667788, ReadInt64BigEndian(span));
            Assert.True(TryReadInt64BigEndian(span, out long longValue));
            Assert.Equal<long>(0x1122334455667788, longValue);

            Assert.Equal<long>(unchecked((long)0x8877665544332211), ReadInt64LittleEndian(span));
            Assert.True(TryReadInt64LittleEndian(span, out longValue));
            Assert.Equal<long>(unchecked((long)0x8877665544332211), longValue);

            Half expectedHalf = BitConverter.Int16BitsToHalf(0x1122);
            Assert.Equal<Half>(expectedHalf, ReadHalfBigEndian(span));
            Assert.True(TryReadHalfBigEndian(span, out Half halfValue));
            Assert.Equal<Half>(expectedHalf, halfValue);

            expectedHalf = BitConverter.Int16BitsToHalf(0x2211);
            Assert.Equal<Half>(expectedHalf, ReadHalfLittleEndian(span));
            Assert.True(TryReadHalfLittleEndian(span, out halfValue));
            Assert.Equal<Half>(expectedHalf, halfValue);

            float expectedFloat = BitConverter.Int32BitsToSingle(0x11223344);
            Assert.Equal<float>(expectedFloat, ReadSingleBigEndian(span));
            Assert.True(TryReadSingleBigEndian(span, out float floatValue));
            Assert.Equal<float>(expectedFloat, floatValue);

            expectedFloat = BitConverter.Int32BitsToSingle(0x44332211);
            Assert.Equal<float>(expectedFloat, ReadSingleLittleEndian(span));
            Assert.True(TryReadSingleLittleEndian(span, out floatValue));
            Assert.Equal<float>(expectedFloat, floatValue);

            double expectedDouble = BitConverter.Int64BitsToDouble(0x1122334455667788);
            Assert.Equal<double>(expectedDouble, ReadDoubleBigEndian(span));
            Assert.True(TryReadDoubleBigEndian(span, out double doubleValue));
            Assert.Equal<double>(expectedDouble, doubleValue);

            expectedDouble = BitConverter.Int64BitsToDouble(unchecked((long)0x8877665544332211));
            Assert.Equal<double>(expectedDouble, ReadDoubleLittleEndian(span));
            Assert.True(TryReadDoubleLittleEndian(span, out doubleValue));
            Assert.Equal<double>(expectedDouble, doubleValue);
        }

        [Fact]
        public void ReadOnlySpanRead()
        {
            ulong value; // [11 22 33 44 55 66 77 88]
            if (BitConverter.IsLittleEndian)
            {
                value = 0x8877665544332211;
            }
            else
            {
                value = 0x1122334455667788;
            }
            ReadOnlySpan<byte> span;
            unsafe
            {
                span = new ReadOnlySpan<byte>(&value, 8);
            }

            Assert.Equal<byte>(0x11, MemoryMarshal.Read<byte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte byteValue));
            Assert.Equal(0x11, byteValue);

            Assert.Equal<sbyte>(0x11, MemoryMarshal.Read<sbyte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte sbyteValue));
            Assert.Equal(0x11, byteValue);

            Assert.Equal<ushort>(0x1122, ReadUInt16BigEndian(span));
            Assert.True(TryReadUInt16BigEndian(span, out ushort ushortValue));
            Assert.Equal(0x1122, ushortValue);

            Assert.Equal<ushort>(0x2211, ReadUInt16LittleEndian(span));
            Assert.True(TryReadUInt16LittleEndian(span, out ushortValue));
            Assert.Equal(0x2211, ushortValue);

            Assert.Equal<short>(0x1122, ReadInt16BigEndian(span));
            Assert.True(TryReadInt16BigEndian(span, out short shortValue));
            Assert.Equal(0x1122, shortValue);

            Assert.Equal<short>(0x2211, ReadInt16LittleEndian(span));
            Assert.True(TryReadInt16LittleEndian(span, out shortValue));
            Assert.Equal(0x2211, ushortValue);

            Assert.Equal<uint>(0x11223344, ReadUInt32BigEndian(span));
            Assert.True(TryReadUInt32BigEndian(span, out uint uintValue));
            Assert.Equal<uint>(0x11223344, uintValue);

            Assert.Equal<uint>(0x44332211, ReadUInt32LittleEndian(span));
            Assert.True(TryReadUInt32LittleEndian(span, out uintValue));
            Assert.Equal<uint>(0x44332211, uintValue);

            Assert.Equal<int>(0x11223344, ReadInt32BigEndian(span));
            Assert.True(TryReadInt32BigEndian(span, out int intValue));
            Assert.Equal<int>(0x11223344, intValue);

            Assert.Equal<int>(0x44332211, ReadInt32LittleEndian(span));
            Assert.True(TryReadInt32LittleEndian(span, out intValue));
            Assert.Equal<int>(0x44332211, intValue);

            Assert.Equal<ulong>(0x1122334455667788, ReadUInt64BigEndian(span));
            Assert.True(TryReadUInt64BigEndian(span, out ulong ulongValue));
            Assert.Equal<ulong>(0x1122334455667788, ulongValue);

            Assert.Equal<ulong>(0x8877665544332211, ReadUInt64LittleEndian(span));
            Assert.True(TryReadUInt64LittleEndian(span, out ulongValue));
            Assert.Equal<ulong>(0x8877665544332211, ulongValue);

            Assert.Equal<long>(0x1122334455667788, ReadInt64BigEndian(span));
            Assert.True(TryReadInt64BigEndian(span, out long longValue));
            Assert.Equal<long>(0x1122334455667788, longValue);

            Assert.Equal<long>(unchecked((long)0x8877665544332211), ReadInt64LittleEndian(span));
            Assert.True(TryReadInt64LittleEndian(span, out longValue));
            Assert.Equal<long>(unchecked((long)0x8877665544332211), longValue);

            Half expectedHalf = BitConverter.Int16BitsToHalf(0x1122);
            Assert.Equal<Half>(expectedHalf, ReadHalfBigEndian(span));
            Assert.True(TryReadHalfBigEndian(span, out Half halfValue));
            Assert.Equal<Half>(expectedHalf, halfValue);

            expectedHalf = BitConverter.Int16BitsToHalf(0x2211);
            Assert.Equal<Half>(expectedHalf, ReadHalfLittleEndian(span));
            Assert.True(TryReadHalfLittleEndian(span, out halfValue));
            Assert.Equal<Half>(expectedHalf, halfValue);

            float expectedFloat = BitConverter.Int32BitsToSingle(0x11223344);
            Assert.Equal<float>(expectedFloat, ReadSingleBigEndian(span));
            Assert.True(TryReadSingleBigEndian(span, out float floatValue));
            Assert.Equal<float>(expectedFloat, floatValue);

            expectedFloat = BitConverter.Int32BitsToSingle(0x44332211);
            Assert.Equal<float>(expectedFloat, ReadSingleLittleEndian(span));
            Assert.True(TryReadSingleLittleEndian(span, out floatValue));
            Assert.Equal<float>(expectedFloat, floatValue);

            double expectedDouble = BitConverter.Int64BitsToDouble(0x1122334455667788);
            Assert.Equal<double>(expectedDouble, ReadDoubleBigEndian(span));
            Assert.True(TryReadDoubleBigEndian(span, out double doubleValue));
            Assert.Equal<double>(expectedDouble, doubleValue);

            expectedDouble = BitConverter.Int64BitsToDouble(unchecked((long)0x8877665544332211));
            Assert.Equal<double>(expectedDouble, ReadDoubleLittleEndian(span));
            Assert.True(TryReadDoubleLittleEndian(span, out doubleValue));
            Assert.Equal<double>(expectedDouble, doubleValue);
        }

        [Fact]
        public void SpanReadFail()
        {
            Span<byte> span = new byte[] { 1 };

            Assert.Equal<byte>(1, MemoryMarshal.Read<byte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte byteValue));
            Assert.Equal(1, byteValue);

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<short>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out short shortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<int>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out int intValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<long>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out long longValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<ushort>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out ushort ushortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<uint>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out uint uintValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<ulong>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out ulong ulongValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<Half>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out Half halfValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<float>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out float floatValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<double>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out double doubleValue));

            Span<byte> largeSpan = new byte[100];
            TestHelpers.AssertThrows<ArgumentException, byte>(largeSpan, (_span) => MemoryMarshal.Read<TestHelpers.TestValueTypeWithReference>(_span));
            TestHelpers.AssertThrows<ArgumentException, byte>(largeSpan, (_span) => MemoryMarshal.TryRead(_span, out TestHelpers.TestValueTypeWithReference stringValue));
        }

        [Fact]
        public void ReadOnlySpanReadFail()
        {
            ReadOnlySpan<byte> span = new byte[] { 1 };

            Assert.Equal<byte>(1, MemoryMarshal.Read<byte>(span));
            Assert.True(MemoryMarshal.TryRead(span, out byte byteValue));
            Assert.Equal(1, byteValue);

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<short>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out short shortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<int>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out int intValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<long>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out long longValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<ushort>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out ushort ushortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<uint>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out uint uintValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<ulong>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out ulong ulongValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<Half>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out Half halfValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<float>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out float floatValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Read<double>(_span));
            Assert.False(MemoryMarshal.TryRead(span, out double doubleValue));

            ReadOnlySpan<byte> largeSpan = new byte[100];
            TestHelpers.AssertThrows<ArgumentException, byte>(largeSpan, (_span) => MemoryMarshal.Read<TestHelpers.TestValueTypeWithReference>(_span));
            TestHelpers.AssertThrows<ArgumentException, byte>(largeSpan, (_span) => MemoryMarshal.TryRead(_span, out TestHelpers.TestValueTypeWithReference stringValue));
        }

        [Fact]
        public void SpanWriteAndReadBigEndianHeterogeneousStruct()
        {
            Span<byte> spanBE = new byte[Unsafe.SizeOf<TestStruct>()];

            WriteInt16BigEndian(spanBE, s_testStruct.S0);
            WriteInt32BigEndian(spanBE.Slice(2), s_testStruct.I0);
            WriteInt64BigEndian(spanBE.Slice(6), s_testStruct.L0);
            WriteUInt16BigEndian(spanBE.Slice(14), s_testStruct.US0);
            WriteUInt32BigEndian(spanBE.Slice(16), s_testStruct.UI0);
            WriteUInt64BigEndian(spanBE.Slice(20), s_testStruct.UL0);
            WriteSingleBigEndian(spanBE.Slice(28), s_testStruct.F0);
            WriteDoubleBigEndian(spanBE.Slice(32), s_testStruct.D0);
            WriteInt16BigEndian(spanBE.Slice(40), s_testStruct.S1);
            WriteInt32BigEndian(spanBE.Slice(42), s_testStruct.I1);
            WriteInt64BigEndian(spanBE.Slice(46), s_testStruct.L1);
            WriteUInt16BigEndian(spanBE.Slice(54), s_testStruct.US1);
            WriteUInt32BigEndian(spanBE.Slice(56), s_testStruct.UI1);
            WriteUInt64BigEndian(spanBE.Slice(60), s_testStruct.UL1);
            WriteSingleBigEndian(spanBE.Slice(68), s_testStruct.F1);
            WriteDoubleBigEndian(spanBE.Slice(72), s_testStruct.D1);
            WriteHalfBigEndian(spanBE.Slice(80), s_testStruct.H0);
            WriteHalfBigEndian(spanBE.Slice(82), s_testStruct.H1);

            ReadOnlySpan<byte> readOnlySpanBE = new ReadOnlySpan<byte>(spanBE.ToArray());

            var readStruct = new TestStruct
            {
                S0 = ReadInt16BigEndian(spanBE),
                I0 = ReadInt32BigEndian(spanBE.Slice(2)),
                L0 = ReadInt64BigEndian(spanBE.Slice(6)),
                US0 = ReadUInt16BigEndian(spanBE.Slice(14)),
                UI0 = ReadUInt32BigEndian(spanBE.Slice(16)),
                UL0 = ReadUInt64BigEndian(spanBE.Slice(20)),
                F0 = ReadSingleBigEndian(spanBE.Slice(28)),
                D0 = ReadDoubleBigEndian(spanBE.Slice(32)),
                S1 = ReadInt16BigEndian(spanBE.Slice(40)),
                I1 = ReadInt32BigEndian(spanBE.Slice(42)),
                L1 = ReadInt64BigEndian(spanBE.Slice(46)),
                US1 = ReadUInt16BigEndian(spanBE.Slice(54)),
                UI1 = ReadUInt32BigEndian(spanBE.Slice(56)),
                UL1 = ReadUInt64BigEndian(spanBE.Slice(60)),
                F1 = ReadSingleBigEndian(spanBE.Slice(68)),
                D1 = ReadDoubleBigEndian(spanBE.Slice(72)),
                H0 = ReadHalfBigEndian(spanBE.Slice(80)),
                H1 = ReadHalfBigEndian(spanBE.Slice(82))
            };

            var readStructFromReadOnlySpan = new TestStruct
            {
                S0 = ReadInt16BigEndian(readOnlySpanBE),
                I0 = ReadInt32BigEndian(readOnlySpanBE.Slice(2)),
                L0 = ReadInt64BigEndian(readOnlySpanBE.Slice(6)),
                US0 = ReadUInt16BigEndian(readOnlySpanBE.Slice(14)),
                UI0 = ReadUInt32BigEndian(readOnlySpanBE.Slice(16)),
                UL0 = ReadUInt64BigEndian(readOnlySpanBE.Slice(20)),
                F0 = ReadSingleBigEndian(readOnlySpanBE.Slice(28)),
                D0 = ReadDoubleBigEndian(readOnlySpanBE.Slice(32)),
                S1 = ReadInt16BigEndian(readOnlySpanBE.Slice(40)),
                I1 = ReadInt32BigEndian(readOnlySpanBE.Slice(42)),
                L1 = ReadInt64BigEndian(readOnlySpanBE.Slice(46)),
                US1 = ReadUInt16BigEndian(readOnlySpanBE.Slice(54)),
                UI1 = ReadUInt32BigEndian(readOnlySpanBE.Slice(56)),
                UL1 = ReadUInt64BigEndian(readOnlySpanBE.Slice(60)),
                F1 = ReadSingleBigEndian(readOnlySpanBE.Slice(68)),
                D1 = ReadDoubleBigEndian(readOnlySpanBE.Slice(72)),
                H0 = ReadHalfBigEndian(readOnlySpanBE.Slice(80)),
                H1 = ReadHalfBigEndian(readOnlySpanBE.Slice(82))
            };

            Assert.Equal(s_testStruct, readStruct);
            Assert.Equal(s_testStruct, readStructFromReadOnlySpan);
        }

        [Fact]
        public void SpanWriteAndReadLittleEndianHeterogeneousStruct()
        {
            Span<byte> spanLE = new byte[Unsafe.SizeOf<TestStruct>()];

            WriteInt16LittleEndian(spanLE, s_testStruct.S0);
            WriteInt32LittleEndian(spanLE.Slice(2), s_testStruct.I0);
            WriteInt64LittleEndian(spanLE.Slice(6), s_testStruct.L0);
            WriteUInt16LittleEndian(spanLE.Slice(14), s_testStruct.US0);
            WriteUInt32LittleEndian(spanLE.Slice(16), s_testStruct.UI0);
            WriteUInt64LittleEndian(spanLE.Slice(20), s_testStruct.UL0);
            WriteSingleLittleEndian(spanLE.Slice(28), s_testStruct.F0);
            WriteDoubleLittleEndian(spanLE.Slice(32), s_testStruct.D0);
            WriteInt16LittleEndian(spanLE.Slice(40), s_testStruct.S1);
            WriteInt32LittleEndian(spanLE.Slice(42), s_testStruct.I1);
            WriteInt64LittleEndian(spanLE.Slice(46), s_testStruct.L1);
            WriteUInt16LittleEndian(spanLE.Slice(54), s_testStruct.US1);
            WriteUInt32LittleEndian(spanLE.Slice(56), s_testStruct.UI1);
            WriteUInt64LittleEndian(spanLE.Slice(60), s_testStruct.UL1);
            WriteSingleLittleEndian(spanLE.Slice(68), s_testStruct.F1);
            WriteDoubleLittleEndian(spanLE.Slice(72), s_testStruct.D1);
            WriteHalfLittleEndian(spanLE.Slice(80), s_testStruct.H0);
            WriteHalfLittleEndian(spanLE.Slice(82), s_testStruct.H1);

            ReadOnlySpan<byte> readOnlySpanLE = new ReadOnlySpan<byte>(spanLE.ToArray());

            var readStruct = new TestStruct
            {
                S0 = ReadInt16LittleEndian(spanLE),
                I0 = ReadInt32LittleEndian(spanLE.Slice(2)),
                L0 = ReadInt64LittleEndian(spanLE.Slice(6)),
                US0 = ReadUInt16LittleEndian(spanLE.Slice(14)),
                UI0 = ReadUInt32LittleEndian(spanLE.Slice(16)),
                UL0 = ReadUInt64LittleEndian(spanLE.Slice(20)),
                F0 = ReadSingleLittleEndian(spanLE.Slice(28)),
                D0 = ReadDoubleLittleEndian(spanLE.Slice(32)),
                S1 = ReadInt16LittleEndian(spanLE.Slice(40)),
                I1 = ReadInt32LittleEndian(spanLE.Slice(42)),
                L1 = ReadInt64LittleEndian(spanLE.Slice(46)),
                US1 = ReadUInt16LittleEndian(spanLE.Slice(54)),
                UI1 = ReadUInt32LittleEndian(spanLE.Slice(56)),
                UL1 = ReadUInt64LittleEndian(spanLE.Slice(60)),
                F1 = ReadSingleLittleEndian(spanLE.Slice(68)),
                D1 = ReadDoubleLittleEndian(spanLE.Slice(72)),
                H0 = ReadHalfLittleEndian(spanLE.Slice(80)),
                H1 = ReadHalfLittleEndian(spanLE.Slice(82))
            };

            var readStructFromReadOnlySpan = new TestStruct
            {
                S0 = ReadInt16LittleEndian(readOnlySpanLE),
                I0 = ReadInt32LittleEndian(readOnlySpanLE.Slice(2)),
                L0 = ReadInt64LittleEndian(readOnlySpanLE.Slice(6)),
                US0 = ReadUInt16LittleEndian(readOnlySpanLE.Slice(14)),
                UI0 = ReadUInt32LittleEndian(readOnlySpanLE.Slice(16)),
                UL0 = ReadUInt64LittleEndian(readOnlySpanLE.Slice(20)),
                F0 = ReadSingleLittleEndian(readOnlySpanLE.Slice(28)),
                D0 = ReadDoubleLittleEndian(readOnlySpanLE.Slice(32)),
                S1 = ReadInt16LittleEndian(readOnlySpanLE.Slice(40)),
                I1 = ReadInt32LittleEndian(readOnlySpanLE.Slice(42)),
                L1 = ReadInt64LittleEndian(readOnlySpanLE.Slice(46)),
                US1 = ReadUInt16LittleEndian(readOnlySpanLE.Slice(54)),
                UI1 = ReadUInt32LittleEndian(readOnlySpanLE.Slice(56)),
                UL1 = ReadUInt64LittleEndian(readOnlySpanLE.Slice(60)),
                F1 = ReadSingleLittleEndian(readOnlySpanLE.Slice(68)),
                D1 = ReadDoubleLittleEndian(readOnlySpanLE.Slice(72)),
                H0 = ReadHalfLittleEndian(readOnlySpanLE.Slice(80)),
                H1 = ReadHalfLittleEndian(readOnlySpanLE.Slice(82))
            };

            Assert.Equal(s_testStruct, readStruct);
            Assert.Equal(s_testStruct, readStructFromReadOnlySpan);
        }

        [Fact]
        public void ReadingStructFieldByFieldOrReadAndReverseEndianness()
        {
            Span<byte> spanBE = new byte[Unsafe.SizeOf<TestHelpers.TestStructExplicit>()];

            var testExplicitStruct = new TestHelpers.TestStructExplicit
            {
                S0 = short.MaxValue,
                I0 = int.MaxValue,
                L0 = long.MaxValue,
                US0 = ushort.MaxValue,
                UI0 = uint.MaxValue,
                UL0 = ulong.MaxValue,
                S1 = short.MinValue,
                I1 = int.MinValue,
                L1 = long.MinValue,
                US1 = ushort.MinValue,
                UI1 = uint.MinValue,
                UL1 = ulong.MinValue
            };

            WriteInt16BigEndian(spanBE, testExplicitStruct.S0);
            WriteInt32BigEndian(spanBE.Slice(2), testExplicitStruct.I0);
            WriteInt64BigEndian(spanBE.Slice(6), testExplicitStruct.L0);
            WriteUInt16BigEndian(spanBE.Slice(14), testExplicitStruct.US0);
            WriteUInt32BigEndian(spanBE.Slice(16), testExplicitStruct.UI0);
            WriteUInt64BigEndian(spanBE.Slice(20), testExplicitStruct.UL0);
            WriteInt16BigEndian(spanBE.Slice(28), testExplicitStruct.S1);
            WriteInt32BigEndian(spanBE.Slice(30), testExplicitStruct.I1);
            WriteInt64BigEndian(spanBE.Slice(34), testExplicitStruct.L1);
            WriteUInt16BigEndian(spanBE.Slice(42), testExplicitStruct.US1);
            WriteUInt32BigEndian(spanBE.Slice(44), testExplicitStruct.UI1);
            WriteUInt64BigEndian(spanBE.Slice(48), testExplicitStruct.UL1);

            Assert.Equal(56, spanBE.Length);

            ReadOnlySpan<byte> readOnlySpanBE = new ReadOnlySpan<byte>(spanBE.ToArray());

            TestHelpers.TestStructExplicit readStructAndReverse = MemoryMarshal.Read<TestHelpers.TestStructExplicit>(spanBE);
            if (BitConverter.IsLittleEndian)
            {
                readStructAndReverse.S0 = ReverseEndianness(readStructAndReverse.S0);
                readStructAndReverse.I0 = ReverseEndianness(readStructAndReverse.I0);
                readStructAndReverse.L0 = ReverseEndianness(readStructAndReverse.L0);
                readStructAndReverse.US0 = ReverseEndianness(readStructAndReverse.US0);
                readStructAndReverse.UI0 = ReverseEndianness(readStructAndReverse.UI0);
                readStructAndReverse.UL0 = ReverseEndianness(readStructAndReverse.UL0);
                readStructAndReverse.S1 = ReverseEndianness(readStructAndReverse.S1);
                readStructAndReverse.I1 = ReverseEndianness(readStructAndReverse.I1);
                readStructAndReverse.L1 = ReverseEndianness(readStructAndReverse.L1);
                readStructAndReverse.US1 = ReverseEndianness(readStructAndReverse.US1);
                readStructAndReverse.UI1 = ReverseEndianness(readStructAndReverse.UI1);
                readStructAndReverse.UL1 = ReverseEndianness(readStructAndReverse.UL1);
            }

            var readStructFieldByField = new TestHelpers.TestStructExplicit
            {
                S0 = ReadInt16BigEndian(spanBE),
                I0 = ReadInt32BigEndian(spanBE.Slice(2)),
                L0 = ReadInt64BigEndian(spanBE.Slice(6)),
                US0 = ReadUInt16BigEndian(spanBE.Slice(14)),
                UI0 = ReadUInt32BigEndian(spanBE.Slice(16)),
                UL0 = ReadUInt64BigEndian(spanBE.Slice(20)),
                S1 = ReadInt16BigEndian(spanBE.Slice(28)),
                I1 = ReadInt32BigEndian(spanBE.Slice(30)),
                L1 = ReadInt64BigEndian(spanBE.Slice(34)),
                US1 = ReadUInt16BigEndian(spanBE.Slice(42)),
                UI1 = ReadUInt32BigEndian(spanBE.Slice(44)),
                UL1 = ReadUInt64BigEndian(spanBE.Slice(48))
            };

            TestHelpers.TestStructExplicit readStructAndReverseFromReadOnlySpan = MemoryMarshal.Read<TestHelpers.TestStructExplicit>(readOnlySpanBE);
            if (BitConverter.IsLittleEndian)
            {
                readStructAndReverseFromReadOnlySpan.S0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.S0);
                readStructAndReverseFromReadOnlySpan.I0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.I0);
                readStructAndReverseFromReadOnlySpan.L0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.L0);
                readStructAndReverseFromReadOnlySpan.US0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.US0);
                readStructAndReverseFromReadOnlySpan.UI0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.UI0);
                readStructAndReverseFromReadOnlySpan.UL0 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.UL0);
                readStructAndReverseFromReadOnlySpan.S1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.S1);
                readStructAndReverseFromReadOnlySpan.I1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.I1);
                readStructAndReverseFromReadOnlySpan.L1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.L1);
                readStructAndReverseFromReadOnlySpan.US1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.US1);
                readStructAndReverseFromReadOnlySpan.UI1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.UI1);
                readStructAndReverseFromReadOnlySpan.UL1 = ReverseEndianness(readStructAndReverseFromReadOnlySpan.UL1);
            }

            var readStructFieldByFieldFromReadOnlySpan = new TestHelpers.TestStructExplicit
            {
                S0 = ReadInt16BigEndian(readOnlySpanBE),
                I0 = ReadInt32BigEndian(readOnlySpanBE.Slice(2)),
                L0 = ReadInt64BigEndian(readOnlySpanBE.Slice(6)),
                US0 = ReadUInt16BigEndian(readOnlySpanBE.Slice(14)),
                UI0 = ReadUInt32BigEndian(readOnlySpanBE.Slice(16)),
                UL0 = ReadUInt64BigEndian(readOnlySpanBE.Slice(20)),
                S1 = ReadInt16BigEndian(readOnlySpanBE.Slice(28)),
                I1 = ReadInt32BigEndian(readOnlySpanBE.Slice(30)),
                L1 = ReadInt64BigEndian(readOnlySpanBE.Slice(34)),
                US1 = ReadUInt16BigEndian(readOnlySpanBE.Slice(42)),
                UI1 = ReadUInt32BigEndian(readOnlySpanBE.Slice(44)),
                UL1 = ReadUInt64BigEndian(readOnlySpanBE.Slice(48))
            };

            Assert.Equal(testExplicitStruct, readStructAndReverse);
            Assert.Equal(testExplicitStruct, readStructFieldByField);

            Assert.Equal(testExplicitStruct, readStructAndReverseFromReadOnlySpan);
            Assert.Equal(testExplicitStruct, readStructFieldByFieldFromReadOnlySpan);
        }

        private static TestStruct s_testStruct = new TestStruct
        {
            S0 = short.MaxValue,
            I0 = int.MaxValue,
            L0 = long.MaxValue,
            US0 = ushort.MaxValue,
            UI0 = uint.MaxValue,
            UL0 = ulong.MaxValue,
            F0 = float.MaxValue,
            D0 = double.MaxValue,
            S1 = short.MinValue,
            I1 = int.MinValue,
            L1 = long.MinValue,
            US1 = ushort.MinValue,
            UI1 = uint.MinValue,
            UL1 = ulong.MinValue,
            F1 = float.MinValue,
            D1 = double.MinValue,
            H0 = Half.MaxValue,
            H1 = Half.MinValue,
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct TestStruct
        {
            public short S0;
            public int I0;
            public long L0;
            public ushort US0;
            public uint UI0;
            public ulong UL0;
            public float F0;
            public double D0;
            public short S1;
            public int I1;
            public long L1;
            public ushort US1;
            public uint UI1;
            public ulong UL1;
            public float F1;
            public double D1;
            public Half H0;
            public Half H1;
        }
    }
}
