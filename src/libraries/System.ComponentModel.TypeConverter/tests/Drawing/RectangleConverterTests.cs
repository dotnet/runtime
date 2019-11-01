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
    public class RectangleConverterTests : StringTypeConverterTestBase<Rectangle>
    {
        protected override TypeConverter Converter { get; } = new RectangleConverter();
        protected override bool StandardValuesSupported { get; } = false;
        protected override bool StandardValuesExclusive { get; } = false;
        protected override Rectangle Default => new Rectangle(0, 0, 100, 100);
        protected override bool CreateInstanceSupported { get; } = true;
        protected override bool IsGetPropertiesSupported { get; } = true;

        protected override IEnumerable<Tuple<Rectangle, Dictionary<string, object>>> CreateInstancePairs {
            get
            {
                yield return Tuple.Create(new Rectangle(10, 10, 20, 30), new Dictionary<string, object>
                {
                    ["X"] = 10,
                    ["Y"] = 10,
                    ["Width"] = 20,
                    ["Height"] = 30,
                });
                yield return Tuple.Create(new Rectangle(-10, -10, 20, 30), new Dictionary<string, object>
                {
                    ["X"] = -10,
                    ["Y"] = -10,
                    ["Width"] = 20,
                    ["Height"] = 30,
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

        public static IEnumerable<object[]> RectangleData =>
            new[]
            {
                new object[] {0, 0, 0, 0},
                new object[] {1, 1, 1, 1},
                new object[] {-1, 1, 1, 1},
                new object[] {1, -1, 1, 1},
                new object[] {-1, -1, 1, 1},
                new object[] {int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue},
                new object[] {int.MinValue, int.MaxValue, int.MaxValue, int.MaxValue},
                new object[] {int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue},
                new object[] {int.MinValue, int.MinValue, int.MaxValue, int.MaxValue},
            };

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertFrom(int x, int y, int width, int height)
        {
            TestConvertFromString(new Rectangle(x, y, width, height), $"{x}, {y}, {width}, {height}");
        }

        [Theory]
        [InlineData("10, 10")]
        [InlineData("1, 1, 1, 1, 1")]
        public void ConvertFrom_ArgumentException(string value)
        {
            ConvertFromThrowsArgumentExceptionForString(value);
        }

        [Fact]
        public void ConvertFrom_Invalid()
        {
            ConvertFromThrowsFormatInnerExceptionForString("*1, 1, 1, 1");
        }

        public static IEnumerable<object[]> ConvertFrom_NotSupportedData =>
            new[]
            {
                new object[] {new Point(10, 10)},
                new object[] {new PointF(10, 10)},
                new object[] {new Size(10, 10)},
                new object[] {new SizeF(10, 10)},
                new object[] {new object()},
                new object[] {1001},
            };

        [Theory]
        [MemberData(nameof(ConvertFrom_NotSupportedData))]
        public void ConvertFrom_NotSupported(object value)
        {
            ConvertFromThrowsNotSupportedFor(value);
        }

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertTo(int x, int y, int width, int height)
        {
            TestConvertToString(new Rectangle(x, y, width, height), $"{x}, {y}, {width}, {height}");
        }

        [Theory]
        [InlineData(typeof(Size))]
        [InlineData(typeof(SizeF))]
        [InlineData(typeof(Point))]
        [InlineData(typeof(PointF))]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        public void ConvertTo_NotSupportedException(Type type)
        {
            ConvertToThrowsNotSupportedForType(type);
        }

        [Fact]
        public void CreateInstance_CaseSensitive()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Converter.CreateInstance(null, new Dictionary<string, object>
                {
                    ["x"] = -10,
                    ["Y"] = -10,
                    ["Width"] = 20,
                    ["Height"] = 30,
                });
            });
        }

        [Fact]
        public void TestGetProperties()
        {
            var rect = new Rectangle(10, 10, 20, 30);
            var propsColl = Converter.GetProperties(rect);
            Assert.Equal(4, propsColl.Count);
            Assert.Equal(rect.X, propsColl["X"].GetValue(rect));
            Assert.Equal(rect.Y, propsColl["Y"].GetValue(rect));
            Assert.Equal(rect.Width, propsColl["Width"].GetValue(rect));
            Assert.Equal(rect.Height, propsColl["Height"].GetValue(rect));

            rect = new Rectangle(-10, -10, 20, 30);
            propsColl = Converter.GetProperties(null, rect);
            Assert.Equal(4, propsColl.Count);
            Assert.Equal(rect.X, propsColl["X"].GetValue(rect));
            Assert.Equal(rect.Y, propsColl["Y"].GetValue(rect));
            Assert.Equal(rect.Width, propsColl["Width"].GetValue(rect));
            Assert.Equal(rect.Height, propsColl["Height"].GetValue(rect));

            rect = new Rectangle(10, 10, 20, 30);
            propsColl = Converter.GetProperties(null, rect, null);
            Assert.Equal(11, propsColl.Count);
            Assert.Equal(rect.X, propsColl["X"].GetValue(rect));
            Assert.Equal(rect.Y, propsColl["Y"].GetValue(rect));
            Assert.Equal(rect.Width, propsColl["Width"].GetValue(rect));
            Assert.Equal(rect.Height, propsColl["Height"].GetValue(rect));
            Assert.Equal(rect.IsEmpty, propsColl["IsEmpty"].GetValue(rect));

            Assert.Equal(rect.Top, propsColl["Top"].GetValue(rect));
            Assert.Equal(rect.Bottom, propsColl["Bottom"].GetValue(rect));
            Assert.Equal(rect.Left, propsColl["Left"].GetValue(rect));
            Assert.Equal(rect.Right, propsColl["Right"].GetValue(rect));
            Assert.Equal(rect.Location, propsColl["Location"].GetValue(rect));
            Assert.Equal(rect.Size, propsColl["Size"].GetValue(rect));
            Assert.Equal(rect.IsEmpty, propsColl["IsEmpty"].GetValue(rect));

            // Pick an attibute that cannot be applied to properties to make sure everything gets filtered
            propsColl = Converter.GetProperties(null, new Rectangle(10, 10, 20, 30), new Attribute[] { new System.Reflection.AssemblyCopyrightAttribute("")});
            Assert.Equal(0, propsColl.Count);
        }

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertFromInvariantString(int x, int y, int width, int height)
        {
            var rect = (Rectangle)Converter.ConvertFromInvariantString($"{x}, {y}, {width}, {height}");
            Assert.Equal(x, rect.X);
            Assert.Equal(y, rect.Y);
            Assert.Equal(width, rect.Width);
            Assert.Equal(height, rect.Height);
        }

        [Fact]
        public void ConvertFromInvariantString_ArgumentException()
        {
            ConvertFromInvariantStringThrowsArgumentException("1, 2, 3");
        }

        [Fact]
        public void ConvertFromInvariantString_FormatException()
        {
            ConvertFromInvariantStringThrowsFormatInnerException("hello");
        }

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertFromString(int x, int y, int width, int height)
        {
            var rect =
                (Rectangle)Converter.ConvertFromString(string.Format("{0}{4} {1}{4} {2}{4} {3}", x, y, width, height,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator));
            Assert.Equal(x, rect.X);
            Assert.Equal(y, rect.Y);
            Assert.Equal(width, rect.Width);
            Assert.Equal(height, rect.Height);
        }

        [Fact]
        public void ConvertFromString_ArgumentException()
        {
            ConvertFromStringThrowsArgumentException(string.Format("1{0} 1{0} 1{0} 1{0} 1",
                CultureInfo.CurrentCulture.TextInfo.ListSeparator));
        }

        [Fact]
        public void ConvertFromString_FormatException()
        {
            ConvertFromStringThrowsFormatInnerException("hello");
        }

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertToInvariantString(int x, int y, int width, int height)
        {
            var str = Converter.ConvertToInvariantString(new Rectangle(x, y, width, height));
            Assert.Equal($"{x}, {y}, {width}, {height}", str);
        }

        [Theory]
        [MemberData(nameof(RectangleData))]
        public void ConvertToString(int x, int y, int width, int height)
        {
            var str = Converter.ConvertToString(new Rectangle(x, y, width, height));
            Assert.Equal(
                string.Format("{0}{4} {1}{4} {2}{4} {3}", x, y, width, height,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator), str);
        }
    }
}
