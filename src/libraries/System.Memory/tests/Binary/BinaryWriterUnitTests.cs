// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Binary.Tests
{
    public class BinaryWriterUnitTests
    {
        [Fact]
        public void SpanWrite()
        {
            Span<byte> span = new byte[8];

            byte byteValue = 0x11;
            MemoryMarshal.Write<byte>(span, ref byteValue);
            TestHelpers.Validate<byte>(span, byteValue);
            Assert.True(MemoryMarshal.TryWrite<byte>(span, ref byteValue));
            TestHelpers.Validate<byte>(span, byteValue);

            sbyte sbyteValue = 0x11;
            MemoryMarshal.Write<sbyte>(span, ref sbyteValue);
            TestHelpers.Validate<sbyte>(span, sbyteValue);
            Assert.True(MemoryMarshal.TryWrite<sbyte>(span, ref sbyteValue));
            TestHelpers.Validate<sbyte>(span, sbyteValue);

            ushort ushortValue = 0x1122;
            MemoryMarshal.Write<ushort>(span, ref ushortValue);
            TestHelpers.Validate<ushort>(span, ushortValue);
            Assert.True(MemoryMarshal.TryWrite<ushort>(span, ref ushortValue));
            TestHelpers.Validate<ushort>(span, ushortValue);

            uint uintValue = 0x11223344;
            MemoryMarshal.Write<uint>(span, ref uintValue);
            TestHelpers.Validate<uint>(span, uintValue);
            Assert.True(MemoryMarshal.TryWrite<uint>(span, ref uintValue));
            TestHelpers.Validate<uint>(span, uintValue);

            ulong ulongValue = 0x1122334455667788;
            MemoryMarshal.Write<ulong>(span, ref ulongValue);
            TestHelpers.Validate<ulong>(span, ulongValue);
            Assert.True(MemoryMarshal.TryWrite<ulong>(span, ref ulongValue));
            TestHelpers.Validate<ulong>(span, ulongValue);

            short shortValue = 0x1122;
            MemoryMarshal.Write<short>(span, ref shortValue);
            TestHelpers.Validate<short>(span, shortValue);
            Assert.True(MemoryMarshal.TryWrite<short>(span, ref shortValue));
            TestHelpers.Validate<short>(span, shortValue);

            int intValue = 0x11223344;
            MemoryMarshal.Write<int>(span, ref intValue);
            TestHelpers.Validate<int>(span, intValue);
            Assert.True(MemoryMarshal.TryWrite<int>(span, ref intValue));
            TestHelpers.Validate<int>(span, intValue);

            long longValue = 0x1122334455667788;
            MemoryMarshal.Write<long>(span, ref longValue);
            TestHelpers.Validate<long>(span, longValue);
            Assert.True(MemoryMarshal.TryWrite<long>(span, ref longValue));
            TestHelpers.Validate<long>(span, longValue);

            Half halfValue = BitConverter.Int16BitsToHalf(0x1122);
            MemoryMarshal.Write<Half>(span, ref halfValue);
            TestHelpers.Validate<Half>(span, halfValue);
            Assert.True(MemoryMarshal.TryWrite<Half>(span, ref halfValue));
            TestHelpers.Validate<Half>(span, halfValue);

            float floatValue = BitConverter.Int32BitsToSingle(0x11223344);
            MemoryMarshal.Write<float>(span, ref floatValue);
            TestHelpers.Validate<float>(span, floatValue);
            Assert.True(MemoryMarshal.TryWrite<float>(span, ref floatValue));
            TestHelpers.Validate<float>(span, floatValue);

            double doubleValue = BitConverter.Int64BitsToDouble(0x1122334455667788);
            MemoryMarshal.Write<double>(span, ref doubleValue);
            TestHelpers.Validate<double>(span, doubleValue);
            Assert.True(MemoryMarshal.TryWrite<double>(span, ref doubleValue));
            TestHelpers.Validate<double>(span, doubleValue);
        }

        [Theory]
        [InlineData(short.MaxValue)]
        [InlineData(short.MinValue)]
        [InlineData(0x7F00)]
        [InlineData(0x00FF)]
        public void SpanWriteInt16(short value)
        {
            var span = new Span<byte>(new byte[2]);
            WriteInt16BigEndian(span, value);
            short read = ReadInt16BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt16BigEndian(span, value));
            read = ReadInt16BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteInt16LittleEndian(span, value);
            read = ReadInt16LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt16LittleEndian(span, value));
            read = ReadInt16LittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(ushort.MaxValue)]
        [InlineData(ushort.MinValue)]
        [InlineData(0xFF00)]
        [InlineData(0x00FF)]
        public void SpanWriteUInt16(ushort value)
        {
            var span = new Span<byte>(new byte[2]);
            WriteUInt16BigEndian(span, value);
            ushort read = ReadUInt16BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt16BigEndian(span, value));
            read = ReadUInt16BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteUInt16LittleEndian(span, value);
            read = ReadUInt16LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt16LittleEndian(span, value));
            read = ReadUInt16LittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0x7F000000)]
        [InlineData(0x00FF0000)]
        [InlineData(0x0000FF00)]
        [InlineData(0x000000FF)]
        public void SpanWriteInt32(int value)
        {
            var span = new Span<byte>(new byte[4]);
            WriteInt32BigEndian(span, value);
            int read = ReadInt32BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt32BigEndian(span, value));
            read = ReadInt32BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteInt32LittleEndian(span, value);
            read = ReadInt32LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt32LittleEndian(span, value));
            read = ReadInt32LittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(uint.MaxValue)]
        [InlineData(uint.MinValue)]
        [InlineData(0xFF000000)]
        [InlineData(0x00FF0000)]
        [InlineData(0x0000FF00)]
        [InlineData(0x000000FF)]
        public void SpanWriteUInt32(uint value)
        {
            var span = new Span<byte>(new byte[4]);
            WriteUInt32BigEndian(span, value);
            uint read = ReadUInt32BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt32BigEndian(span, value));
            read = ReadUInt32BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteUInt32LittleEndian(span, value);
            read = ReadUInt32LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt32LittleEndian(span, value));
            read = ReadUInt32LittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        [InlineData(0x7F00000000000000)]
        [InlineData(0x00FF000000000000)]
        [InlineData(0x0000FF0000000000)]
        [InlineData(0x000000FF00000000)]
        [InlineData(0x00000000FF000000)]
        [InlineData(0x0000000000FF0000)]
        [InlineData(0x000000000000FF00)]
        [InlineData(0x00000000000000FF)]
        public void SpanWriteInt64(long value)
        {
            var span = new Span<byte>(new byte[8]);
            WriteInt64BigEndian(span, value);
            long read = ReadInt64BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt64BigEndian(span, value));
            read = ReadInt64BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteInt64LittleEndian(span, value);
            read = ReadInt64LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteInt64LittleEndian(span, value));
            read = ReadInt64LittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(ulong.MaxValue)]
        [InlineData(ulong.MinValue)]
        [InlineData(0xFF00000000000000)]
        [InlineData(0x00FF000000000000)]
        [InlineData(0x0000FF0000000000)]
        [InlineData(0x000000FF00000000)]
        [InlineData(0x00000000FF000000)]
        [InlineData(0x0000000000FF0000)]
        [InlineData(0x000000000000FF00)]
        [InlineData(0x00000000000000FF)]
        public void SpanWriteUInt64(ulong value)
        {
            var span = new Span<byte>(new byte[8]);
            WriteUInt64BigEndian(span, value);
            ulong read = ReadUInt64BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt64BigEndian(span, value));
            read = ReadUInt64BigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteUInt64LittleEndian(span, value);
            read = ReadUInt64LittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteUInt64LittleEndian(span, value));
            read = ReadUInt64LittleEndian(span);
            Assert.Equal(value, read);
        }

        // Half cannot be used as constants in InlineData
        public static IEnumerable<object[]> SpanWriteHalf_TestData()
        {
            yield return new object[] { Half.MaxValue };
            yield return new object[] { Half.MinValue };
            yield return new object[] { Half.Epsilon };
            yield return new object[] { Half.PositiveInfinity };
            yield return new object[] { Half.NegativeInfinity };
            yield return new object[] { Half.NaN };
        }

        [Theory]
        [MemberData(nameof(SpanWriteHalf_TestData))]
        public void SpanWriteHalf(Half value)
        {
            var span = new Span<byte>(new byte[4]);
            WriteHalfBigEndian(span, value);
            Half read = ReadHalfBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteHalfBigEndian(span, value));
            read = ReadHalfBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteHalfLittleEndian(span, value);
            read = ReadHalfLittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteHalfLittleEndian(span, value));
            read = ReadHalfLittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(float.Epsilon)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.NaN)]
        public void SpanWriteSingle(float value)
        {
            var span = new Span<byte>(new byte[4]);
            WriteSingleBigEndian(span, value);
            float read = ReadSingleBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteSingleBigEndian(span, value));
            read = ReadSingleBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteSingleLittleEndian(span, value);
            read = ReadSingleLittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteSingleLittleEndian(span, value));
            read = ReadSingleLittleEndian(span);
            Assert.Equal(value, read);
        }

        [Theory]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        [InlineData(double.Epsilon)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.NaN)]
        public void SpanWriteDouble(double value)
        {
            var span = new Span<byte>(new byte[8]);
            WriteDoubleBigEndian(span, value);
            double read = ReadDoubleBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteDoubleBigEndian(span, value));
            read = ReadDoubleBigEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            WriteDoubleLittleEndian(span, value);
            read = ReadDoubleLittleEndian(span);
            Assert.Equal(value, read);

            span.Clear();
            Assert.True(TryWriteDoubleLittleEndian(span, value));
            read = ReadDoubleLittleEndian(span);
            Assert.Equal(value, read);
        }

        [Fact]
        public void SpanWriteFail()
        {
            byte byteValue = 1;
            sbyte sbyteValue = 1;
            short shortValue = 1;
            ushort ushortValue = 1;
            int intValue = 1;
            uint uintValue = 1;
            long longValue = 1;
            ulong ulongValue = 1;
            Half halfValue = (Half)1;
            float floatValue = 1;
            double doubleValue = 1;

            Span<byte> span = new byte[1];

            MemoryMarshal.Write<byte>(span, ref byteValue);
            byte read = MemoryMarshal.Read<byte>(span);
            Assert.Equal<byte>(byteValue, read);

            span.Clear();
            Assert.True(MemoryMarshal.TryWrite<byte>(span, ref byteValue));
            read = MemoryMarshal.Read<byte>(span);
            Assert.Equal<byte>(byteValue, read);

            MemoryMarshal.Write<sbyte>(span, ref sbyteValue);
            sbyte readSbyte = MemoryMarshal.Read<sbyte>(span);
            Assert.Equal<sbyte>(sbyteValue, readSbyte);

            span.Clear();
            Assert.True(MemoryMarshal.TryWrite<sbyte>(span, ref sbyteValue));
            readSbyte = MemoryMarshal.Read<sbyte>(span);
            Assert.Equal<sbyte>(sbyteValue, readSbyte);

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<short>(_span, ref shortValue));
            Assert.False(MemoryMarshal.TryWrite<short>(span, ref shortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<int>(_span, ref intValue));
            Assert.False(MemoryMarshal.TryWrite<int>(span, ref intValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<long>(_span, ref longValue));
            Assert.False(MemoryMarshal.TryWrite<long>(span, ref longValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<ushort>(_span, ref ushortValue));
            Assert.False(MemoryMarshal.TryWrite<ushort>(span, ref ushortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<uint>(_span, ref uintValue));
            Assert.False(MemoryMarshal.TryWrite<uint>(span, ref uintValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<ulong>(_span, ref ulongValue));
            Assert.False(MemoryMarshal.TryWrite<ulong>(span, ref ulongValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<Half>(_span, ref halfValue));
            Assert.False(MemoryMarshal.TryWrite<Half>(span, ref halfValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<float>(_span, ref floatValue));
            Assert.False(MemoryMarshal.TryWrite<float>(span, ref floatValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => MemoryMarshal.Write<double>(_span, ref doubleValue));
            Assert.False(MemoryMarshal.TryWrite<double>(span, ref doubleValue));

            var structValue = new TestHelpers.TestValueTypeWithReference { I = 1, S = "1" };
            TestHelpers.AssertThrows<ArgumentException, byte>(span, (_span) => MemoryMarshal.Write<TestHelpers.TestValueTypeWithReference>(_span, ref structValue));
            TestHelpers.AssertThrows<ArgumentException, byte>(span, (_span) => MemoryMarshal.TryWrite<TestHelpers.TestValueTypeWithReference>(_span, ref structValue));
        }
    }
}
