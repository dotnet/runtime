// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class BitConverterSpan : BitConverterBase
    {
        [Fact]
        public void TryWriteBytes_DestinationSpanNotLargeEnough()
        {
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, false));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, 'a'));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (short)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, 2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (long)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (ushort)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (uint)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (ulong)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, (float)2));
            Assert.False(BitConverter.TryWriteBytes(Span<byte>.Empty, 2.0));
        }

        [Fact]
        public void ToMethods_DestinationSpanNotLargeEnough()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToChar(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToInt16(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToInt32(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToInt64(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToUInt16(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToUInt32(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToUInt64(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToSingle(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToDouble(Span<byte>.Empty); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { BitConverter.ToBoolean(Span<byte>.Empty); });
        }

        public override void ConvertFromBool(bool boolean, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[1]);
            Assert.True(BitConverter.TryWriteBytes(span, boolean));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromShort(short num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[2]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromChar(char character, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[2]);
            Assert.True(BitConverter.TryWriteBytes(span, character));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromInt(int num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[4]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromLong(long num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[8]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromUShort(ushort num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[2]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromUInt(uint num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[4]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromULong(ulong num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[8]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromFloat(float num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[4]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ConvertFromDouble(double num, byte[] expected)
        {
            Span<byte> span = new Span<byte>(new byte[8]);
            Assert.True(BitConverter.TryWriteBytes(span, num));
            Assert.Equal(expected, span.ToArray());
        }

        public override void ToChar(int index, char expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            BitConverter.ToChar(span);
            Assert.Equal(expected, BitConverter.ToChar(span.Slice(index)));
        }

        public override void ToInt16(int index, short expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToInt16(span.Slice(index)));
        }

        public override void ToInt32(int expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToInt32(byteArray));
        }

        public override void ToInt64(int index, long expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToInt64(span.Slice(index)));
        }

        public override void ToUInt16(int index, ushort expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToUInt16(span.Slice(index)));
        }

        public override void ToUInt32(int index, uint expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToUInt32(span.Slice(index)));
        }

        public override void ToUInt64(int index, ulong expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToUInt64(span.Slice(index)));
        }

        public override void ToSingle(int index, float expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToSingle(span.Slice(index)));
        }

        public override void ToDouble(int index, double expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToDouble(span.Slice(index)));
        }

        public override void ToBoolean(int index, bool expected, byte[] byteArray)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(byteArray);
            Assert.Equal(expected, BitConverter.ToBoolean(span.Slice(index)));
        }
    }
}
