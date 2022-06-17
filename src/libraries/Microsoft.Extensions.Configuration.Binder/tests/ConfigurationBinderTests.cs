// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.Test
{
    public class ConfigurationBinderTests
    {
        public class ComplexOptions
        {
            public ComplexOptions()
            {
                Nested = new NestedOptions();
                Virtual = "complex";
            }

            public NestedOptions Nested { get; set; }
            public int Integer { get; set; }
            public bool Boolean { get; set; }
            public virtual string Virtual { get; set; }
            public object Object { get; set; }

            public string PrivateSetter { get; private set; }
            public string ProtectedSetter { get; protected set; }
            public string InternalSetter { get; internal set; }
            public static string StaticProperty { get; set; }

            private string PrivateProperty { get; set; }
            internal string InternalProperty { get; set; }
            protected string ProtectedProperty { get; set; }

            [ConfigurationKeyName("Named_Property")]
            public string NamedProperty { get; set; }

            protected string ProtectedPrivateSet { get; private set; }

            private string PrivateReadOnly { get; }
            internal string InternalReadOnly { get; }
            protected string ProtectedReadOnly { get; }

            public string ReadOnly
            {
                get { return null; }
            }

            public IEnumerable<string> NonInstantiatedIEnumerable { get; set; } = null!;
            public IEnumerable<string> InstantiatedIEnumerable { get; set; } = new List<string>();
            public ICollection<string> InstantiatedICollection { get; set; } = new List<string>();
            public IReadOnlyCollection<string> InstantiatedIReadOnlyCollection { get; set; } = new List<string>();
        }

        public class NestedOptions
        {
            public int Integer { get; set; }
        }

        public class DerivedOptions : ComplexOptions
        {
            public override string Virtual
            {
                get
                {
                    return base.Virtual;
                }
                set
                {
                    base.Virtual = "Derived:" + value;
                }
            }
        }

        public class NullableOptions
        {
            public bool? MyNullableBool { get; set; }
            public int? MyNullableInt { get; set; }
            public DateTime? MyNullableDateTime { get; set; }
        }

        public class EnumOptions
        {
            public UriKind UriKind { get; set; }
        }

        public class GenericOptions<T>
        {
            public T Value { get; set; }
        }

        public class OptionsWithNesting
        {
            public NestedOptions Nested { get; set; }

            public class NestedOptions
            {
                public int Value { get; set; }
            }
        }

        public class ConfigurationInterfaceOptions
        {
            public IConfigurationSection Section { get; set; }
        }

        public class DerivedOptionsWithIConfigurationSection : DerivedOptions
        {
            public IConfigurationSection DerivedSection { get; set; }
        }

        public record struct RecordStructTypeOptions(string Color, int Length);

        // Here, the constructor has three parameters, but not all of those match
        // match to a property or field
        public class ClassWhereParametersDoNotMatchProperties
        {
            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersDoNotMatchProperties(string name, string address, int age)
            {
                Name = name;
                Address = address;
            }
        }

        // Here, the constructor has three parameters, and two of them match properties
        // and one of them match a field.
        public class ClassWhereParametersMatchPropertiesAndFields
        {
            private int Age;

            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersMatchPropertiesAndFields(string name, string address, int age)
            {
                Name = name;
                Address = address;
                Age = age;
            }

            public int GetAge() => Age;
        }

        public record RecordWhereParametersHaveDefaultValue(string Name, string Address, int Age = 42);

        public record ClassWhereParametersHaveDefaultValue
        {
            public string? Name { get; }
            public string Address { get; }
            public int Age { get; }

            public ClassWhereParametersHaveDefaultValue(string? name, string address, int age = 42)
            {
                Name = name;
                Address = address;
                Age = age;
            }
        }
        

        public record RecordTypeOptions(string Color, int Length);

        public record Line(string Color, int Length, int Thickness);

        public class ClassWithMatchingParametersAndProperties
        {
            private readonly string _color;

            public ClassWithMatchingParametersAndProperties(string Color, int Length)
            {
                _color = Color;
                this.Length = Length;
            }

            public int Length { get; set; }

            public string Color
            {
                get => _color;
                init => _color = "the color is " + value;
            }
        }

        public readonly record struct ReadonlyRecordStructTypeOptions(string Color, int Length);

        public class ContainerWithNestedImmutableObject
        {
            public string ContainerName { get; set; }
            public ImmutableLengthAndColorClass LengthAndColor { get; set; }
        }

        public struct MutableStructWithConstructor
        {
            public MutableStructWithConstructor(string randomParameter)
            {
                Color = randomParameter;
                Length = randomParameter.Length;
            }

            public string Color { get; set; }
            public int Length { get; set;  }
        }
        
        public class ImmutableLengthAndColorClass
        {
            public ImmutableLengthAndColorClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructor
        {
            public ImmutableClassWithOneParameterizedConstructor(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithInParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithInParameter(in string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithRefParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithRefParameter(string string1, ref int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithOutParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithOutParameter(string string1, int int1,
                string string2, out decimal int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                int2 = 0;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithMultipleParameterizedConstructors
        {
            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1)
            {
                String1 = string1;
                Int1 = int1;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1)
            {
                String1 = string1;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class SemiImmutableClass
        {
            public SemiImmutableClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; set; }
        }

        public class SemiImmutableClassWithInit
        {
            public SemiImmutableClassWithInit(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; init; }
        }

        public struct ValueTypeOptions
        {
            public int MyInt32 { get; set; }
            public string MyString { get; set; }
        }

        public class ByteArrayOptions
        {
            public byte[] MyByteArray { get; set; }
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
        public void CanBindNonInstantiatedIEnumerableWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedIEnumerable:0", "Yo1"},
                {"NonInstantiatedIEnumerable:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.NonInstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedIEnumerable.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIEnumerableWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIEnumerable:0", "Yo1"},
                {"InstantiatedIEnumerable:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.InstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIEnumerable.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedICollectionWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedICollection:0", "Yo1"},
                {"InstantiatedICollection:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedICollection.Count());
            Assert.Equal("Yo1", options.InstantiatedICollection.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedICollection.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIReadOnlyCollectionWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIReadOnlyCollection:0", "Yo1"},
                {"InstantiatedIReadOnlyCollection:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIReadOnlyCollection.Count);
            Assert.Equal("Yo1", options.InstantiatedIReadOnlyCollection.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIReadOnlyCollection.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIEnumerableWithNullItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIEnumerable:0", null},
                {"InstantiatedIEnumerable:1", "Yo1"},
                {"InstantiatedIEnumerable:2", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.InstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIEnumerable.ElementAt(1));
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

        [Fact]
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
        [Fact]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51211", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51211", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
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
            config.Bind(options, o => o.BindNonPublicProperties = true );
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

            string expectedMessage = SR.Format(SR.Error_MultipleParameterizedConstructors, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithMultipleParameterizedConstructors");

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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithInParameter", "string1");

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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithRefParameter", "int1");

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

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithOutParameter", "int2");

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

        [Fact]
        public void CanBindVirtualPropertiesWithoutDuplicates()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Test:0", "1" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();
            config.Bind(test);
            Assert.Equal("1", Assert.Single(test.Test));
        }


        private interface ISomeInterface
        {
        }

        private class ClassWithoutPublicConstructor
        {
            private ClassWithoutPublicConstructor()
            {
            }
        }

        private class ThrowsWhenActivated
        {
            public ThrowsWhenActivated()
            {
                throw new Exception();
            }
        }

        private class NestedOptions1
        {
            public NestedOptions2 NestedOptions2Property { get; set; }
        }

        private class NestedOptions2
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }
        }

        private class TestOptions
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }

            public ClassWithoutPublicConstructor ClassWithoutPublicConstructorProperty { get; set; }
            public ClassWhereParametersDoNotMatchProperties ClassWhereParametersDoNotMatchPropertiesProperty { get; set; }
            public Line LineProperty { get; set; }
            public ClassWhereParametersHaveDefaultValue ClassWhereParametersHaveDefaultValueProperty { get; set; }
            public ClassWhereParametersMatchPropertiesAndFields ClassWhereParametersMatchPropertiesAndFieldsProperty { get; set; }
            public RecordWhereParametersHaveDefaultValue RecordWhereParametersHaveDefaultValueProperty { get; set; }

            public int IntProperty { get; set; }

            public ThrowsWhenActivated ThrowsWhenActivatedProperty { get; set; }

            public NestedOptions1 NestedOptionsProperty { get; set; }
        }

        private class ClassWithReadOnlyPropertyThatThrows
        {
            public string StringThrows => throw new InvalidOperationException(nameof(StringThrows));

            public IEnumerable<int> EnumerableThrows => throw new InvalidOperationException(nameof(EnumerableThrows));

            public string Safe { get; set; }
        }

        private struct StructWithNestedStructs
        {
            public Nested ReadWriteNestedStruct { get; set; }

            public Nested ReadOnlyNestedStruct { get; }

            public Nested? NullableNestedStruct { get; set; }

            public struct Nested
            {
                public string String { get; set; }
                public DeeplyNested DeeplyNested { get; set; }
            }

            public struct DeeplyNested
            {
                public int Int32 { get; set; }
                public bool Boolean { get; set; }
            }
        }

        public class BaseClassWithVirtualProperty
        {
            public virtual string[] Test { get; set; } = System.Array.Empty<string>();
        }

        public class ClassOverridingVirtualProperty : BaseClassWithVirtualProperty
        {
            public override string[] Test { get => base.Test; set => base.Test = value; }
        }
    }
}
