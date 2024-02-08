// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
#if BUILDING_SOURCE_GENERATOR_TESTS
using Microsoft.Extensions.Configuration;
#endif
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    public abstract class ConfigurationBinderTestsBase
    {
        public ConfigurationBinderTestsBase()
        {
#if LAUNCH_DEBUGGER
if (!System.Diagnostics.Debugger.IsAttached) { System.Diagnostics.Debugger.Launch(); }
#endif
        }
    }

    public partial class ConfigurationBinderTests : ConfigurationBinderTestsBase
    {
        [Fact]
        public void BindWithNestedTypesWithReadOnlyProperties()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Nested:MyProp", "Dummy" }
                })
                .Build();

            var result = configuration.Get<RootConfig>();
            Assert.Equal("Dummy", result.Nested.MyProp);

            result = (RootConfig)configuration.Get(typeof(RootConfig));
            Assert.Equal("Dummy", result.Nested.MyProp);

            result = result = configuration.Get<RootConfig>(options => { });
            Assert.Equal("Dummy", result.Nested.MyProp);

            result = (RootConfig)configuration.Get(typeof(RootConfig), options => { });
            Assert.Equal("Dummy", result.Nested.MyProp);
        }

        // Add test for type with parameterless ctor + init-only properties.

        [Fact]
        public void EnumBindCaseInsensitiveNotThrows()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Option1", "opt1"},
                {"Section:option2", "opt2"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var configSection = config.GetSection("Section");

            var configOptions = new Dictionary<TestSettingsEnum, string>();
            configSection.Bind(configOptions);

            Assert.Equal("opt1", configOptions[TestSettingsEnum.Option1]);
            Assert.Equal("opt2", configOptions[TestSettingsEnum.Option2]);
        }

        [Fact]
        public void CanBindIConfigurationSection()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ConfigurationInterfaceOptions>();
            var childOptions = options.Section.Get<DerivedOptions>();
            Test();

            options = (ConfigurationInterfaceOptions)config.Get(typeof(ConfigurationInterfaceOptions));
            childOptions = (DerivedOptions)options.Section.Get(typeof(DerivedOptions));
            Test();

            options = config.Get<ConfigurationInterfaceOptions>(options => { });
            childOptions = options.Section.Get<DerivedOptions>(options => { });
            Test();

            options = (ConfigurationInterfaceOptions)config.Get(typeof(ConfigurationInterfaceOptions), options => { });
            childOptions = (DerivedOptions)options.Section.Get(typeof(DerivedOptions), options => { });
            Test();

            void Test()
            {
                Assert.True(childOptions.Boolean);
                Assert.Equal(-2, childOptions.Integer);
                Assert.Equal(11, childOptions.Nested.Integer);
                Assert.Equal("Derived:Sup", childOptions.Virtual);

                Assert.Equal("Section", options.Section.Key);
                Assert.Equal("Section", options.Section.Path);
                Assert.Null(options.Section.Value);
            }
        }

        [Fact]
        public void CanBindWithKeyOverload()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new DerivedOptions();
            config.Bind("Section", options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void CanBindIConfigurationSectionWithDerivedOptionsSection()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"},
                {"Section:DerivedSection:Nested:Integer", "11"},
                {"Section:DerivedSection:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ConfigurationInterfaceOptions>();

            var childOptions = options.Section.Get<DerivedOptionsWithIConfigurationSection>();

            var childDerivedOptions = childOptions.DerivedSection.Get<DerivedOptions>();

            Assert.True(childOptions.Boolean);
            Assert.Equal(-2, childOptions.Integer);
            Assert.Equal(11, childOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childOptions.Virtual);
            Assert.Equal(11, childDerivedOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childDerivedOptions.Virtual);

            Assert.Equal("Section", options.Section.Key);
            Assert.Equal("Section", options.Section.Path);
            Assert.Equal("DerivedSection", childOptions.DerivedSection.Key);
            Assert.Equal("Section:DerivedSection", childOptions.DerivedSection.Path);
            Assert.Null(options.Section.Value);
        }

        [Fact]
        public void CanBindConfigurationKeyNameAttributes()
        {
            var dic = new Dictionary<string, string>
            {
                {"Named_Property", "Yo"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>();

            Assert.Equal("Yo", options.NamedProperty);
        }

        [Fact]
        public void EmptyStringIsNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"empty", ""},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

#if BUILDING_SOURCE_GENERATOR_TESTS
            // Ensure exception messages are in sync
            Assert.Throws<InvalidOperationException>(() => config.GetValue<bool?>("empty"));
            Assert.Throws<InvalidOperationException>(() => config.GetValue<int?>("empty"));
#else
            Assert.Null(config.GetValue<bool?>("empty"));
            Assert.Null(config.GetValue<int?>("empty"));
#endif
        }

        [Fact]
        public void Get_Scalar()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.True(config.GetValue<bool>("Boolean"));
            Assert.Equal(-2, config.GetValue<int>("Integer"));
            Assert.Equal(11, config.GetValue<int>("Nested:Integer"));

            Assert.True((bool)config.GetValue(typeof(bool), "Boolean"));
            Assert.Equal(-2, (int)config.GetValue(typeof(int), "Integer"));
            Assert.Equal(11, (int)config.GetValue(typeof(int), "Nested:Integer"));
        }

        [Fact]
        public void Get_ScalarNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.True(config.GetValue<bool?>("Boolean"));
            Assert.Equal(-2, config.GetValue<int?>("Integer"));
            Assert.Equal(11, config.GetValue<int?>("Nested:Integer"));

            Assert.True((bool)config.GetValue(typeof(bool?), "Boolean"));
            Assert.Equal(-2, (int)config.GetValue(typeof(int?), "Integer"));
            Assert.Equal(11, (int)config.GetValue(typeof(int?), "Nested:Integer"));
        }

        [Fact]
        public void GetValue_Scalar()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.True(config.GetSection("Boolean").Get<bool>());
            Assert.Equal(-2, config.GetSection("Integer").Get<int>());
            Assert.Equal(11, config.GetSection("Nested:Integer").Get<int>());

            Assert.True((bool)config.GetSection("Boolean").Get(typeof(bool)));
            Assert.Equal(-2, (int)config.GetSection("Integer").Get(typeof(int)));
            Assert.Equal(11, (int)config.GetSection("Nested:Integer").Get(typeof(int)));
        }

        [Fact]
        public void GetValue_ScalarNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.True(config.GetSection("Boolean").Get<bool?>());
            Assert.Equal(-2, config.GetSection("Integer").Get<int?>());
            Assert.Equal(11, config.GetSection("Nested:Integer").Get<int?>());

            Assert.True(config.GetSection("Boolean").Get(typeof(bool?)) is true);
            Assert.Equal(-2, (int)config.GetSection("Integer").Get(typeof(int?)));
            Assert.Equal(11, (int)config.GetSection("Nested:Integer").Get(typeof(int?)));
        }

        [Fact]
        public void CanBindToObjectProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"Object", "whatever" }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Equal("whatever", options.Object);
        }

        [Fact]
        public void GetNullValue()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", null},
                {"Boolean", null},
                {"Nested:Integer", null},
                {"Object", null }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            // Generic overloads.
            Assert.False(config.GetValue<bool>("Boolean"));
            Assert.Equal(0, config.GetValue<int>("Integer"));
            Assert.Equal(0, config.GetValue<int>("Nested:Integer"));
            Assert.Null(config.GetValue<ComplexOptions>("Object"));

            // Generic overloads with default value.
            Assert.True(config.GetValue("Boolean", true));
            Assert.Equal(1, config.GetValue("Integer", 1));
            Assert.Equal(1, config.GetValue("Nested:Integer", 1));
            Assert.Equal(new NestedConfig(""), config.GetValue("Object", new NestedConfig("")));

            // Type overloads.
            Assert.Null(config.GetValue(typeof(bool), "Boolean"));
            Assert.Null(config.GetValue(typeof(int), "Integer"));
            Assert.Null(config.GetValue(typeof(int), "Nested:Integer"));
            Assert.Null(config.GetValue(typeof(ComplexOptions), "Object"));

            // Type overloads with default value.
            Assert.True((bool)config.GetValue(typeof(bool), "Boolean", true));
            Assert.Equal(1, (int)config.GetValue(typeof(int), "Integer", 1));
            Assert.Equal(1, (int)config.GetValue(typeof(int), "Nested:Integer", 1));
            Assert.Equal(new NestedConfig(""), config.GetValue("Object", new NestedConfig("")));

            // GetSection tests.
            Assert.False(config.GetSection("Boolean").Get<bool>());
            Assert.Equal(0, config.GetSection("Integer").Get<int>());
            Assert.Equal(0, config.GetSection("Nested:Integer").Get<int>());
            Assert.Null(config.GetSection("Object").Get<ComplexOptions>());
        }

        [Fact]
        public void ThrowsIfPropertyInConfigMissingInModel_Bind()
        {
            var dic = new Dictionary<string, string>
            {
                {"ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(ComplexOptions), "'ThisDoesNotExistInTheModel'");

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ThrowsIfPropertyInConfigMissingInNestedModel_Bind()
        {
            var dic = new Dictionary<string, string>
            {
                {"Nested:ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(NestedOptions), "'ThisDoesNotExistInTheModel'");

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ThrowsIfPropertyInConfigMissingInModel_Get()
        {
            var dic = new Dictionary<string, string>
            {
                {"ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(ComplexOptions), "'ThisDoesNotExistInTheModel'");

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ThrowsIfPropertyInConfigMissingInNestedModel_Get()
        {
            var dic = new Dictionary<string, string>
            {
                {"Nested:ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(NestedOptions), "'ThisDoesNotExistInTheModel'");

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void GetDefaultsWhenDataDoesNotExist()
        {
            var dic = new Dictionary<string, string>
            {
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.False(config.GetValue<bool>("Boolean"));
            Assert.Equal(0, config.GetValue<int>("Integer"));
            Assert.Equal(0, config.GetValue<int>("Nested:Integer"));
            Assert.Null(config.GetValue<ComplexOptions>("Object"));
            Assert.True(config.GetValue("Boolean", true));
            Assert.Equal(3, config.GetValue("Integer", 3));
            Assert.Equal(1, config.GetValue("Nested:Integer", 1));
            var foo = new ComplexOptions();
            Assert.Same(config.GetValue("Object", foo), foo);
        }

        [Fact]
        public void GetUri()
        {
            var dic = new Dictionary<string, string>
            {
                {"AnUri", "http://www.bing.com"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var uri = config.GetValue<Uri>("AnUri");

            Assert.Equal("http://www.bing.com", uri.OriginalString);
        }

        [ConditionalTheory(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Reflection fallback: generic type info not supported with source gen.
        [InlineData("2147483647", typeof(int))]
        [InlineData("4294967295", typeof(uint))]
        [InlineData("32767", typeof(short))]
        [InlineData("65535", typeof(ushort))]
        [InlineData("-9223372036854775808", typeof(long))]
        [InlineData("18446744073709551615", typeof(ulong))]
        [InlineData("trUE", typeof(bool))]
        [InlineData("255", typeof(byte))]
        [InlineData("127", typeof(sbyte))]
        [InlineData("\uffff", typeof(char))]
        [InlineData("79228162514264337593543950335", typeof(decimal))]
        [InlineData("1.79769e+308", typeof(double))]
        [InlineData("3.40282347E+38", typeof(float))]
        [InlineData("2015-12-24T07:34:42-5:00", typeof(DateTime))]
        [InlineData("12/24/2015 13:44:55 +4", typeof(DateTimeOffset))]
        [InlineData("99.22:22:22.1234567", typeof(TimeSpan))]
        [InlineData("http://www.bing.com", typeof(Uri))]
        // enum test
        [InlineData("Constructor", typeof(AttributeTargets))]
        [InlineData("CA761232-ED42-11CE-BACD-00AA0057B223", typeof(Guid))]
        public void CanReadAllSupportedTypes(string value, Type type)
        {
            // arrange
            var dic = new Dictionary<string, string>
            {
                {"Value", value}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);
            var expectedValue = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);

            // act
#pragma warning disable SYSLIB1104
            config.Bind(options);
            var optionsValue = options.GetType().GetProperty("Value").GetValue(options);
            var getValueValue = config.GetValue(type, "Value");
            var getValue = config.GetSection("Value").Get(type);
#pragma warning restore SYSLIB1104

            // assert
            Assert.Equal(expectedValue, optionsValue);
            Assert.Equal(expectedValue, getValue);
            Assert.Equal(expectedValue, getValueValue);
        }

        [ConditionalTheory(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Reflection fallback: generic type info not supported with source gen.
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(char))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(AttributeTargets))]
        [InlineData(typeof(Guid))]
        public void ConsistentExceptionOnFailedBinding(Type type)
        {
            // arrange
            const string IncorrectValue = "Invalid data";
            const string ConfigKey = "Value";
            var dic = new Dictionary<string, string>
            {
                {ConfigKey, IncorrectValue}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);

            // act
#pragma warning disable SYSLIB1104
            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            var getValueException = Assert.Throws<InvalidOperationException>(
                () => config.GetValue(type, "Value"));

            var getException = Assert.Throws<InvalidOperationException>(
                () => config.GetSection("Value").Get(type));
#pragma warning restore SYSLIB1104

            // assert
            Assert.NotNull(exception.InnerException);
            Assert.NotNull(getException.InnerException);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                exception.Message);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                getException.Message);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                getValueException.Message);
        }

        [Fact]
        public void ExceptionOnFailedBindingIncludesPath()
        {
            const string IncorrectValue = "Invalid data";
            const string ConfigKey = "Nested:Value";

            var dic = new Dictionary<string, string>
            {
                {ConfigKey, IncorrectValue}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new OptionsWithNesting();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            Assert.Equal(SR.Format(SR.Error_FailedBinding, ConfigKey, typeof(int)),
                exception.Message);
        }

        [Fact]
        public void BinderIgnoresIndexerProperties()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();
            config.Bind(new List<string>());
        }

        [Fact]
        public void BindCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();
            config.Bind(instance);

            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
        }

        [Fact]
        public void GetCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Fact]
        public void BindCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new DerivedOptions();
            config.Bind(instance);

            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
            Assert.Equal("Derived:Sup", instance.Virtual);
        }

        [Fact]
        public void GetCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new DerivedOptions();
            config.Bind(options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void GetCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Equal("stuff", ComplexOptions.StaticProperty);
        }

        [Fact]
        public void CanGetComplexOptionsWhichHasAlsoHasValue()
        {
            var dic = new Dictionary<string, string>
            {
                {"obj", "whut" },
                {"obj:Integer", "-2"},
                {"obj:Boolean", "TRUe"},
                {"obj:Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.GetSection("obj").Get<ComplexOptions>();
            Assert.NotNull(options);
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void GetIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>();
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("InternalReadOnly")]
        [InlineData("PrivateReadOnly")]
        [InlineData("ProtectedReadOnly")]
        public void NonPublicModeGetStillIgnoresReadonly(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

#if BUILDING_SOURCE_GENERATOR_TESTS
            var ex = Assert.Throws<NotSupportedException>(() => config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());
#else
            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
#endif
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void BindIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void BindCanSetNonPublicWhenSet(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();

#if BUILDING_SOURCE_GENERATOR_TESTS
            var ex = Assert.Throws<NotSupportedException>(() => config.Bind(options, o => o.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());
#else
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
#endif
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void GetCanSetNonPublicWhenSet(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

#if BUILDING_SOURCE_GENERATOR_TESTS
            var ex = Assert.Throws<NotSupportedException>(() => config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());

            ex = Assert.Throws<NotSupportedException>(() => config.Get(typeof(ComplexOptions), o => o.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());
#else
            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));

            options = (ComplexOptions)config.Get(typeof(ComplexOptions), o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
#endif
        }

        [Theory]
        [InlineData("InternalReadOnly")]
        [InlineData("PrivateReadOnly")]
        [InlineData("ProtectedReadOnly")]
        public void NonPublicModeBindStillIgnoresReadonly(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
#if BUILDING_SOURCE_GENERATOR_TESTS
            Assert.Throws<NotSupportedException>(() => config.Bind(options, o => o.BindNonPublicProperties = true));
#else
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
#endif
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void ExceptionWhenTryingToBindToInterface()
        {
            var input = new Dictionary<string, string>
            {
                {"ISomeInterfaceProperty:Subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(() => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ISomeInterface)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindClassWithoutParameterlessConstructor()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWithoutPublicConstructorProperty:Subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_MissingPublicInstanceConstructor, typeof(ClassWithoutPublicConstructor)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindClassWherePropertiesDoNotMatchConstructorParameters()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Name", "John"},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Address", "123, Abc St."},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersDoNotMatchProperties), "age"),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToConstructorWithMissingConfig()
        {
            var input = new Dictionary<string, string>
            {
                {"LineProperty:Color", "Red"},
                {"LineProperty:Length", "22"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ParameterHasNoMatchingConfig, typeof(Line), nameof(Line.Thickness)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindConfigToClassWhereNoMatchingParameterIsFoundInConstructor()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Name", "John"},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Address", "123, Abc St."},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersDoNotMatchProperties), "age"),
                exception.Message);
        }

        [Fact]
        public void BindsToClassConstructorParametersWithDefaultValues()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersHaveDefaultValueProperty:Name", "John"},
                {"ClassWhereParametersHaveDefaultValueProperty:Address", "123, Abc St."}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            TestOptions testOptions = new TestOptions();

            config.Bind(testOptions);
            Assert.Equal("John", testOptions.ClassWhereParametersHaveDefaultValueProperty.Name);
            Assert.Equal("123, Abc St.", testOptions.ClassWhereParametersHaveDefaultValueProperty.Address);
            Assert.Equal(42, testOptions.ClassWhereParametersHaveDefaultValueProperty.Age);
            Assert.Equal(42.0f, testOptions.ClassWhereParametersHaveDefaultValueProperty.F);
            Assert.Equal(3.14159, testOptions.ClassWhereParametersHaveDefaultValueProperty.D);
            Assert.Equal(3.1415926535897932384626433M, testOptions.ClassWhereParametersHaveDefaultValueProperty.M);
            Assert.Equal(StringComparison.Ordinal, testOptions.ClassWhereParametersHaveDefaultValueProperty.SC);
            Assert.Equal('q', testOptions.ClassWhereParametersHaveDefaultValueProperty.C);
            Assert.Equal(42, testOptions.ClassWhereParametersHaveDefaultValueProperty.NAge);
            Assert.Equal(42.0f, testOptions.ClassWhereParametersHaveDefaultValueProperty.NF);
            Assert.Equal(3.14159, testOptions.ClassWhereParametersHaveDefaultValueProperty.ND);
            Assert.Equal(3.1415926535897932384626433M, testOptions.ClassWhereParametersHaveDefaultValueProperty.NM);
            Assert.Equal(StringComparison.Ordinal, testOptions.ClassWhereParametersHaveDefaultValueProperty.NSC);
            Assert.Equal('q', testOptions.ClassWhereParametersHaveDefaultValueProperty.NC);
        }

        [Fact]
        public void FieldsNotSupported_ExceptionBindingToConstructorWithParameterMatchingAField()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Name", "John"},
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Address", "123, Abc St."},
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));

            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersMatchPropertiesAndFields), "age"),
                exception.Message);
        }

        [Fact]
        public void BindsToRecordPrimaryConstructorParametersWithDefaultValues()
        {
            var input = new Dictionary<string, string>
            {
                {"RecordWhereParametersHaveDefaultValueProperty:Name", "John"},
                {"RecordWhereParametersHaveDefaultValueProperty:Address", "123, Abc St."}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            TestOptions testOptions = new TestOptions();

            config.Bind(testOptions);
            Assert.Equal("John", testOptions.RecordWhereParametersHaveDefaultValueProperty.Name);
            Assert.Equal("123, Abc St.", testOptions.RecordWhereParametersHaveDefaultValueProperty.Address);
            Assert.Equal(42, testOptions.RecordWhereParametersHaveDefaultValueProperty.Age);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void ExceptionWhenTryingToBindToTypeThrowsWhenActivated()
        {
            var input = new Dictionary<string, string>
            {
                {"ThrowsWhenActivatedProperty:subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                SR.Format(SR.Error_FailedToActivate, typeof(ThrowsWhenActivated)),
                exception.Message);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void ExceptionIncludesKeyOfFailedBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NestedOptionsProperty:NestedOptions2Property:ISomeInterfaceProperty:subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ISomeInterface)),
                exception.Message);
        }

        [Fact]
        public void CanBindValueTypeOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"MyInt32", "42"},
                {"MyString", "hello world"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var options = config.Get<ValueTypeOptions>();
            Assert.Equal(42, options.MyInt32);
            Assert.Equal("hello world", options.MyString);
        }

        [Fact]
        public void CanBindImmutableClass()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ImmutableLengthAndColorClass>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindMutableClassWitNestedImmutableObject()
        {
            var dic = new Dictionary<string, string>
            {
                {"ContainerName", "Container123"},
                {"LengthAndColor:Length", "42"},
                {"LengthAndColor:Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ContainerWithNestedImmutableObject>();
            Assert.Equal("Container123", options.ContainerName);
            Assert.Equal(42, options.LengthAndColor.Length);
            Assert.Equal("Green", options.LengthAndColor.Color);
        }

        // If the immutable type has multiple public parameterized constructors, then throw
        // an exception.
        [Fact]
        public void CanBindImmutableClass_ThrowsOnMultipleParameterizedConstructors()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_MultipleParameterizedConstructors, typeof(ImmutableClassWithMultipleParameterizedConstructors));

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithMultipleParameterizedConstructors>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructor, then throw
        // that constructor has an 'in' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithAnInParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, typeof(ImmutableClassWithOneParameterizedConstructorButWithInParameter), "string1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithInParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // that constructor has a 'ref' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithARefParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, typeof(ImmutableClassWithOneParameterizedConstructorButWithRefParameter), "int1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithRefParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // if the constructor has an 'out' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithAnOutParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, typeof(ImmutableClassWithOneParameterizedConstructorButWithOutParameter), "int2");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithOutParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CanBindMutableStruct_UnmatchedConstructorsAreIgnored()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<MutableStructWithConstructor>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        // If the immutable type has a public parameterized constructor,
        // then pick it.
        [Fact]
        public void CanBindImmutableClass_PicksParameterizedConstructorIfNoParameterlessConstructorExists()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ImmutableClassWithOneParameterizedConstructor>();
            Assert.Equal("s1", options.String1);
            Assert.Equal("s2", options.String2);
            Assert.Equal(1, options.Int1);
            Assert.Equal(2, options.Int2);
        }

        [Fact]
        public void CanBindSemiImmutableClass()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
                {"Thickness", "1.23"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<SemiImmutableClass>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
            Assert.Equal(1.23m, options.Thickness);
        }

        [Fact]
        public void CanBindSemiImmutableClass_WithInitProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
                {"Thickness", "1.23"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<SemiImmutableClassWithInit>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
            Assert.Equal(1.23m, options.Thickness);
        }

        [Fact]
        public void CanBindRecordOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<RecordTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindClassWithPrimaryCtor()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ClassWithPrimaryCtor>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindClassWithPrimaryCtorWithDefaultValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "-1"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ClassWithPrimaryCtorDefaultValues>();
            Assert.Equal(-1, options.Length);
            Assert.Equal("blue", options.Color);
            Assert.Equal(5.946238490567943927384M, options.Height);
            Assert.Equal(EditorBrowsableState.Never, options.EB);
        }

        [Fact]
        public void CanBindRecordStructOptions()
        {
            IConfiguration config = GetConfiguration("Length", "Color");
            Validate(config.Get<RecordStructTypeOptions>());
            Validate(config.Get<RecordStructTypeOptions?>().Value);

            config = GetConfiguration("Options.Length", "Options.Color");
            // GetValue works for only primitives.
            //Reflection impl handles them by honoring `TypeConverter` only.
            // Source-gen supports based on an allow-list.
            Assert.Equal(default(RecordStructTypeOptions), config.GetValue<RecordStructTypeOptions>("Options"));
            Assert.False(config.GetValue<RecordStructTypeOptions?>("Options").HasValue);

            static void Validate(RecordStructTypeOptions options)
            {
                Assert.Equal(42, options.Length);
                Assert.Equal("Green", options.Color);
            }

            static IConfiguration GetConfiguration(string key1, string key2)
            {
                var dic = new Dictionary<string, string>
                {
                    { key1, "42" },
                    { key2, "Green" },
                };

                var configurationBuilder = new ConfigurationBuilder();
                configurationBuilder.AddInMemoryCollection(dic);
                return configurationBuilder.Build();
            }
        }

        [Fact]
        public void CanBindNestedRecordOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Number", "1"},
                {"Nested1:ValueA", "Cool"},
                {"Nested1:ValueB", "42"},
                {"Nested2:ValueA", "Uncool"},
                {"Nested2:ValueB", "24"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<RecordOptionsWithNesting>();
            Assert.Equal(1, options.Number);
            Assert.Equal("Cool", options.Nested1.ValueA);
            Assert.Equal(42, options.Nested1.ValueB);
            Assert.Equal("Uncool", options.Nested2.ValueA);
            Assert.Equal(24, options.Nested2.ValueB);
        }

        [Fact]
        public void CanBindOnParametersAndProperties_PropertiesAreSetAfterTheConstructor()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ClassWithMatchingParametersAndProperties>();
            Assert.Equal(42, options.Length);
            Assert.Equal("the color is Green", options.Color);
        }

        [Fact]
        public void CanBindReadonlyRecordStructOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ReadonlyRecordStructTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindByteArray()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", Convert.ToBase64String(bytes) }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var options = config.Get<ByteArrayOptions>();
            Assert.Equal(bytes, options.MyByteArray);
        }

        [Fact]
        public void CanBindByteArrayWhenValueIsNull()
        {
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", null }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ByteArrayOptions>();
            Assert.Null(options.MyByteArray);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToByteArray()
        {
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", "(not a valid base64 string)" }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<ByteArrayOptions>());
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, "MyByteArray", typeof(byte[])),
                exception.Message);
        }

        [Fact]
        public void DoesNotReadPropertiesUnnecessarily()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { nameof(ClassWithReadOnlyPropertyThatThrows.Safe), "value" },
                { nameof(ClassWithReadOnlyPropertyThatThrows.StringThrows), "value" },
                { $"{nameof(ClassWithReadOnlyPropertyThatThrows.EnumerableThrows)}:0", "0" },
            });
            IConfiguration config = configurationBuilder.Build();

            ClassWithReadOnlyPropertyThatThrows bound = config.Get<ClassWithReadOnlyPropertyThatThrows>();
            Assert.Equal("value", bound.Safe);
        }

        /// <summary>
        /// Binding to mutable structs is important to support properties
        /// like JsonConsoleFormatterOptions.JsonWriterOptions.
        /// </summary>
        [Fact]
        public void CanBindNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ReadWriteNestedStruct:String", "s" },
                { "ReadWriteNestedStruct:DeeplyNested:Int32", "100" },
                { "ReadWriteNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();

            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.Equal("s", bound.ReadWriteNestedStruct.String);
            Assert.Equal(100, bound.ReadWriteNestedStruct.DeeplyNested.Int32);
            Assert.True(bound.ReadWriteNestedStruct.DeeplyNested.Boolean);
        }

        [Fact]
        public void CanBindNestedStructProperties_SetterCalledWithMissingConfigEntry()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "dmy", "dmy" },
            });

            IConfiguration config = configurationBuilder.Build();

            var bound = config.Get<StructWithNestedStructAndSetterLogic>();
            Assert.Null(bound.String);
            Assert.Null(bound.NestedStruct.String);
            Assert.Equal(42, bound.Int32);
            Assert.Equal(0, bound.NestedStruct.Int32);
        }

        [Fact]
        public void CanBindNestedStructProperties_SetterNotCalledWithMissingConfigSection()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                // An empty value will not trigger defaulting.
            });

            IConfiguration config = configurationBuilder.Build();

            var bound = config.Get<StructWithNestedStructAndSetterLogic>();
            Assert.Null(bound.String);
            Assert.Null(bound.NestedStruct.String);
            Assert.Equal(0, bound.Int32);
            Assert.Equal(0, bound.NestedStruct.Int32);
        }

        [Fact]
        public void CanBindNestedStructProperties_SetterCalledWithMissingConfig_Array()
        {
            var config = TestHelpers.GetConfigurationFromJsonString(
                """{"value": [{ }]}""");

            var bound = config.GetSection("value").Get<StructWithNestedStructAndSetterLogic[]>();
            Assert.Null(bound[0].String);
            Assert.Equal(0, bound[0].Int32);
        }

        [Fact]
        public void IgnoresReadOnlyNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ReadOnlyNestedStruct:String", "s" },
                { "ReadOnlyNestedStruct:DeeplyNested:Int32", "100" },
                { "ReadOnlyNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();

            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.Null(bound.ReadOnlyNestedStruct.String);
            Assert.Equal(0, bound.ReadWriteNestedStruct.DeeplyNested.Int32);
            Assert.False(bound.ReadWriteNestedStruct.DeeplyNested.Boolean);
        }

        [Fact]
        public void CanBindNullableNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "NullableNestedStruct:String", "s" },
                { "NullableNestedStruct:DeeplyNested:Int32", "100" },
                { "NullableNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();
            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.NotNull(bound.NullableNestedStruct);
            Assert.Equal("s", bound.NullableNestedStruct.Value.String);
            Assert.Equal(100, bound.NullableNestedStruct.Value.DeeplyNested.Int32);
            Assert.True(bound.NullableNestedStruct.Value.DeeplyNested.Boolean);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need property selection in sync with reflection.
        public void CanBindVirtualProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"{nameof(BaseClassWithVirtualProperty.Test)}:0", "1" },
                { $"{nameof(BaseClassWithVirtualProperty.TestGetSetOverridden)}", "2" },
                { $"{nameof(BaseClassWithVirtualProperty.TestGetOverridden)}", "3" },
                { $"{nameof(BaseClassWithVirtualProperty.TestSetOverridden)}", "4" },
                { $"{nameof(BaseClassWithVirtualProperty.TestNoOverridden)}", "5" },
                { $"{nameof(BaseClassWithVirtualProperty.TestVirtualSet)}", "6" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();
            config.Bind(test);

            Assert.Equal("1", Assert.Single(test.Test));
            Assert.Equal("2", test.TestGetSetOverridden);
            Assert.Equal("3", test.TestGetOverridden);
            Assert.Equal("4", test.TestSetOverridden);
            Assert.Equal("5", test.TestNoOverridden);
            Assert.Null(test.ExposeTestVirtualSet());
        }

        [Fact]
        public void PrivatePropertiesFromBaseClass_Bind()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "PrivateProperty", "a" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();

#if BUILDING_SOURCE_GENERATOR_TESTS
            var ex = Assert.Throws<NotSupportedException>(() => config.Bind(test, b => b.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());
#else
            config.Bind(test, b => b.BindNonPublicProperties = true);
            Assert.Equal("a", test.ExposePrivatePropertyValue());
#endif
        }

        [Fact]
        public void PrivatePropertiesFromBaseClass_Get()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "PrivateProperty", "a" }
            });
            IConfiguration config = configurationBuilder.Build();

#if BUILDING_SOURCE_GENERATOR_TESTS
            var ex = Assert.Throws<NotSupportedException>(() => config.Get<ClassOverridingVirtualProperty>(b => b.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());

            ex = Assert.Throws<NotSupportedException>(() => config.Get(typeof(ClassOverridingVirtualProperty), b => b.BindNonPublicProperties = true));
            Assert.Contains("BinderOptions.BindNonPublicProperties", ex.ToString());
#else
            var test = config.Get<ClassOverridingVirtualProperty>(b => b.BindNonPublicProperties = true);
            Assert.Equal("a", test.ExposePrivatePropertyValue());

            test = (ClassOverridingVirtualProperty)config.Get(typeof(ClassOverridingVirtualProperty), b => b.BindNonPublicProperties = true);
            Assert.Equal("a", test.ExposePrivatePropertyValue());
#endif
        }

        [Fact]
        public void EnsureCallingThePropertySetter()
        {
            var json = @"{
                ""IPFiltering"": {
                    ""HttpStatusCode"": 401,
                    ""Blacklist"": [ ""192.168.0.10-192.168.10.20"", ""fe80::/10"" ]
                }
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(TestStreamHelpers.StringToStream(json))
                .Build();

            OptionWithCollectionProperties options = configuration.GetSection("IPFiltering").Get<OptionWithCollectionProperties>();

            Assert.NotNull(options);
            Assert.Equal(2, options.Blacklist.Count);
            Assert.Equal("192.168.0.10-192.168.10.20", options.Blacklist.ElementAt(0));
            Assert.Equal("fe80::/10", options.Blacklist.ElementAt(1));

            Assert.Equal(2, options.ParsedBlacklist.Count); // should be initialized when calling the options.Blacklist setter.

            Assert.Equal(401, options.HttpStatusCode); // exists in configuration and properly sets the property

            // This doesn't exist in configuration but the setter should be called which defaults the to '2' from input of '0'.
            Assert.Equal(2, options.OtherCode);

            // These don't exist in configuration and setters are not called since they are nullable.
            Assert.Equal(0, options.OtherCodeNullable);
            Assert.Equal("default", options.OtherCodeString);
            Assert.Null(options.OtherCodeNull);
            Assert.Null(options.OtherCodeUri);
        }

        [Fact]
        public void EnsureNotCallingSettersWhenGivenExistingInstanceNotInConfig()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new KeyValuePair<string, string?>[] { });
            var config = builder.Build();

            ClassThatThrowsOnSetters instance = new();

            // The setter for MyIntProperty throws, so this verifies that the setter is not called.
            config.GetSection("Dmy").Bind(instance);
            Assert.Equal(42, instance.MyIntProperty);
        }

        [Fact]
        public void EnsureSuccessfullyBind()
        {
            var json = @"{
                ""queueConfig"": {
                    ""Namespaces"": [
                        {
                            ""Namespace"": ""devnortheurope"",
                            ""Queues"": {
                                ""q1"": {
                                    ""DequeueOnlyMarkedDate"": ""2022-01-20T12:49:03.395150-08:00""
                                },
                                ""q2"": {
                                    ""DequeueOnlyMarkedDate"": ""2022-01-20T12:49:03.395150-08:00""
                                }
                            }
                        },
                        {
                            ""Namespace"": ""devnortheurope2"",
                            ""Queues"": {
                                ""q3"": {
                                    ""DequeueOnlyMarkedDate"": ""2022-01-20T12:49:03.395150-08:00""
                                },
                                ""q4"": {
                                }
                            }
                        }
                    ]
                }
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(TestStreamHelpers.StringToStream(json))
                .Build();

            DistributedQueueConfig options = new DistributedQueueConfig();
            configuration.GetSection("queueConfig").Bind(options);

            Assert.NotNull(options);
            Assert.Equal(2, options.Namespaces.Count);
            Assert.Equal(2, options.Namespaces.First().Queues.Count);
            Assert.Equal(2, options.Namespaces.Skip(1).First().Queues.Count);
            Assert.NotNull(options.Namespaces.Skip(1).First().Queues.Last().Value);
        }

        [Fact]
        public void RecursiveTypeGraphs_DirectRef()
        {
            var data = @"{
                ""MyString"":""Hello"",
                ""MyClass"": {
                    ""MyString"": ""World"",
                    ""MyClass"": {
                        ""MyString"": ""World"",
                        ""MyClass"": null
                    }
                }
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(TestStreamHelpers.StringToStream(data))
                .Build();

            var obj = configuration.Get<ClassWithDirectSelfReference>();
            Assert.Equal("Hello", obj.MyString);

            var nested = obj.MyClass;
            Assert.Equal("World", nested.MyString);

            var deeplyNested = nested.MyClass;
            Assert.Equal("World", deeplyNested.MyString);
            Assert.Null(deeplyNested.MyClass);
        }

        [Fact]
        public void RecursiveTypeGraphs_IndirectRef()
        {
            var data = @"{
                ""MyString"":""Hello"",
                ""MyList"": [{
                    ""MyString"": ""World"",
                    ""MyList"": [{
                        ""MyString"": ""World"",
                        ""MyClass"": null
                    }]
                }]
            }";

            var configuration = new ConfigurationBuilder()
                .AddJsonStream(TestStreamHelpers.StringToStream(data))
                .Build();

            var obj = configuration.Get<ClassWithIndirectSelfReference>();
            Assert.Equal("Hello", obj.MyString);

            var nested = obj.MyList[0];
            Assert.Equal("World", nested.MyString);

            var deeplyNested = nested.MyList[0];
            Assert.Equal("World", deeplyNested.MyString);
            Assert.Null(deeplyNested.MyList);
        }

        [Fact]
        public void TypeWithPrimitives_Pass()
        {
            var data = @"{
                ""Prop0"": true,
                ""Prop1"": 1,
                ""Prop2"": 2,
                ""Prop3"": ""C"",
                ""Prop4"": 3.2,
                ""Prop5"": ""Hello, world!"",
                ""Prop6"": 4,
                ""Prop8"": 9,
                ""Prop9"": 7,
                ""Prop10"": 2.3,
                ""Prop13"": 5,
                ""Prop14"": 10,
                ""Prop15"": 11,
                ""Prop16"": ""obj always parsed as string"",
                ""Prop17"": ""yo-NG"",
                ""Prop19"": ""2023-03-29T18:23:43.9977489+00:00"",
                ""Prop20"": ""2023-03-29T18:21:22.8046981+00:00"",
                ""Prop21"": 5.3,
                ""Prop23"": ""10675199.02:48:05.4775807"",
                ""Prop24"": ""e905a75b-d195-494d-8938-e55dcee44574"",
                ""Prop25"": ""https://microsoft.com"",
                ""Prop26"": ""4.3.2.1"",
            }";

            var configuration = TestHelpers.GetConfigurationFromJsonString(data);
            var obj = configuration.Get<RecordWithPrimitives>();

            Assert.True(obj.Prop0);
            Assert.Equal(1, obj.Prop1);
            Assert.Equal(2, obj.Prop2);
            Assert.Equal('C', obj.Prop3);
            Assert.Equal(3.2, obj.Prop4);
            Assert.Equal("Hello, world!", obj.Prop5);
            Assert.Equal(4, obj.Prop6);
            Assert.Equal(9, obj.Prop8);
            Assert.Equal(7, obj.Prop9);
            Assert.Equal((float)2.3, obj.Prop10);
            Assert.Equal(5, obj.Prop13);
            Assert.Equal((uint)10, obj.Prop14);
            Assert.Equal((ulong)11, obj.Prop15);
            Assert.Equal("obj always parsed as string", obj.Prop16);
            Assert.Equal(CultureInfo.GetCultureInfoByIetfLanguageTag("yo-NG"), obj.Prop17);
            Assert.Equal(DateTime.Parse("2023-03-29T18:23:43.9977489+00:00", CultureInfo.InvariantCulture), obj.Prop19);
            Assert.Equal(DateTimeOffset.Parse("2023-03-29T18:21:22.8046981+00:00", CultureInfo.InvariantCulture), obj.Prop20);
            Assert.Equal((decimal)5.3, obj.Prop21);
            Assert.Equal(TimeSpan.Parse("10675199.02:48:05.4775807", CultureInfo.InvariantCulture), obj.Prop23);
            Assert.Equal(Guid.Parse("e905a75b-d195-494d-8938-e55dcee44574"), obj.Prop24);
            Uri.TryCreate("https://microsoft.com", UriKind.RelativeOrAbsolute, out Uri? value);
            Assert.Equal(value, obj.Prop25);
#if BUILDING_SOURCE_GENERATOR_TESTS
            Assert.Equal(Version.Parse("4.3.2.1"), obj.Prop26);
#endif
            Assert.Equal(CultureInfo.GetCultureInfo("yo-NG"), obj.Prop17);

#if NETCOREAPP
            data = @"{
                ""Prop7"": 9,
                ""Prop11"": 65500,
                ""Prop12"": 34,
                ""Prop18"": ""2002-03-22"",
                ""Prop22"": ""18:26:38.7327436"",
            }";

            configuration = TestHelpers.GetConfigurationFromJsonString(data);
            configuration.Bind(obj);

            Assert.Equal((Int128)9, obj.Prop7);
            Assert.Equal((Half)65500, obj.Prop11);
            Assert.Equal((UInt128)34, obj.Prop12);
            Assert.Equal(DateOnly.Parse("2002-03-22"), obj.Prop18);
            Assert.Equal(TimeOnly.Parse("18:26:38.7327436"), obj.Prop22);
#endif
        }

        [Fact]
        public void ForClasses_ParameterlessConstructorIsPickedOverParameterized()
        {
            string data = """
                {
                    "MyInt": 9,
                }
                """;

            var configuration = TestHelpers.GetConfigurationFromJsonString(data);
            var obj = configuration.Get<ClassWithParameterlessAndParameterizedCtor>();
            Assert.Equal(1, obj.MyInt);
        }

        [Fact]
        public void ForStructs_ParameterlessConstructorIsPickedOverParameterized()
        {
            string data = """
                {
                    "MyInt": 10,
                }
                """;

            var configuration = TestHelpers.GetConfigurationFromJsonString(data);
            var obj = configuration.Get<ClassWithParameterlessAndParameterizedCtor>();
            Assert.Equal(1, obj.MyInt);
        }

        [Fact]
        public void BindRootStructIsNoOp()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "Int32": 9,
                    "Boolean": true,
                }
                """);

#pragma warning disable SYSLIB1103
            StructWithNestedStructs.DeeplyNested obj = new();
            configuration.Bind(obj);
            Assert.Equal(0, obj.Int32);
            Assert.False(obj.Boolean);

            StructWithNestedStructs.DeeplyNested? nullableObj = new();
            configuration.Bind(nullableObj);
            Assert.Equal(0, obj.Int32);
            Assert.False(obj.Boolean);
#pragma warning restore SYSLIB1103
        }

        [Fact]
        public void AllowsCaseInsensitiveMatch()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "vaLue": "MyString",
                }
                """);

            GenericOptions<string> obj = new();
            configuration.Bind(obj);
            Assert.Equal("MyString", obj.Value);

            GenericOptionsRecord<string> obj1 = configuration.Get<GenericOptionsRecord<string>>();
            Assert.Equal("MyString", obj1.Value);

            GenericOptionsWithParamCtor<string> obj2 = configuration.Get<GenericOptionsWithParamCtor<string>>();
            Assert.Equal("MyString", obj2.Value);
        }

        [Fact]
        public void ObjWith_TypeConverter()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "Location":
                    {
                        "Latitude": 3,
                        "Longitude": 4,
                    }
                }
                """);

            // TypeConverter impl is not honored (https://github.com/dotnet/runtime/issues/83599).

            GeolocationWrapper obj = configuration.Get<GeolocationWrapper>();
            ValidateGeolocation(obj.Location);

            configuration = TestHelpers.GetConfigurationFromJsonString(""" { "Geolocation": "3, 4", } """);
            obj = configuration.Get<GeolocationWrapper>();
            Assert.Equal(Geolocation.Zero, obj.Location);
        }

        [Fact]
        public void ComplexObj_As_Dictionary_Element()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "First":
                    {
                        "Latitude": 3,
                        "Longitude": 4,
                    }
                }
                """);

            Geolocation obj = configuration.Get<IDictionary<string, Geolocation>>()["First"];
            ValidateGeolocation(obj);
            obj = configuration.Get<IReadOnlyDictionary<string, Geolocation>>()["First"];
            ValidateGeolocation(obj);

            GeolocationClass obj1 = configuration.Get<IDictionary<string, GeolocationClass>>()["First"];
            ValidateGeolocation(obj1);
            obj1 = configuration.Get<IReadOnlyDictionary<string, GeolocationClass>>()["First"];
            ValidateGeolocation(obj1);

            GeolocationRecord obj2 = configuration.Get<IDictionary<string, GeolocationRecord>>()["First"];
            ValidateGeolocation(obj2);
            obj1 = configuration.Get<IReadOnlyDictionary<string, GeolocationClass>>()["First"];
            ValidateGeolocation(obj2);
        }

        [Fact]
        public void ComplexObj_As_Enumerable_Element()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""{ "Enumerable": [{ "Latitude": 3, "Longitude": 4 }] }""")
                .GetSection("Enumerable");

            Geolocation obj = configuration.Get<IList<Geolocation>>()[0];
            ValidateGeolocation(obj);

            obj = configuration.Get<Geolocation[]>()[0];
            ValidateGeolocation(obj);

            obj = configuration.Get<IReadOnlyList<Geolocation>>()[0];
            ValidateGeolocation(obj);
        }

#if !BUILDING_SOURCE_GENERATOR_TESTS
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotAppleMobile))]
        public void TraceSwitchTest()
        {
            var dic = new Dictionary<string, string>
            {
                {"TraceSwitch:Level", "Info"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            TraceSwitch ts = new(displayName: "TraceSwitch", description: "This switch is set via config.");
            ConfigurationBinder.Bind(config, "TraceSwitch", ts);
            Assert.Equal(TraceLevel.Info, ts.Level);
#if NETCOREAPP
            // Value property is not publicly exposed in .NET Framework.
            Assert.Equal("Info", ts.Value);
#endif // NETCOREAPP
        }
#endif

        private void ValidateGeolocation(IGeolocation location)
        {
            Assert.Equal(3, location.Latitude);
            Assert.Equal(4, location.Longitude);
        }

        [Fact]
#if !BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Investigate Build browser-wasm linux Release LibraryTests_EAT CI failure for reflection impl", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
#endif
        public void TestGraphWithUnsupportedMember()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""{ "WriterOptions": { "Indented": "true" } }""");
            var obj = new GraphWithUnsupportedMember();
            configuration.Bind(obj);
            Assert.True(obj.WriterOptions.Indented);

            // Encoder prop not supported; throw if there's config data.
            configuration = TestHelpers.GetConfigurationFromJsonString("""{ "WriterOptions": { "Indented": "true", "Encoder": { "Random": "" } } }""");
            Assert.Throws<InvalidOperationException>(() => configuration.Bind(obj));
        }

        [Fact]
        public void CanBindToObjectMembers()
        {
            var config = TestHelpers.GetConfigurationFromJsonString("""{ "Local": { "Authority": "Auth1" } }""");

            // Regression tests for https://github.com/dotnet/runtime/issues/89273 and https://github.com/dotnet/runtime/issues/89732.
            TestBind(options => config.Bind("Local", options.GenericProp), obj => obj.GenericProp);
            TestBind(options => config.GetSection("Local").Bind(options.NonGenericProp), obj => obj.NonGenericProp);
            TestBind(options => config.GetSection("Local").Bind(options._genericField, _ => { }), obj => obj._genericField);
            TestBind(options => config.Bind("Local", options._nonGenericField), obj => obj._nonGenericField);

            // Check statics.
            TestBind(options => config.GetSection("Local").Bind(RemoteAuthenticationOptions<OidcProviderOptions>.StaticGenericProp), obj => RemoteAuthenticationOptions<OidcProviderOptions>.StaticGenericProp);
            TestBind(options => config.GetSection("Local").Bind(RemoteAuthenticationOptions<OidcProviderOptions>.StaticNonGenericProp, _ => { }), obj => RemoteAuthenticationOptions<OidcProviderOptions>.StaticNonGenericProp);
            TestBind(options => config.Bind("Local", RemoteAuthenticationOptions<OidcProviderOptions>.s_GenericField), obj => RemoteAuthenticationOptions<OidcProviderOptions>.s_GenericField);
            TestBind(options => config.GetSection("Local").Bind(RemoteAuthenticationOptions<OidcProviderOptions>.s_NonGenericField), obj => RemoteAuthenticationOptions<OidcProviderOptions>.s_NonGenericField);

            // No null refs.
            config.GetSection("Local").Bind(new RemoteAuthenticationOptions<OidcProviderOptions>().NullGenericProp);
            config.GetSection("Local").Bind(RemoteAuthenticationOptions<OidcProviderOptions>.s_NullNonGenericField);

            static void TestBind(Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure, Func<RemoteAuthenticationOptions<OidcProviderOptions>, OidcProviderOptions> getBindedProp)
            {
                var obj = new RemoteAuthenticationOptions<OidcProviderOptions>();
                configure(obj);
                Assert.Equal("Auth1", getBindedProp(obj).Authority);
            }
        }

        [Fact]
        public void BinderSupportsObjCreationInput()
        {
            var configuration = new ConfigurationBuilder().Build();
            // No diagnostic warning SYSLIB1104.
            configuration.Bind(new GraphWithUnsupportedMember());
        }

        [Fact]
        public void TestNullHandling_Get()
        {
            // Null configuration.
            IConfiguration? configuration = null;

            Assert.Throws<ArgumentNullException>(() => configuration.Get<GeolocationClass>());
            Assert.Throws<ArgumentNullException>(() => configuration.Get<GeolocationClass>(_ => { }));
            Assert.Throws<ArgumentNullException>(() => configuration.Get<Geolocation>());
            Assert.Throws<ArgumentNullException>(() => configuration.Get<Geolocation>(_ => { }));

            // Null Type.
            configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");
#pragma warning disable SYSLIB1104 // The target type for a binder call could not be determined
            Assert.Throws<ArgumentNullException>(() => configuration.Get(type: null));
            Assert.Throws<ArgumentNullException>(() => configuration.Get(type: null, _ => { }));
#pragma warning restore SYSLIB1104 // The target type for a binder call could not be determined
        }

        [Fact]
        public void TestNullHandling_GetValue()
        {
            string key = "Longitude";

            // Null configuration.
            Test(configuration: null, key);

            // Null type.
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");
#pragma warning disable SYSLIB1104 // The target type for a binder call could not be determined
            Assert.Throws<ArgumentNullException>(() => configuration.GetValue(type: null, key));
            Assert.Throws<ArgumentNullException>(() => configuration.GetValue(type: null, key, defaultValue: null));
#pragma warning restore SYSLIB1104 // The target type for a binder call could not be determined

            // Null key.
            Test(configuration: configuration, key: null);

            void Test(IConfiguration? configuration, string? key)
            {
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue<GeolocationClass>(key));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue<GeolocationClass>(key, defaultValue: null));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue<Geolocation>(key));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue<Geolocation>(key, defaultValue: default));
                TestUntypedOverloads(configuration: null, key);
            }

            void TestUntypedOverloads(IConfiguration? configuration, string? key)
            {
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(GeolocationClass), key));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(GeolocationClass), key, defaultValue: null));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(GeolocationClass), key, new GeolocationClass()));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(Geolocation), key));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(Geolocation), key, defaultValue: null));
                Assert.Throws<ArgumentNullException>(() => configuration.GetValue(typeof(Geolocation), key, default(Geolocation)));
            }
        }

        [Fact]
        public void TestNullHandling_Bind()
        {
            // Null configuration.
            IConfiguration? configuration = null;
            GeolocationClass? location = new();
            Assert.Throws<ArgumentNullException>(() => configuration.Bind(location));
            Assert.Throws<ArgumentNullException>(() => configuration.Bind(location, _ => { }));
            Assert.Throws<ArgumentNullException>(() => configuration.Bind("", location));

            // Null object.
            configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Longitude"":1,""Latitude"":2}");
            location = null;
            // Expect no exceptions.
            configuration.Bind(location);
            configuration.Bind(location, _ => { });
            configuration.Bind("", location);
        }

        [Fact]
        public void TestAbstractTypeAsNestedMemberForBinding()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/91324.

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new KeyValuePair<string, string?>[]
                {
                     new KeyValuePair<string, string?>("ConfigBindRepro:EndPoints:0", "localhost"),
                     new KeyValuePair<string, string?>("ConfigBindRepro:Property", "true")
                })
                .Build();

            AClass settings = new();
            configuration.GetSection("ConfigBindRepro").Bind(settings);

            Assert.Empty(settings.EndPoints); // Need custom binding feature to map "localhost" string into Endpoint instance.
            Assert.True(settings.Property);
        }

        [Fact]
        public static void TestGettingAbstractType()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Value"":1}");
            Assert.Throws<InvalidOperationException>(() => configuration.Get<AbstractBase>());
        }

        [Fact]
        public static void TestBindingAbstractInstance()
        {
            // Regression tests for https://github.com/dotnet/runtime/issues/90974.
            // We only bind members on the declared binding type, i.e. AbstractBase, even
            // though the actual instances are derived types that may have their own properties.

            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{""Value"":1,""Value2"":2}");

            AbstractBase d = new Derived();
            configuration.Bind(d);
            Assert.Equal(1, d.Value);

            d = new DerivedWithAnotherProp();
            configuration.Bind(d);
            Assert.Equal(1, d.Value);

#if BUILDING_SOURCE_GENERATOR_TESTS
            // Divergence from reflection impl: reflection binds using instance type,
            // while src-gen can only use declared type (everything has to be known AOT).
            // This could change if we add an explicit API to indicate the expected runtime type(s).
            Assert.Equal(0, ((DerivedWithAnotherProp)d).Value2);
#else
            Assert.Equal(2, ((DerivedWithAnotherProp)d).Value2);
#endif
        }

        [Fact]
        public static void TestBindingAbstractMember_AsCtorParam()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{ ""AbstractProp"": {""Value"":1} }");
            Assert.Throws<InvalidOperationException>(configuration.Get<ClassWithAbstractCtorParam>);
            Assert.Throws<InvalidOperationException>(configuration.Get<ClassWithOptionalAbstractCtorParam>);
        }

        [Fact]
        public static void TestBindingInitializedAbstractMember()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{ ""AbstractProp"": {""Value"":1} }");
            ClassWithAbstractProp c = new();
            c.AbstractProp = new Derived();
            configuration.Bind(c);
            Assert.Equal(1, c.AbstractProp.Value);
        }

        [Fact]
        public static void TestBindingUninitializedAbstractMember()
        {
            IConfiguration configuration = TestHelpers.GetConfigurationFromJsonString(@"{ ""AbstractProp"": {""Value"":1} }");
            ClassWithAbstractProp c = new();
            c.AbstractProp = null;
            Assert.Throws<InvalidOperationException>(() => configuration.Bind(c));
        }

        [Fact]
        public void GetIConfigurationSection()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "vaLue": "MyString",
                }
                """);

            var obj = configuration.GetSection("value").Get<IConfigurationSection>();
            Assert.Equal("MyString", obj.Value);

            configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "vaLue": [ "MyString", { "nested": "value" } ],
                }
                """);

            var list = configuration.GetSection("value").Get<List<IConfigurationSection>>();
            ValidateList(list);

            var dict = configuration.Get<Dictionary<string, List<IConfigurationSection>>>();
            Assert.Equal(1, dict.Count);
            ValidateList(dict["vaLue"]);

            static void ValidateList(List<IConfigurationSection> list)
            {
                Assert.Equal(2, list.Count);
                Assert.Equal("0", list[0].Key);
                Assert.Equal("MyString", list[0].Value);

                Assert.Equal("1", list[1].Key);
                var nestedSection = Assert.IsAssignableFrom<IConfigurationSection>(list[1].GetSection("nested"));
                Assert.Equal("value", nestedSection.Value);
            }
        }

        [Fact]
        public void NullableDictKeys()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""{ "1": "MyString" }""");
            var dict = configuration.Get<Dictionary<int?, string>>();
            Assert.Empty(dict);
        }

        [Fact]
        public void IConfigurationSectionAsCtorParam()
        {
            var configuration = TestHelpers.GetConfigurationFromJsonString("""
                {
                    "MySection": "MySection",
                    "MyObject": "MyObject",
                    "MyString": "MyString",
                }
                """);

            var obj = configuration.Get<ClassWith_DirectlyAssignable_CtorParams>();
            Assert.Equal("MySection", obj.MySection.Value);
            Assert.Equal("MyObject", obj.MyObject);
            Assert.Equal("MyString", obj.MyString);
        }

        [Fact]
        public void SharedChildInstance()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new KeyValuePair<string, string?>[]
            {
                new("A:B:ConnectionString", "localhost"),
            });

            var config = builder.Build();

            SharedChildInstance_Class instance = new();
            config.GetSection("A:B").Bind(instance);
            Assert.Equal("localhost", instance.ConnectionString);

            // Binding to a new section should not set the value to null.
            config.GetSection("A").Bind(instance);
            Assert.Equal("localhost", instance.ConnectionString);
        }

        [Fact]
        public void CanBindToMockConfigurationSection()
        {
            const string expectedA = "hello";

            var configSource = new MemoryConfigurationSource()
            {
                InitialData = new Dictionary<string, string?>()
                {
                    [$":{nameof(SimplePoco.A)}"] = expectedA,
                }
            };
            var configRoot = new MockConfigurationRoot(new[] { configSource.Build(null) });
            var configSection = new ConfigurationSection(configRoot, string.Empty);

            SimplePoco result = new();
            configSection.Bind(result);

            Assert.Equal(expectedA, result.A);
            Assert.Equal(default(string), result.B);
        }

        // a mock configuration root that will return null for undefined Sections,
        // as is common when Configuration interfaces are mocked
        class MockConfigurationRoot : ConfigurationRoot, IConfigurationRoot
        {
            public MockConfigurationRoot(IList<IConfigurationProvider> providers) : base(providers)
            { }

            IConfigurationSection IConfiguration.GetSection(string key) =>
                this[key] is null ? null : new ConfigurationSection(this, key);
        }
    }
}
