// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.TypeConverterTests
{
    public abstract class StringTypeConverterTestBase<T>
    {
        protected abstract T Default { get; }
        protected abstract TypeConverter Converter { get; }
        protected abstract bool StandardValuesSupported { get; }
        protected abstract bool StandardValuesExclusive { get; }
        protected abstract bool CreateInstanceSupported { get; }
        protected abstract bool IsGetPropertiesSupported { get; }

        protected virtual IEnumerable<Tuple<T, Dictionary<string, object>>> CreateInstancePairs
        {
            get { yield break; }
        }

        [Fact]
        public void GetStandardValuesSupported()
        {
            Assert.Equal(StandardValuesSupported, Converter.GetStandardValuesSupported());
            Assert.Equal(StandardValuesSupported, Converter.GetStandardValuesSupported(null));
        }

        [Fact]
        public void GetStandardValues()
        {
            if (!StandardValuesSupported)
            {
                Assert.Null(Converter.GetStandardValues());
            }
        }

        [Fact]
        public void GetStandardValuesExclusive()
        {
            Assert.Equal(StandardValuesExclusive, Converter.GetStandardValuesExclusive());
        }

        protected void CanConvertFrom(Type type)
        {
            Assert.True(Converter.CanConvertFrom(type));
            Assert.True(Converter.CanConvertFrom(null, type));
        }

        protected void CannotConvertFrom(Type type)
        {
            Assert.False(Converter.CanConvertFrom(type));
            Assert.False(Converter.CanConvertFrom(null, type));
        }

        protected void CanConvertTo(Type type)
        {
            Assert.True(Converter.CanConvertTo(type));
            Assert.True(Converter.CanConvertTo(null, type));
        }

        protected void CannotConvertTo(Type type)
        {
            Assert.False(Converter.CanConvertTo(type));
            Assert.False(Converter.CanConvertTo(null, type));
        }

        protected void TestConvertFromString(T value, string str)
        {
            Assert.Equal(value, (T)Converter.ConvertFrom(null, CultureInfo.InvariantCulture, str));
        }

        protected void TestConvertToString(T value, string str)
        {
            Assert.Equal(str, (string)Converter.ConvertTo(null, CultureInfo.InvariantCulture, value, typeof(string)));
        }

        protected void ConvertFromThrowsArgumentExceptionForString(string value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
        }

        protected void ConvertFromThrowsFormatInnerExceptionForString(string value)
        {
            var ex = AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                Converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
            Assert.NotNull(ex.InnerException);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        protected void ConvertFromThrowsNotSupportedFor(object value)
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                Converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
            });
        }

        protected void ConvertToThrowsNotSupportedForType(Type type)
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                Converter.ConvertTo(null, CultureInfo.InvariantCulture, Default, type);
            });
        }

        [Fact]
        public void GetCreateInstanceSupported()
        {
            Assert.Equal(CreateInstanceSupported, Converter.GetCreateInstanceSupported());
            Assert.Equal(CreateInstanceSupported, Converter.GetCreateInstanceSupported(null));
        }

        [Fact]
        public void CreateInstance()
        {
            foreach (var pair in CreateInstancePairs)
            {
                Assert.Equal(pair.Item1, Converter.CreateInstance(pair.Item2));
            }
        }

        [Fact]
        public void GetPropertiesSupported()
        {
            Assert.Equal(IsGetPropertiesSupported, Converter.GetPropertiesSupported());
            Assert.Equal(IsGetPropertiesSupported, Converter.GetPropertiesSupported(null));
        }

        protected void ConvertFromInvariantStringThrowsArgumentException(string str)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Converter.ConvertFromInvariantString(str);
            });
        }

        protected void ConvertFromInvariantStringThrowsFormatInnerException(string str)
        {
            var ex = AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                Converter.ConvertFromInvariantString(str);
            });
            Assert.NotNull(ex.InnerException);
            Assert.IsType<FormatException>(ex.InnerException);
        }

        protected void ConvertFromStringThrowsArgumentException(string str)
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                Converter.ConvertFromString(str);
            });
        }

        protected void ConvertFromStringThrowsFormatInnerException(string str)
        {
            var ex = AssertExtensions.Throws<ArgumentException, Exception>(() =>
            {
                Converter.ConvertFromString(str);
            });
            Assert.NotNull(ex.InnerException);
            Assert.IsType<FormatException>(ex.InnerException);
        }
    }
}
