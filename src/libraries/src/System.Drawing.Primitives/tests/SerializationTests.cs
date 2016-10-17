using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static IEnumerable<object[]> Size_Roundtrip_MemberData()
        {
            yield return new object[] { new SizeF(123.4f, 567.8f) };
        }

        public static IEnumerable<object[]> Point_Roundtrip_MemberData()
        {
            yield return new object[] { new PointF(123.4f, 567.8f) };
        }

        public static IEnumerable<object[]> Rectangle_Roundtrip_MemberData()
        {
            yield return new object[] { new RectangleF(1.2f, 3.4f, 5.6f, 7.8f) };
        }

        [Theory]
        [MemberData(nameof(Color_Roundtrip_MemberData))]
        public void Color_Roundtrip(Color c) => Assert.Equal(c, BinaryFormatterHelpers.Clone(c));

        [Theory]
        [MemberData(nameof(Size_Roundtrip_MemberData))]
        public void Size_Roundtrip(SizeF s)
        {
            Assert.Equal(s, BinaryFormatterHelpers.Clone(s));
            Assert.Equal(s.ToSize(), BinaryFormatterHelpers.Clone(s.ToSize()));
        }

        [Theory]
        [MemberData(nameof(Point_Roundtrip_MemberData))]
        public void Point_Roundtrip(PointF p)
        {
            Assert.Equal(p, BinaryFormatterHelpers.Clone(p));
            Assert.Equal(Point.Truncate(p), BinaryFormatterHelpers.Clone(Point.Truncate(p)));
        }

        [Theory]
        [MemberData(nameof(Rectangle_Roundtrip_MemberData))]
        public void Rectangle_Roundtrip(RectangleF r)
        {
            Assert.Equal(r, BinaryFormatterHelpers.Clone(r));
            Assert.Equal(Rectangle.Truncate(r), BinaryFormatterHelpers.Clone(Rectangle.Truncate(r)));
        }
    }
}
