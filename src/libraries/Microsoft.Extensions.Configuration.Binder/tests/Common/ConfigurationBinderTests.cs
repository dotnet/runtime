// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    public partial class ConfigurationBinderTests
    {
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for records.
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
        }

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

            Assert.True(childOptions.Boolean);
            Assert.Equal(-2, childOptions.Integer);
            Assert.Equal(11, childOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childOptions.Virtual);

            Assert.Equal("Section", options.Section.Key);
            Assert.Equal("Section", options.Section.Path);
            Assert.Null(options.Section.Value);
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

            Assert.Null(config.GetValue<bool?>("empty"));
            Assert.Null(config.GetValue<int?>("empty"));
        }

        [Fact]
        public void GetScalarNullable()
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

            Assert.False(config.GetValue<bool>("Boolean"));
            Assert.Equal(0, config.GetValue<int>("Integer"));
            Assert.Equal(0, config.GetValue<int>("Nested:Integer"));
            Assert.Null(config.GetValue<ComplexOptions>("Object"));
            Assert.False(config.GetSection("Boolean").Get<bool>());
            Assert.Equal(0, config.GetSection("Integer").Get<int>());
            Assert.Equal(0, config.GetSection("Nested:Integer").Get<int>());
            Assert.Null(config.GetSection("Object").Get<ComplexOptions>());
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need to honor binder options.
        public void ThrowsIfPropertyInConfigMissingInModel()
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need to honor binder options.
        public void ThrowsIfPropertyInConfigMissingInNestedModel()
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

        [Theory]
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
            config.Bind(options);
            var optionsValue = options.GetType().GetProperty("Value").GetValue(options);
            var getValueValue = config.GetValue(type, "Value");
            var getValue = config.GetSection("Value").Get(type);

            // assert
            Assert.Equal(expectedValue, optionsValue);
            Assert.Equal(expectedValue, getValue);
            Assert.Equal(expectedValue, getValueValue);
        }

        [Theory]
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
            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            var getValueException = Assert.Throws<InvalidOperationException>(
                () => config.GetValue(type, "Value"));

            var getException = Assert.Throws<InvalidOperationException>(
                () => config.GetSection("Value").Get(type));

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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
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
        public void BindCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "other stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();
            config.Bind(instance);

            Assert.Equal("other stuff", ComplexOptions.StaticProperty);
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

            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
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

            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
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
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
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
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
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

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ISomeInterface)),
                exception.Message);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
        public void ExceptionWhenTryingToBindClassWherePropertiesDoMatchConstructorParameters()
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void ExceptionWhenTryingToBindToConstructorWithMissingConfig() // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void ExceptionWhenTryingToBindConfigToClassWhereNoMatchingParameterIsFoundInConstructor() // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void BindsToClassConstructorParametersWithDefaultValues() // Need support for parameterized ctors.
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
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

            string expectedMessage = SR.Format(SR.Error_MultipleParameterizedConstructors, "Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests+ImmutableClassWithMultipleParameterizedConstructors");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithMultipleParameterizedConstructors>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructor, then throw
        // that constructor has an 'in' parameter
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithInParameter", "string1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithInParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // that constructor has a 'ref' parameter
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithRefParameter", "int1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithRefParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // if the constructor has an 'out' parameter
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Tests.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithOutParameter", "int2");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithOutParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
        public void CanBindRecordStructOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<RecordStructTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need support for parameterized ctors.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need collection support.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need to honor binder options.
        public void CanBindPrivatePropertiesFromBaseClass()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "PrivateProperty", "a" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();
            config.Bind(test, b => b.BindNonPublicProperties = true);
            Assert.Equal("a", test.ExposePrivatePropertyValue());
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Need collection support.
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
            Assert.Equal(2, options.OtherCode); // doesn't exist in configuration. the setter sets default value '2'
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

        public class ClassWithDirectSelfReference
        {
            public string MyString { get; set; }
            public ClassWithDirectSelfReference MyClass { get; set; }
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

        public class ClassWithIndirectSelfReference
        {
            public string MyString { get; set; }
            public List<ClassWithIndirectSelfReference> MyList { get; set; }
        }
    }
}
