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
            // Arrange
            Rgba<byte> expected = new Rgba<byte>(red, green, blue, alpha);
            uint color = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;
            if (BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);

            // Act & Assert
            Assert.Equal(expected, Rgba.CreateBigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_CreateLittleEndian(byte red, byte green, byte blue, byte alpha)
        {
            // Arrange
            Rgba<byte> expected = new Rgba<byte>(red, green, blue, alpha);
            uint color = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;
            if (!BitConverter.IsLittleEndian)
                color = BinaryPrimitives.ReverseEndianness(color);

            // Act & Assert
            Assert.Equal(expected, Rgba.CreateLittleEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_ToUInt32BigEndian(byte red, byte green, byte blue, byte alpha)
        {
            // Arrange
            uint expected = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;
            if (BitConverter.IsLittleEndian)
                expected = BinaryPrimitives.ReverseEndianness(expected);
            Rgba<byte> color = new Rgba<byte>(red, green, blue, alpha);

            // Act & Assert
            Assert.Equal(expected, Rgba.ToUInt32BigEndian(color));
        }

        [Theory]
        [MemberData(nameof(TestHelpers.ByteColors), MemberType = typeof(TestHelpers))]
        public void Rgba_ToUInt32LittleEndian(byte red, byte green, byte blue, byte alpha)
        {
            // Arrange
            uint expected = (((uint)red << 8 | green) << 8 | blue) << 8 | alpha;
            if (!BitConverter.IsLittleEndian)
                expected = BinaryPrimitives.ReverseEndianness(expected);
            Rgba<byte> color = new Rgba<byte>(red, green, blue, alpha);

            // Act & Assert
            Assert.Equal(expected, Rgba.ToUInt32LittleEndian(color));
        }
    }
}
