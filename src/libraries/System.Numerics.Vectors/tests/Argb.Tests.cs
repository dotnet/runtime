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
            // Arrange
            Argb<byte> expected = new Argb<byte>(alpha, red, green, blue);
            uint color = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;
            if (BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);

            // Act & Assert
            Assert.Equal(expected, Argb.CreateBigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_CreateLittleEndian(byte alpha, byte red, byte green, byte blue)
        {
            // Arrange
            Argb<byte> expected = new Argb<byte>(alpha, red, green, blue);
            uint color = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;
            if (!BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);

            // Act & Assert
            Assert.Equal(expected, Argb.CreateLittleEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_ToUInt32BigEndian(byte alpha, byte red, byte green, byte blue)
        {
            // Arrange
            uint expected = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;
            if (BitConverter.IsLittleEndian)
                expected = BinaryPrimitives.ReverseEndianness(expected);
            Argb<byte> color = new Argb<byte>(alpha, red, green, blue);

            // Act & Assert
            Assert.Equal(expected, Argb.ToUInt32BigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Argb_ToUInt32LittleEndian(byte alpha, byte red, byte green, byte blue)
        {
            // Arrange
            uint expected = (((uint)alpha << 8 | red) << 8 | green) << 8 | blue;
            if (!BitConverter.IsLittleEndian)
                expected = BinaryPrimitives.ReverseEndianness(expected);
            Argb<byte> color = new Argb<byte>(alpha, red, green, blue);

            // Act & Assert
            Assert.Equal(expected, Argb.ToUInt32LittleEndian(color));
        }
    }
}
