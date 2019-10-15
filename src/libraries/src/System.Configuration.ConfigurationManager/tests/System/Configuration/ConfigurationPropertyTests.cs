// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationPropertyTests
    {
        [Fact]
        public void ConfigurationSectionThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new ConfigurationProperty("foo", typeof(ConfigurationSection)));
        }

        [Fact]
        public void AppSettingsSectionThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new ConfigurationProperty("foo", typeof(AppSettingsSection)));
        }

        [Fact]
        public void NullNameThrows()
        {
            AssertExtensions.Throws<ArgumentException>("name", () => new ConfigurationProperty(null, typeof(string)));
        }

        [Fact]
        public void EmptyNameThrows()
        {
            AssertExtensions.Throws<ArgumentException>("name", () => new ConfigurationProperty("", typeof(string)));
        }

        [Theory,
            InlineData("lock"),
            InlineData("locks"),
            InlineData("config"),
            InlineData("configuration")
            ]
        public void ReservedNameThrows(string name)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ConfigurationProperty(name, typeof(string)));
        }

        [Theory,
            InlineData("Lock"),
            InlineData("ilock"),
            InlineData("LOCKS"),
            InlineData("CoNfig"),
            InlineData("conFIGuration")
            ]
        public void ReservedNameOrdinallyCompared(string name)
        {
            // Want to make sure the comparison is ordinal and starts with if people have depended on this
            new ConfigurationProperty(name, typeof(string));
        }

        // Base class returns false for CanValidate by default
        public class CantValidateValidator : ConfigurationValidatorBase
        {
            public override void Validate(object value)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void NonMatchingValidatorThrows()
        {
            CantValidateValidator validator = new CantValidateValidator();
            Assert.Throws<ConfigurationErrorsException>(() => new ConfigurationProperty("foo", typeof(string), null, null, validator, ConfigurationPropertyOptions.None));
        }

        public class FooFailsValidator : ConfigurationValidatorBase
        {
            public override bool CanValidate(Type type)
            {
                return true;
            }

            public override void Validate(object value)
            {
                if (string.Equals(value, "foo"))
                    throw new InvalidOperationException();
            }
        }

        [Fact]
        public void BadDefaultValueThrows()
        {
            FooFailsValidator validator = new FooFailsValidator();
            Action action = () => new ConfigurationProperty("bar", typeof(string), "foo", null, validator, ConfigurationPropertyOptions.None);
            Assert.IsType<InvalidOperationException>(Assert.Throws<ConfigurationErrorsException>(action).InnerException);
        }

        [TypeConverter(typeof(DummyCantConverter))]
        public class SimpleConfigurationElement : ConfigurationElement
        {
        }

        // By default can't convert from or to
        public class DummyCantConverter : TypeConverter
        {
        }

        public class DummyCanConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(string);
            }
        }

        [Fact]
        public void ConfigurationElementConverterIgnored()
        {
            ConfigurationProperty property = new ConfigurationProperty("foo", typeof(SimpleConfigurationElement));
            Assert.Null(property.Converter);
        }

        [TypeConverter(typeof(DummyCanConverter))]
        public class MyConvertableClass
        {
        }

        [Fact]
        public void TypeConverterRecognized()
        {
            ConfigurationProperty property = new ConfigurationProperty("foo", typeof(MyConvertableClass));
            Assert.IsType<DummyCanConverter>(property.Converter);
        }

        [TypeConverter(typeof(DummyCantConverter))]
        public class MyUnconvertableClass
        {
        }

        [Fact]
        public void UnconvertableFailsOnConverterAccess()
        {
            ConfigurationProperty property = new ConfigurationProperty("foo", typeof(MyUnconvertableClass));
            Assert.Throws<ConfigurationErrorsException>(() => property.Converter);
        }

        [TypeConverter(typeof(DummyCantConverter))]
        public enum AllSay
        {
            Yea,
            Nay
        }

        [Fact]
        public void EnumsGetGenericEnumConverter()
        {
            ConfigurationProperty property = new ConfigurationProperty("foo", typeof(AllSay));
            Assert.IsType<GenericEnumConverter>(property.Converter);
        }

        [Theory,
            InlineData(typeof(string), typeof(StringConverter)),
            InlineData(typeof(int), typeof(Int32Converter))
            ]
        public void FindConverterForBuiltInTypes(Type type, Type converterType)
        {
            ConfigurationProperty property = new ConfigurationProperty("foo", type);
            Assert.IsType(converterType, property.Converter);
        }

        [Fact]
        public void IsRequiredExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsRequired);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsRequired).IsRequired);
        }

        [Fact]
        public void IsKeyExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsRequired);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsKey).IsKey);
        }

        [Fact]
        public void IsDefaultCollectionExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsDefaultCollection);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsDefaultCollection).IsDefaultCollection);
        }

        [Fact]
        public void IsTypeStringTransformationRequiredExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsTypeStringTransformationRequired);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsTypeStringTransformationRequired).IsTypeStringTransformationRequired);
        }

        [Fact]
        public void IsAssemblyStringTransformationRequiredExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsAssemblyStringTransformationRequired);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsAssemblyStringTransformationRequired).IsAssemblyStringTransformationRequired);
        }

        [Fact]
        public void IsVersionCheckRequiredExposed()
        {
            Assert.False(new ConfigurationProperty("foo", typeof(string)).IsVersionCheckRequired);
            Assert.True(new ConfigurationProperty("foo", typeof(string), null, ConfigurationPropertyOptions.IsVersionCheckRequired).IsVersionCheckRequired);
        }

        [Fact]
        public void TypeIsExposed()
        {
            Assert.Equal(typeof(string), new ConfigurationProperty("foo", typeof(string)).Type);
        }

        [Fact]
        public void DefaultValueIsExposed()
        {
            Assert.Equal("bar", new ConfigurationProperty("foo", typeof(string), "bar").DefaultValue);
        }
    }
}
