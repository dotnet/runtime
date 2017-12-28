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
    public class ColorConverterTests
    {
        [Theory]
        [InlineData(typeof(string))]
        public void CanConvertFromTrue(Type type)
        {
            var conv = new ColorConverter();
            Assert.True(conv.CanConvertFrom(type));
            Assert.True(conv.CanConvertFrom(null, type));
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
            var conv = new ColorConverter();
            Assert.False(conv.CanConvertFrom(type));
            Assert.False(conv.CanConvertFrom(null, type));
        }

        [Theory]
        [InlineData(typeof(string))]
        public void CanConvertToTrue(Type type)
        {
            var conv = new ColorConverter();
            Assert.True(conv.CanConvertTo(type));
            Assert.True(conv.CanConvertTo(null, type));
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
            var conv = new ColorConverter();
            Assert.False(conv.CanConvertTo(type));
            Assert.False(conv.CanConvertTo(null, type));
        }

        public static IEnumerable<object[]> ColorData
        {
            get
            {
                for (int a = 0; a < 256; a += 53)
                {
                    for (int r = 0; r < 256; r += 59)
                    {
                        for (int g = 0; g < 256; g += 61)
                        {
                            for (int b = 0; b < 256; b += 67)
                            {
                                yield return new object[] { a, r, g, b };
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> ColorNames => typeof(Color).GetProperties()
                .Where(p => p.PropertyType == typeof(Color))
                .Select(p => new object[] { p.Name });

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertFrom(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            Color color = (Color)conv.ConvertFrom(null, CultureInfo.InvariantCulture, $"#0x{a:x2}{r:x2}{g:x2}{b:x2}");
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);

            Assert.Equal(color,
                (Color)conv.ConvertFrom(null, CultureInfo.InvariantCulture, $"#0X{a:x2}{r:x2}{g:x2}{b:x2}"));
            Assert.Equal(color,
                (Color)conv.ConvertFrom(null, CultureInfo.InvariantCulture, $"0x{a:x2}{r:x2}{g:x2}{b:x2}"));
            Assert.Equal(color,
                (Color)conv.ConvertFrom(null, CultureInfo.InvariantCulture, $"0X{a:x2}{r:x2}{g:x2}{b:x2}"));
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertFrom_InvariantSeparator(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var color =
                (Color)
                conv.ConvertFrom(null, CultureInfo.InvariantCulture,
                    string.Format("{0}{4} {1}{4} {2}{4} {3}", a, r, g, b,
                        CultureInfo.InvariantCulture.TextInfo.ListSeparator));
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertFrom_FrFrSeparator(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var culture = new CultureInfo("fr-FR");
            var color =
                (Color)
                conv.ConvertFrom(null, culture,
                    string.Format("{0}{4} {1}{4} {2}{4} {3}", a, r, g, b,
                        culture.TextInfo.ListSeparator));
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertFrom_Name(string name)
        {
            var conv = new ColorConverter();
            var color = Color.FromName(name);
            Assert.Equal(color, (Color)conv.ConvertFrom(name));
            Assert.Equal(color, (Color)conv.ConvertFrom(" " + name + " "));
        }

        [Fact]
        public void ConvertFrom_Empty()
        {
            var conv = new ColorConverter();
            var color = Color.Empty;
            Assert.Equal(color, (Color)conv.ConvertFrom(string.Empty));
            Assert.Equal(color, (Color)conv.ConvertFrom(" "));
        }

        [Theory]
        [InlineData("10, 20")]
        [InlineData("-10, 20, 30")]
        [InlineData("1, 1, 1, 1, 1")]
        public void ConvertFrom_ArgumentException(string value)
        {
            var conv = new ColorConverter();
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                conv.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
        }

        [Theory]
        [InlineData("*1, 1")]
        public void ConvertFrom_Exception(string value)
        {
            var conv = new ColorConverter();
            AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                conv.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
        }

        public static IEnumerable<object[]> ConvertFrom_NotsupportedExceptionData =>
            new[]
            {
                new object[] {new Point(10, 10)},
                new object[] {new PointF(10, 10)},
                new object[] {new Size(10, 10)},
                new object[] {new SizeF(10, 10)},
                new object[] {0x10},
            };

        [Theory]
        [MemberData(nameof(ConvertFrom_NotsupportedExceptionData))]
        public void ConvertFrom_NotSupportedException(object value)
        {
            var conv = new ColorConverter();
            Assert.Throws<NotSupportedException>(() =>
            {
                conv.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
        }

        [Fact]
        public void ConvertFrom_NullCulture()
        {
            var conv = new ColorConverter();
            var color = (Color)conv.ConvertFrom(null, null, "#0x23190A44");
            Assert.Equal(35, color.A);
            Assert.Equal(25, color.R);
            Assert.Equal(10, color.G);
            Assert.Equal(68, color.B);
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertTo(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            Assert.Equal($"{a}, {r}, {g}, {b}",
                (string)conv.ConvertTo(null, CultureInfo.InvariantCulture, Color.FromArgb(a, r, g, b), typeof(string)));
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertTo_Named(string name)
        {
            var conv = new ColorConverter();
            Assert.Equal(name,
                (string)conv.ConvertTo(null, CultureInfo.InvariantCulture, Color.FromName(name), typeof(string)));
        }

        [Fact]
        public void ConvertTo_Empty()
        {
            var conv = new ColorConverter();
            Assert.Equal(string.Empty,
                (string)conv.ConvertTo(null, CultureInfo.InvariantCulture, Color.Empty, typeof(string)));
        }

        [Theory]
        [InlineData(typeof(Color))]
        [InlineData(typeof(Size))]
        [InlineData(typeof(SizeF))]
        [InlineData(typeof(Point))]
        [InlineData(typeof(PointF))]
        [InlineData(typeof(int))]
        public void ConvertTo_NotSupported(Type type)
        {
            var conv = new ColorConverter();
            var col = Color.Red;
            Assert.Throws<NotSupportedException>(() =>
            {
                conv.ConvertTo(null, CultureInfo.InvariantCulture, col, type);
            });
        }

        [Fact]
        public void GetCreateInstanceSupported()
        {
            var conv = new ColorConverter();
            Assert.False(conv.GetCreateInstanceSupported());
            Assert.False(conv.GetCreateInstanceSupported(null));
        }

        [Fact]
        public void CreateInstance()
        {
            var conv = new ColorConverter();
            Assert.Null(conv.CreateInstance(new Dictionary<string, object>
            {
                ["R"] = 10,
                ["G"] = 20,
                ["B"] = 30,
            }));

            Assert.Null(conv.CreateInstance(new Dictionary<string, object>
            {
                ["Name"] = "ForestGreen",
            }));
        }

        [Fact]
        public void GetPropertiesSupported()
        {
            var conv = new ColorConverter();
            Assert.False(conv.GetPropertiesSupported());
            Assert.False(conv.GetPropertiesSupported(null));
        }

        [Fact]
        public void GetProperties()
        {
            var conv = new ColorConverter();
            Assert.Null(conv.GetProperties(Color.Red));
            Assert.Null(conv.GetProperties(null, Color.Red, null));
            Assert.Null(conv.GetProperties(null, Color.Red,
                typeof(Color).GetCustomAttributes(true).OfType<Attribute>().ToArray()));
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertFromInvariantString(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var color = (Color)conv.ConvertFromInvariantString($"{a}, {r}, {g}, {b}");
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertFromInvariantString_Name(string name)
        {
            var conv = new ColorConverter();
            var color = Color.FromName(name);
            Assert.Equal(color, (Color)conv.ConvertFromInvariantString(name));
        }

        [Fact]
        public void ConvertFromInvariantString_Invalid()
        {
            var conv = new ColorConverter();
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                conv.ConvertFromInvariantString("1, 2, 3, 4, 5");
            });
        }

        [Fact]
        public void ConvertFromInvariantString_NotNumber()
        {
            var conv = new ColorConverter();
            var ex = AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                conv.ConvertFromInvariantString("hello");
            });
            Assert.NotNull(ex.InnerException);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertFromString(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var color =
                (Color)
                conv.ConvertFromString(string.Format("{0}{4} {1}{4} {2}{4} {3}", a, r, g, b,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator));
            Assert.Equal(a, color.A);
            Assert.Equal(r, color.R);
            Assert.Equal(g, color.G);
            Assert.Equal(b, color.B);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertFromString_Name(string name)
        {
            var conv = new ColorConverter();
            var color = Color.FromName(name);
            Assert.Equal(color, (Color)conv.ConvertFromString(name));
        }

        [Fact]
        public void ConvertFromString_Invalid()
        {
            var conv = new ColorConverter();
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                conv.ConvertFromString(string.Format("1{0} 2{0} 3{0} 4{0} 5", CultureInfo.CurrentCulture.TextInfo.ListSeparator));
            });
        }

        [Fact]
        public void ConvertFromString_NotNumber()
        {
            var conv = new ColorConverter();
            var ex = AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                conv.ConvertFromString("hello");
            });
            Assert.NotNull(ex.InnerException);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertToInvariantString(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var str = conv.ConvertToInvariantString(Color.FromArgb(a, r, g, b));
            Assert.Equal($"{a}, {r}, {g}, {b}", str);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertToInvariantString_Name(string name)
        {
            var conv = new ColorConverter();
            Assert.Equal(name, conv.ConvertToInvariantString(Color.FromName(name)));
        }

        [Theory]
        [MemberData(nameof(ColorData))]
        public void ConvertToString(int a, int r, int g, int b)
        {
            var conv = new ColorConverter();
            var str = conv.ConvertToString(Color.FromArgb(a, r, g, b));
            Assert.Equal(string.Format("{0}{4} {1}{4} {2}{4} {3}", a, r, g, b, CultureInfo.CurrentCulture.TextInfo.ListSeparator), str);
        }

        [Theory]
        [MemberData(nameof(ColorNames))]
        public void ConvertToString_Name(string name)
        {
            var conv = new ColorConverter();
            Assert.Equal(name, conv.ConvertToString(Color.FromName(name)));
        }

        [Fact]
        public void GetStandardValuesSupported()
        {
            var conv = new ColorConverter();
            Assert.True(conv.GetStandardValuesSupported());
            Assert.True(conv.GetStandardValuesSupported(null));
        }

        [Fact]
        public void GetStandardValues()
        {
            var conv = new ColorConverter();

            Assert.True(conv.GetStandardValues().Count > 0);
        }

        [Fact]
        public void GetStandardValuesExclusive()
        {
            var conv = new ColorConverter();
            Assert.False(conv.GetStandardValuesExclusive());
        }
    }
}
