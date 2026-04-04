// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Xunit;

namespace System.Numerics.Colors.Tests
{
    public class ArgbTests
    {
        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_CreateBigEndian(byte alpha, byte red, byte green, byte blue)
        {
            Argb<byte> expected = new Argb<byte>(alpha, red, green, blue);
            uint color = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;

            Assert.Equal(expected, Argb.CreateBigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_CreateLittleEndian(byte alpha, byte red, byte green, byte blue)
        {
            Argb<byte> expected = new Argb<byte>(alpha, red, green, blue);
            uint color = BinaryPrimitives.ReverseEndianness((((uint)alpha << 8 | red) << 8 | green) << 8 | blue);

            Assert.Equal(expected, Argb.CreateLittleEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_ToUInt32BigEndian(byte alpha, byte red, byte green, byte blue)
        {
            uint expected = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;
            Argb<byte> color = new Argb<byte>(alpha, red, green, blue);

            Assert.Equal(expected, Argb.ToUInt32BigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_ToUInt32LittleEndian(byte alpha, byte red, byte green, byte blue)
        {
            uint expected = BinaryPrimitives.ReverseEndianness((((uint)alpha << 8 | red) << 8 | green) << 8 | blue);
            Argb<byte> color = new Argb<byte>(alpha, red, green, blue);

            Assert.Equal(expected, Argb.ToUInt32LittleEndian(color));
        }
    }
}
