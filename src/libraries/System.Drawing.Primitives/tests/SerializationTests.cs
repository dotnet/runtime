using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Drawing.Primitives.Tests
{
    public class SerializationTests
    {
        public static IEnumerable<object[]> Color_Roundtrip_MemberData()
        {
            yield return new object[] { default(Color) };
            yield return new object[] { Color.FromKnownColor(KnownColor.AliceBlue) };
            yield return new object[] { Color.AliceBlue };
            yield return new object[] { Color.FromArgb(255, 1, 2, 3) };
            yield return new object[] { Color.FromArgb(0, 1, 2, 3) };
            yield return new object[] { Color.FromArgb(1, 2, 3) };
            yield return new object[] { Color.FromName("SomeName") };
        }

        [Theory]
        [MemberData(nameof(Color_Roundtrip_MemberData))]
        public void Color_Roundtrip(Color c)
        {
            Assert.Equal(c, BinaryFormatterHelpers.Clone(c));
        }

        [Fact]
        public void Size_Roundtrip()
        {
            SizeF s = new SizeF(123.4f, 567.8f);
            Assert.Equal(s, BinaryFormatterHelpers.Clone(s));
            Assert.Equal(s.ToSize(), BinaryFormatterHelpers.Clone(s.ToSize()));
        }

        [Fact]
        public void Point_Roundtrip()
        {
            PointF p = new PointF(123.4f, 567.8f);
            Assert.Equal(p, BinaryFormatterHelpers.Clone(p));
            Assert.Equal(Point.Truncate(p), BinaryFormatterHelpers.Clone(Point.Truncate(p)));
        }

        [Fact]
        public void Rectangle_Roundtrip()
        {
            RectangleF r = new RectangleF(1.2f, 3.4f, 5.6f, 7.8f);
            Assert.Equal(r, BinaryFormatterHelpers.Clone(r));
            Assert.Equal(Rectangle.Truncate(r), BinaryFormatterHelpers.Clone(Rectangle.Truncate(r)));
        }
    }
}
