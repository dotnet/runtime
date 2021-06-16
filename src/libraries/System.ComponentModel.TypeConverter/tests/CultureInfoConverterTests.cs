// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class CultureInfoConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new CultureInfoConverter();

        public override bool StandardValuesSupported => true;

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid(string.Empty, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid("nl-BE", new CultureInfo("nl-BE"), CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid("(Default)", CultureInfo.InvariantCulture, CultureInfo.InvariantCulture);
            CultureInfo culture = null;
            try
            {
                culture = new CultureInfo("nl--B");
            }
            catch { }

            if (culture != null)
            {
                yield return ConvertTest.Valid("nl-B", new CultureInfo("nl--B"), CultureInfo.InvariantCulture);
                yield return ConvertTest.Valid("nl-B", new CultureInfo("nl--B"), new CultureInfo("en-US"));
            }
            if (PlatformDetection.IsNotBrowser)
            {
                yield return ConvertTest.Valid("Afrikaans", new CultureInfo("af"));
            }

            yield return ConvertTest.CantConvertFrom(CultureInfo.CurrentCulture);
            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid(new CustomCultureInfo(), "nl-BE");
            yield return ConvertTest.Valid(null, "(Default)");
            yield return ConvertTest.Valid(CultureInfo.InvariantCulture, "(Default)");
            yield return ConvertTest.Valid(CultureInfo.InvariantCulture, "(Default)", CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid(new CultureInfo("nl-BE"), "nl-BE");
            yield return ConvertTest.Valid(1, "1");

            yield return ConvertTest.CantConvertTo(CultureInfo.InvariantCulture, typeof(object));
            yield return ConvertTest.CantConvertTo(CultureInfo.InvariantCulture, typeof(CultureInfo));
            yield return ConvertTest.CantConvertTo(CultureInfo.InvariantCulture, typeof(int));
        }

        [Theory]
        [InlineData("Dutch (Bel")]
        [InlineData("(default)")]
        [InlineData(" ")]
        [InlineData("\r\n")]
        public void ConvertFrom_String_InvalidCulture(string cultureName)
        {
            try
            {
                // Linux may be able to create these cultures.
                new CultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                // If we cannot create the cultures we should get exception from the Converter too.
                AssertExtensions.Throws<ArgumentException>("value", () => Converter.ConvertFrom(null, CultureInfo.InvariantCulture, cultureName));
            }
        }

        [Fact]
        public void ConvertTo_InstanceDescriptor_ReturnsExpected()
        {
            var culture = new CultureInfo("en-US");
            Assert.True(Converter.CanConvertTo(typeof(InstanceDescriptor)));

            InstanceDescriptor instanceDescriptor = Assert.IsType<InstanceDescriptor>(Converter.ConvertTo(culture, typeof(InstanceDescriptor)));
            Assert.Equal(new Type[] { typeof(string) }, Assert.IsAssignableFrom<ConstructorInfo>(instanceDescriptor.MemberInfo).GetParameters().Select(p => p.ParameterType));
            Assert.Equal(new object[] { culture.Name }, instanceDescriptor.Arguments);
        }

        [Theory]
        [InlineData(typeof(InstanceDescriptor))]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidValue_ThrowsNotSupportedException(Type destinationType)
        {
            Assert.Throws<NotSupportedException>(() => Converter.ConvertTo(new object(), destinationType));
        }

        [Theory]
        [InlineData(typeof(InstanceDescriptor))]
        [InlineData(typeof(int))]
        public void ConvertTo_InstanceAndNullCulture_ThrowsNotSupportedException(Type destinationType)
        {
            Assert.Throws<NotSupportedException>(() => Converter.ConvertTo(null, destinationType));
        }

        public static IEnumerable<object[]> GetCultureName_TestData()
        {
            yield return new object[] { new CultureInfo("fr-FR"), new CultureInfo("fr-FR").Name };
            yield return new object[] { new CultureInfo("es-MX"), new CultureInfo("es-MX").Name };
        }

        [Theory]
        [MemberData(nameof(GetCultureName_TestData))]
        public void GetCultureName_Invoke_ReturnsExpected(CultureInfo culture, string expected)
        {
            var converter = new SubCultureInfoConverter();
            Assert.Equal(expected, converter.GetCultureName(culture));
        }

        [Fact]
        public void GetCultureName_NullCulture_ThrowsArgumentNullException()
        {
            var converter = new SubCultureInfoConverter();
            Assert.Throws<ArgumentNullException>("culture", () => converter.GetCultureName(null));
        }

        public static IEnumerable<object[]> ConvertFrom_OverridenGetCultureName_TestData()
        {
            yield return new object[] { "Fixed", "Fixed", CultureInfo.InvariantCulture };
            yield return new object[] { "None", "en-US", new CultureInfo("en-US") };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_OverridenGetCultureName_TestData))]
        public void ConvertFrom_OverridenGetCultureName_ReturnsExpected(string fixedValue, string text, CultureInfo expected)
        {
            var converter = new FixedCultureInfoConverter
            {
                FixedValue = fixedValue
            };
            Assert.Equal(expected, converter.ConvertFromString(text));
        }

        [Fact]
        public void GetCultureName_Overriden_ConversionsReturnsExpected()
        {
            var converter = new FixedCultureInfoConverter
            {
                FixedValue = "Fixed"
            };
            Assert.Equal("(Default)", converter.ConvertTo(CultureInfo.InvariantCulture, typeof(string)));
            Assert.Equal("Fixed", converter.ConvertTo(new CultureInfo("en-US"), typeof(string)));
        }

        private class SubCultureInfoConverter : CultureInfoConverter
        {
            public new string GetCultureName(CultureInfo culture)
            {
                return base.GetCultureName(culture);
            }
        }

        private class FixedCultureInfoConverter : CultureInfoConverter
        {
            public string FixedValue { get; set; }

            protected override string GetCultureName(CultureInfo culture) => FixedValue;
        }

        [Serializable]
        private sealed class CustomCultureInfo : CultureInfo
        {
            public CustomCultureInfo() : base("nl-BE")
            {
            }

            public override string DisplayName => "display";

            public override string EnglishName => "english";
        }
    }
}
