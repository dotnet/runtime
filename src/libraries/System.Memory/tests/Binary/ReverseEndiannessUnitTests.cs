// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Binary.Tests
{
    public class ReverseEndianessUnitTests
    {
        [Fact]
        public void ReverseEndianness_ByteAndSByte_DoesNothing()
        {
            byte valueMax = byte.MaxValue;
            byte valueMin = byte.MinValue;
            sbyte signedValueMax = sbyte.MaxValue;
            sbyte signedValueMin = sbyte.MinValue;

            Assert.Equal(valueMax, BinaryPrimitives.ReverseEndianness(valueMax));
            Assert.Equal(valueMin, BinaryPrimitives.ReverseEndianness(valueMin));
            Assert.Equal(signedValueMax, BinaryPrimitives.ReverseEndianness(signedValueMax));
            Assert.Equal(signedValueMin, BinaryPrimitives.ReverseEndianness(signedValueMin));
        }

        [Theory]
        [InlineData(0x0123, 0x2301)]
        [InlineData(0xABCD, 0xCDAB)]
        public void ReverseEndianness_Int16AndUInt16(ushort a, ushort b)
        {
            Assert.Equal((short)b, BinaryPrimitives.ReverseEndianness((short)a));
            Assert.Equal((short)a, BinaryPrimitives.ReverseEndianness((short)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
        }

        [Theory]
        [InlineData(0x01234567, 0x67452301)]
        [InlineData(0xABCDEF01, 0x01EFCDAB)]
        public void ReverseEndianness_Int32AndUInt32(uint a, uint b)
        {
            Assert.Equal((int)b, BinaryPrimitives.ReverseEndianness((int)a));
            Assert.Equal((int)a, BinaryPrimitives.ReverseEndianness((int)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
        }

        [Theory]
        [InlineData(0x0123456789ABCDEF, 0xEFCDAB8967452301)]
        [InlineData(0xABCDEF0123456789, 0x8967452301EFCDAB)]
        public void ReverseEndianness_Int64AndUInt64(ulong a, ulong b)
        {
            Assert.Equal((long)b, BinaryPrimitives.ReverseEndianness((long)a));
            Assert.Equal((long)a, BinaryPrimitives.ReverseEndianness((long)b));
            Assert.Equal(b, BinaryPrimitives.ReverseEndianness(a));
            Assert.Equal(a, BinaryPrimitives.ReverseEndianness(b));
        }
    }
}
