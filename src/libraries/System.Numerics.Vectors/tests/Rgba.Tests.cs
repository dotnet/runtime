// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Xunit;

namespace System.Numerics.Colors.Tests
{
    public class RgbaTests
    {
        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_CreateBigEndian(byte red, byte green, byte blue, byte alpha)
        {
            Rgba<byte> expected = new Rgba<byte>(red, green, blue, alpha);
            uint color = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;

            Assert.Equal(expected, Rgba.CreateBigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_CreateLittleEndian(byte red, byte green, byte blue, byte alpha)
        {
            Rgba<byte> expected = new Rgba<byte>(red, green, blue, alpha);
            uint color = BinaryPrimitives.ReverseEndianness((((uint)red << 8 | green) << 8 | blue) << 8 | alpha);

            Assert.Equal(expected, Rgba.CreateLittleEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_ToUInt32BigEndian(byte red, byte green, byte blue, byte alpha)
        {
            uint expected = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;
            Rgba<byte> color = new Rgba<byte>(red, green, blue, alpha);

            Assert.Equal(expected, Rgba.ToUInt32BigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_ToUInt32LittleEndian(byte red, byte green, byte blue, byte alpha)
        {
            uint expected = BinaryPrimitives.ReverseEndianness((((uint)red << 8 | green) << 8 | blue) << 8 | alpha);
            Rgba<byte> color = new Rgba<byte>(red, green, blue, alpha);

            Assert.Equal(expected, Rgba.ToUInt32LittleEndian(color));
        }
    }
}
