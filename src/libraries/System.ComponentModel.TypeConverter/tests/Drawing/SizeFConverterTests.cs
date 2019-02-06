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
    public class SizeFConverterTests : StringTypeConverterTestBase<SizeF>
    {
        protected override TypeConverter Converter { get; } = new SizeFConverter();
        protected override bool StandardValuesSupported { get; } = false;
        protected override bool StandardValuesExclusive { get; } = false;
        protected override SizeF Default => new SizeF(1, 1);
        protected override bool CreateInstanceSupported { get; } = true;
        protected override bool IsGetPropertiesSupported { get; } = true;

        protected override IEnumerable<Tuple<SizeF, Dictionary<string, object>>> CreateInstancePairs
        {
            get
            {
                yield return Tuple.Create(new SizeF(10, 20), new Dictionary<string, object>
                {
                    ["Width"] = 10f,
                    ["Height"] = 20f,
                });
                yield return Tuple.Create(new SizeF(-2, 3), new Dictionary<string, object>
                {
                    ["Width"] = -2f,
                    ["Height"] = 3f,
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

        public static IEnumerable<object[]> SizeFData =>
            new[]
            {
                new object[] {0, 0},
                new object[] {1, 1},
                new object[] {-1, 1},
                new object[] {1, -1},
                new object[] {-1, -1},
                new object[] {float.MaxValue, float.MaxValue},
                new object[] {float.MinValue, float.MaxValue},
                new object[] {float.MaxValue, float.MinValue},
                new object[] {float.MinValue, float.MinValue},
            };

        [Theory]
        [MemberData(nameof(SizeFData))]
        public void ConvertFrom(float width, float height)
        {
            TestConvertFromString(new SizeF(width, height), FormattableString.Invariant($"{width:G9}, {height:G9}"));
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
                new object[] {new SizeF(1, 1)},
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
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void ConvertTo_NetFramework(float width, float height)
        {
            TestConvertToString(new SizeF(width, height), FormattableString.Invariant($"{width:G9}, {height:G9}"));
        }


        [Theory]
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ConvertTo_NotNetFramework(float width, float height)
        {
            TestConvertToString(new SizeF(width, height), FormattableString.Invariant($"{width}, {height}"));
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
            Assert.Equal($"1{listSep} 1", Converter.ConvertTo(null, null, new SizeF(1, 1), typeof(string)));
        }

        [Fact]
        public void CreateInstance_CaseSensitive()
        {
            // NET Framework throws NullReferenceException but we want it to be friendly on Core so it 
            // correctly throws an ArgumentException
            Type expectedExceptionType =
                PlatformDetection.IsFullFramework ? typeof(NullReferenceException) : typeof(ArgumentException);

            Assert.Throws(expectedExceptionType, () =>
            {
                Converter.CreateInstance(null, new Dictionary<string, object>
                {
                    ["width"] = 1,
                    ["Height"] = 1,
                });
            });
        }

        [Fact]
        public void GetProperties()
        {
            var pt = new SizeF(1, 1);
            var props = Converter.GetProperties(new SizeF(1, 1));
            Assert.Equal(2, props.Count);
            Assert.Equal(1f, props["Width"].GetValue(pt));
            Assert.Equal(1f, props["Height"].GetValue(pt));

            props = Converter.GetProperties(null, new SizeF(1, 1));
            Assert.Equal(2, props.Count);
            Assert.Equal(1f, props["Width"].GetValue(pt));
            Assert.Equal(1f, props["Height"].GetValue(pt));

            props = Converter.GetProperties(null, new SizeF(1, 1), null);
            Assert.Equal(3, props.Count);
            Assert.Equal(1f, props["Width"].GetValue(pt));
            Assert.Equal(1f, props["Height"].GetValue(pt));
            Assert.Equal(false, props["IsEmpty"].GetValue(pt));

            props = Converter.GetProperties(null, new SizeF(1, 1), new Attribute[0]);
            Assert.Equal(3, props.Count);
            Assert.Equal(1f, props["Width"].GetValue(pt));
            Assert.Equal(1f, props["Height"].GetValue(pt));
            Assert.Equal(false, props["IsEmpty"].GetValue(pt));

            // Pick an attibute that cannot be applied to properties to make sure everything gets filtered
            props = Converter.GetProperties(null, new SizeF(1, 1), new Attribute[] { new System.Reflection.AssemblyCopyrightAttribute("")});
            Assert.Equal(0, props.Count);
        }

        [Theory]
        [MemberData(nameof(SizeFData))]
        public void ConvertFromInvariantString(float width, float height)
        {
            var point = (SizeF)Converter.ConvertFromInvariantString(FormattableString.Invariant($"{width:G9}, {height:G9}"));
            Assert.Equal(width, point.Width);
            Assert.Equal(height, point.Height);
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
        [MemberData(nameof(SizeFData))]
        public void ConvertFromString(float width, float height)
        {
            var point =
                (SizeF)Converter.ConvertFromString(string.Format(CultureInfo.CurrentCulture, "{0:g9}{2} {1:g9}", width, height,
                    CultureInfo.CurrentCulture.TextInfo.ListSeparator));
            Assert.Equal(width, point.Width);
            Assert.Equal(height, point.Height);
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
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void ConvertToInvariantString_NetFramework(float width, float height)
        {
            var str = Converter.ConvertToInvariantString(new SizeF(width, height));
            Assert.Equal(FormattableString.Invariant($"{width:G9}, {height:G9}"), str);
        }

        [Theory]
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ConvertToInvariantString_NotNetFramework(float width, float height)
        {
            var str = Converter.ConvertToInvariantString(new SizeF(width, height));
            Assert.Equal(FormattableString.Invariant($"{width}, {height}"), str);
        }

        [Theory]
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void ConvertToString_NetFramework(float width, float height)
        {
            var str = Converter.ConvertToString(new SizeF(width, height));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{0:G9}{2} {1:G9}", width, height, CultureInfo.CurrentCulture.TextInfo.ListSeparator), str);
        }

        [Theory]
        [MemberData(nameof(SizeFData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void ConvertToString_NotNetFramework(float width, float height)
        {
            var str = Converter.ConvertToString(new SizeF(width, height));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, "{0}{2} {1}", width, height, CultureInfo.CurrentCulture.TextInfo.ListSeparator), str);
        }
    }
}
