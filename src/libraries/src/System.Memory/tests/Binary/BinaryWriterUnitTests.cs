// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Binary.Tests
{
    public class BinaryWriterUnitTests
    {
        [Fact]
        public void SpanWrite()
        {
            Assert.True(BitConverter.IsLittleEndian);

            Span<byte> span = new byte[8];

            byte byteValue = 0x11;
            WriteMachineEndian<byte>(span, ref byteValue);
            TestHelpers.Validate<byte>(span, byteValue);
            Assert.True(TryWriteMachineEndian<byte>(span, ref byteValue));
            TestHelpers.Validate<byte>(span, byteValue);

            sbyte sbyteValue = 0x11;
            WriteMachineEndian<sbyte>(span, ref sbyteValue);
            TestHelpers.Validate<sbyte>(span, sbyteValue);
            Assert.True(TryWriteMachineEndian<sbyte>(span, ref sbyteValue));
            TestHelpers.Validate<sbyte>(span, sbyteValue);

            ushort ushortValue = 0x1122;
            WriteMachineEndian<ushort>(span, ref ushortValue);
            TestHelpers.Validate<ushort>(span, ushortValue);
            Assert.True(TryWriteMachineEndian<ushort>(span, ref ushortValue));
            TestHelpers.Validate<ushort>(span, ushortValue);

            uint uintValue = 0x11223344;
            WriteMachineEndian<uint>(span, ref uintValue);
            TestHelpers.Validate<uint>(span, uintValue);
            Assert.True(TryWriteMachineEndian<uint>(span, ref uintValue));
            TestHelpers.Validate<uint>(span, uintValue);

            ulong ulongValue = 0x1122334455667788;
            WriteMachineEndian<ulong>(span, ref ulongValue);
            TestHelpers.Validate<ulong>(span, ulongValue);
            Assert.True(TryWriteMachineEndian<ulong>(span, ref ulongValue));
            TestHelpers.Validate<ulong>(span, ulongValue);

            short shortValue = 0x1122;
            WriteMachineEndian<short>(span, ref shortValue);
            TestHelpers.Validate<short>(span, shortValue);
            Assert.True(TryWriteMachineEndian<short>(span, ref shortValue));
            TestHelpers.Validate<short>(span, shortValue);

            int intValue = 0x11223344;
            WriteMachineEndian<int>(span, ref intValue);
            TestHelpers.Validate<int>(span, intValue);
            Assert.True(TryWriteMachineEndian<int>(span, ref intValue));
            TestHelpers.Validate<int>(span, intValue);

            long longValue = 0x1122334455667788;
            WriteMachineEndian<long>(span, ref longValue);
            TestHelpers.Validate<long>(span, longValue);
            Assert.True(TryWriteMachineEndian<long>(span, ref longValue));
            TestHelpers.Validate<long>(span, longValue);
        }

        [Theory]
        [InlineData(short.MaxValue)]
        [InlineData(short.MinValue)]
        [InlineData(0x7F00)]
        [InlineData(0x00FF)]
        public void SpanWriteInt16(short value)
        {
            Assert.True(BitConverter.IsLittleEndian);
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
            Assert.True(BitConverter.IsLittleEndian);
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
            Assert.True(BitConverter.IsLittleEndian);
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
            Assert.True(BitConverter.IsLittleEndian);
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
            Assert.True(BitConverter.IsLittleEndian);
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
            Assert.True(BitConverter.IsLittleEndian);
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

            Span<byte> span = new byte[1];

            WriteMachineEndian<byte>(span, ref byteValue);
            byte read = ReadMachineEndian<byte>(span);
            Assert.Equal<byte>(byteValue, read);

            span.Clear();
            Assert.True(TryWriteMachineEndian<byte>(span, ref byteValue));
            read = ReadMachineEndian<byte>(span);
            Assert.Equal<byte>(byteValue, read);

            WriteMachineEndian<sbyte>(span, ref sbyteValue);
            sbyte readSbyte = ReadMachineEndian<sbyte>(span);
            Assert.Equal<sbyte>(sbyteValue, readSbyte);

            span.Clear();
            Assert.True(TryWriteMachineEndian<sbyte>(span, ref sbyteValue));
            readSbyte = ReadMachineEndian<sbyte>(span);
            Assert.Equal<sbyte>(sbyteValue, readSbyte);

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<short>(_span, ref shortValue));
            Assert.False(TryWriteMachineEndian<short>(span, ref shortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<int>(_span, ref intValue));
            Assert.False(TryWriteMachineEndian<int>(span, ref intValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<long>(_span, ref longValue));
            Assert.False(TryWriteMachineEndian<long>(span, ref longValue));

            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<ushort>(_span, ref ushortValue));
            Assert.False(TryWriteMachineEndian<ushort>(span, ref ushortValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<uint>(_span, ref uintValue));
            Assert.False(TryWriteMachineEndian<uint>(span, ref uintValue));
            TestHelpers.AssertThrows<ArgumentOutOfRangeException, byte>(span, (_span) => WriteMachineEndian<ulong>(_span, ref ulongValue));
            Assert.False(TryWriteMachineEndian<ulong>(span, ref ulongValue));

            var structValue = new TestHelpers.TestValueTypeWithReference { I = 1, S = "1" };
            TestHelpers.AssertThrows<ArgumentException, byte>(span, (_span) => WriteMachineEndian<TestHelpers.TestValueTypeWithReference>(_span, ref structValue));
            TestHelpers.AssertThrows<ArgumentException, byte>(span, (_span) => TryWriteMachineEndian<TestHelpers.TestValueTypeWithReference>(_span, ref structValue));
        }
    }
}
