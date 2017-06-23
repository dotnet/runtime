// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.ComponentModel.TypeConverterTests
{
    public class PointConverterTests : StringTypeConverterTestBase<Point>
    {
        protected override TypeConverter Converter { get; } = new PointConverter();
        protected override bool StandardValuesSupported { get; } = false;
        protected override bool StandardValuesExclusive { get; } = false;
        protected override Point Default => new Point(1, 1);
        protected override bool CreateInstanceSupported { get; } = true;
        protected override bool IsGetPropertiesSupported { get; } = true;

        protected override IEnumerable<Tuple<Point, Dictionary<string, object>>> CreateInstancePairs
        {
            get
            {
                yield return Tuple.Create(new Point(10, 20), new Dictionary<string, object>
                {
                    ["X"] = 10,
                    ["Y"] = 20,
                });
                yield return Tuple.Create(new Point(-2, 3), new Dictionary<string, object>
                {
                    ["X"] = -2,
                    ["Y"] = 3,
                });
            }
        }

        [Theory]
        [InlineData(typeof(string))]
        public void CanConvertFromTrue(Type type)
        {
            CanConvertFrom(type);
        }

        [Theory]
        [InlineData(typeof(Rectangle))]
        [InlineData(typeof(RectangleF))]
        [InlineData(typeof(Point))]
        [InlineData(typeof(PointF))]
        [InlineData(typeof(Color))]
        [InlineData(typeof(Size))]
        [InlineData(typeof(SizeF))]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        public void CanConvertFromFalse(Type type)
        {
            CannotConvertFrom(type);
        }

        [Theory]
        [InlineData(typeof(string))]
        public void CanConvertToTrue(Type type)
        {
            CanConvertTo(type);
        }

        [Theory]
        [InlineData(typeof(Rectangle))]
        [InlineData(typeof(RectangleF))]
        [InlineData(typeof(Point))]
        [InlineData(typeof(PointF))]
        [InlineData(typeof(Color))]
        [InlineData(typeof(Size))]
        [InlineData(typeof(SizeF))]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        public void CanConvertToFalse(Type type)
        {
            CannotConvertTo(type);
        }

        public static IEnumerable<object[]> PointData =>
            new[]
            {
                new object[] {0, 0},
                new object[] {1, 1},
                new object[] {-1, 1},
                new object[] {1, -1},
                new object[] {-1, -1},
                new object[] {int.MaxValue, int.MaxValue},
                new object[] {int.MinValue, int.MaxValue},
                new object[] {int.MaxValue, int.MinValue},
                new object[] {int.MinValue, int.MinValue},
            };

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertFrom(int x, int y)
        {
            TestConvertFromString(new Point(x, y), $"{x}, {y}");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1, 1, 1")]
        public void ConvertFrom_ArgumentException(string value)
        {
            ConvertFromThrowsArgumentExceptionForString(value);
        }

        [Fact]
        public void ConvertFrom_Invalid()
        {
            ConvertFromThrowsFormatInnerExceptionForString("*1, 1");
        }

        public static IEnumerable<object[]> ConvertFrom_NotSupportedData =>
            new[]
            {
                new object[] {new Point(1, 1)},
                new object[] {new PointF(1, 1)},
                new object[] {new Size(1, 1)},
                new object[] {new SizeF(1, 1)},
                new object[] {0x10},
            };

        [Theory]
        [MemberData(nameof(ConvertFrom_NotSupportedData))]
        public void ConvertFrom_NotSupported(object value)
        {
            ConvertFromThrowsNotSupportedFor(value);
        }

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertTo(int x, int y)
        {
            TestConvertToString(new Point(x, y), $"{x}, {y}");
        }

        [Theory]
        [InlineData(typeof(Size))]
        [InlineData(typeof(SizeF))]
        [InlineData(typeof(Point))]
        [InlineData(typeof(PointF))]
        [InlineData(typeof(int))]
        public void ConvertTo_NotSupportedException(Type type)
        {
            ConvertToThrowsNotSupportedForType(type);
        }

        [Fact]
        public void ConvertTo_NullCulture()
        {
            string listSep = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            Assert.Equal($"1{listSep} 1", Converter.ConvertTo(null, null, new Point(1, 1), typeof(string)));
        }

        [Fact]
        public void CreateInstance_CaseSensitive()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Converter.CreateInstance(null, new Dictionary<string, object>
                {
                    ["x"] = 1,
                    ["y"] = -1,
                });
            });
        }

        [Fact]
        public void GetProperties()
        {
            var pt = new Point(1, 1);
            var props = Converter.GetProperties(new Point(1, 1));
            Assert.Equal(2, props.Count);
            Assert.Equal(1, props["X"].GetValue(pt));
            Assert.Equal(1, props["Y"].GetValue(pt));

            props = Converter.GetProperties(null, new Point(1, 1));
            Assert.Equal(2, props.Count);
            Assert.Equal(1, props["X"].GetValue(pt));
            Assert.Equal(1, props["Y"].GetValue(pt));

            props = Converter.GetProperties(null, new Point(1, 1), null);
            Assert.Equal(3, props.Count);
            Assert.Equal(1, props["X"].GetValue(pt));
            Assert.Equal(1, props["Y"].GetValue(pt));
            Assert.Equal(false, props["IsEmpty"].GetValue(pt));

            props = Converter.GetProperties(null, new Point(1, 1), new Attribute[0]);
            Assert.Equal(3, props.Count);
            Assert.Equal(1, props["X"].GetValue(pt));
            Assert.Equal(1, props["Y"].GetValue(pt));
            Assert.Equal(false, props["IsEmpty"].GetValue(pt));

            // Pick an attibute that cannot be applied to properties to make sure everything gets filtered
            props = Converter.GetProperties(null, new Point(1, 1), new Attribute[] { new System.Reflection.AssemblyCopyrightAttribute("")});
            Assert.Equal(0, props.Count);
        }

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertFromInvariantString(int x, int y)
        {
            var point = (Point)Converter.ConvertFromInvariantString($"{x}, {y}");
            Assert.Equal(x, point.X);
            Assert.Equal(y, point.Y);
        }

        [Fact]
        public void ConvertFromInvariantString_ArgumentException()
        {
            ConvertFromInvariantStringThrowsArgumentException("1");
        }

        [Fact]
        public void ConvertFromInvariantString_FormatException()
        {
            ConvertFromInvariantStringThrowsFormatInnerException("hello");
        }

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertFromString(int x, int y)
        {
            var point =
                (Point)Converter.ConvertFromString(string.Format("{0}{2} {1}", x, y,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator));
            Assert.Equal(x, point.X);
            Assert.Equal(y, point.Y);
        }

        [Fact]
        public void ConvertFromString_ArgumentException()
        {
            ConvertFromStringThrowsArgumentException("1");
        }

        [Fact]
        public void ConvertFromString_FormatException()
        {
            ConvertFromStringThrowsFormatInnerException("hello");
        }

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertToInvariantString(int x, int y)
        {
            var str = Converter.ConvertToInvariantString(new Point(x, y));
            Assert.Equal($"{x}, {y}", str);
        }

        [Theory]
        [MemberData(nameof(PointData))]
        public void ConvertToString(int x, int y)
        {
            var str = Converter.ConvertToString(new Point(x, y));
            Assert.Equal(string.Format("{0}{2} {1}", x, y, CultureInfo.CurrentCulture.TextInfo.ListSeparator), str);
        }
    }
}
