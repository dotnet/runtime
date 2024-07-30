// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class MetadataTests(JsonSerializerWrapper serializerUnderTest) : SerializerTests(serializerUnderTest)
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(string))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(Dictionary<Guid, int>))]
        [InlineData(typeof(ClassWithoutCtor))]
        [InlineData(typeof(StructWithDefaultCtor?))]
        [InlineData(typeof(IInterfaceWithProperties))]
        [InlineData(typeof(IDerivedInterface))]
        public void TypeWithoutConstructor_TypeInfoReportsNullCtorProvider(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);

            Assert.Null(typeInfo.ConstructorAttributeProvider);
        }

        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        public void TypeWithConstructor_TypeInfoReportsExpectedCtorProvider(Type typeWithCtor)
        {
            ConstructorInfo? expectedCtor = typeWithCtor.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(ctor => ctor.GetCustomAttribute<JsonConstructorAttribute>() is not null)
                .FirstOrDefault();

            Assert.NotNull(expectedCtor);

            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithCtor);

            Assert.Same(expectedCtor, typeInfo.ConstructorAttributeProvider);
        }

        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        public void TypeWithConstructor_SettingCtorDelegate_ResetsCtorAttributeProvider(Type typeWithCtor)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithCtor, mutable: true);
            Assert.NotNull(typeInfo.ConstructorAttributeProvider);

            typeInfo.CreateObject = () => Activator.CreateInstance(typeWithCtor);

            Assert.Null(typeInfo.ConstructorAttributeProvider);
        }

        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(ClassWithoutCtor))]
        [InlineData(typeof(IInterfaceWithProperties))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        [InlineData(typeof(IDerivedInterface))]
        public void JsonPropertyInfo_DeclaringType_HasExpectedValue(Type typeWithProperties)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithProperties);
            Assert.NotEmpty(typeInfo.Properties);

            Assert.All(typeInfo.Properties, propertyInfo =>
            {
                Assert.True(propertyInfo.DeclaringType.IsAssignableFrom(typeInfo.Type));
            });
        }

        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(ClassWithoutCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(IInterfaceWithProperties))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        [InlineData(typeof(IDerivedInterface))]
        public void JsonPropertyInfo_AttributeProvider_HasExpectedValue(Type typeWithProperties)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithProperties);
            Assert.NotEmpty(typeInfo.Properties);

            Assert.All(typeInfo.Properties, propertyInfo =>
            {
                MemberInfo? memberInfo = ResolveMember(typeWithProperties, propertyInfo.Name);
                Assert.Same(memberInfo, propertyInfo.AttributeProvider);
            });
        }

        [Theory]
        [InlineData(typeof(ClassWithoutCtor))]
        [InlineData(typeof(IInterfaceWithProperties))]
        [InlineData(typeof(IDerivedInterface))]
        public void TypeWithoutConstructor_JsonPropertyInfo_AssociatedParameter_IsNull(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            Assert.NotEmpty(typeInfo.Properties);

            Assert.All(typeInfo.Properties, propertyInfo => Assert.Null(propertyInfo.AssociatedParameter));
        }


        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        public void TypeWithConstructor_JsonPropertyInfo_AssociatedParameter_MatchesCtorParams(Type typeWithCtor)
        {
            ConstructorInfo? expectedCtor = typeWithCtor.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(ctor => ctor.GetCustomAttribute<JsonConstructorAttribute>() is not null)
                .FirstOrDefault();

            Assert.NotNull(expectedCtor);

            Dictionary<string, ParameterInfo> parameters = expectedCtor.GetParameters().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithCtor);

            Assert.All(typeInfo.Properties, jsonProperty =>
            {
                JsonParameterInfo? jsonParameter = jsonProperty.AssociatedParameter;

                if (!parameters.TryGetValue(jsonProperty.Name, out ParameterInfo? parameter))
                {
                    Assert.Null(jsonParameter);
                    return;
                }

                Assert.NotNull(jsonParameter);

                Assert.Equal(typeWithCtor, jsonParameter.DeclaringType);
                Assert.Equal(parameter.Position, jsonParameter.Position);
                Assert.Equal(parameter.ParameterType, jsonParameter.ParameterType);
                Assert.Equal(parameter.Name, jsonParameter.Name);
                Assert.Equal(parameter.Name, jsonProperty.Name, StringComparer.OrdinalIgnoreCase);

                Assert.Equal(parameter.HasDefaultValue, jsonParameter.HasDefaultValue);
                Assert.Equal(GetDefaultValue(parameter), jsonParameter.DefaultValue);
                Assert.Same(parameter, jsonParameter.AttributeProvider);
                Assert.Equal(jsonProperty.IsSetNullable, jsonParameter.IsNullable);
                Assert.False(jsonParameter.IsMemberInitializer);

                parameters.Remove(jsonProperty.Name);
            });

            Assert.Empty(parameters);
        }

        [Theory]
        [InlineData(typeof(ClassWithRequiredMember))]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        public void TypeWithRequiredOrInitMember_SourceGen_HasAssociatedParameterInfo(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            JsonPropertyInfo propertyInfo = typeInfo.Properties.Single();

            JsonParameterInfo? jsonParameter = propertyInfo.AssociatedParameter;

            if (Serializer.IsSourceGeneratedSerializer)
            {
                Assert.NotNull(jsonParameter);

                Assert.Equal(type, jsonParameter.DeclaringType);
                Assert.Equal(0, jsonParameter.Position);
                Assert.Equal(propertyInfo.PropertyType, jsonParameter.ParameterType);
                Assert.Equal(propertyInfo.Name, jsonParameter.Name);

                Assert.False(jsonParameter.HasDefaultValue);
                Assert.Null(jsonParameter.DefaultValue);
                Assert.Null(jsonParameter.AttributeProvider);
                Assert.Equal(propertyInfo.IsSetNullable, jsonParameter.IsNullable);
                Assert.True(jsonParameter.IsMemberInitializer);
            }
            else
            {
                Assert.Null(jsonParameter);
            }
        }

        [Theory]
        [InlineData(typeof(ClassWithDefaultCtor))]
        [InlineData(typeof(StructWithDefaultCtor))]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(ClassWithRequiredMember))]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        [InlineData(typeof(ClassWithMultipleConstructors))]
        [InlineData(typeof(DerivedClassWithShadowingProperties))]
        public void TypeWithConstructor_SettingCtorDelegate_ResetsAssociatedParameters(Type typeWithCtor)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeWithCtor, mutable: true);
            Assert.NotEmpty(typeInfo.Properties);

            typeInfo.CreateObject = () => Activator.CreateInstance(typeWithCtor);

            Assert.All(typeInfo.Properties, propertyInfo => Assert.Null(propertyInfo.AssociatedParameter));
        }

        [Theory]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(int?), null)]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(ClassWithDefaultCtor), null)]
        [InlineData(typeof(StructWithDefaultCtor), null)]
        [InlineData(typeof(StructWithDefaultCtor?), null)]
        [InlineData(typeof(string[]), null)]
        [InlineData(typeof(List<string>), null)]
        [InlineData(typeof(IList<string>), null)]
        [InlineData(typeof(ImmutableArray<string>), null)]
        [InlineData(typeof(DerivedList<int>), null)]
        [InlineData(typeof(DerivedListWithCustomConverter), null)]
        [InlineData(typeof(Dictionary<Guid, int>), typeof(Guid))]
        [InlineData(typeof(IReadOnlyDictionary<Guid, int>), typeof(Guid))]
        [InlineData(typeof(ImmutableDictionary<Guid, int>), typeof(Guid))]
        [InlineData(typeof(DerivedDictionary<int>), typeof(Guid))]
        [InlineData(typeof(DerivedDictionaryWithCustomConverter), null)]
        [InlineData(typeof(ArrayList), null)]
        [InlineData(typeof(Hashtable), typeof(string))]
        public void JsonTypeInfo_KeyType_ReturnsExpectedValue(Type type, Type? expectedKeyType)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            Assert.Equal(expectedKeyType, typeInfo.KeyType);
        }

        [Theory]
        [InlineData(typeof(int), null)]
        [InlineData(typeof(int?), typeof(int))]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(ClassWithDefaultCtor), null)]
        [InlineData(typeof(StructWithDefaultCtor), null)]
        [InlineData(typeof(StructWithDefaultCtor?), typeof(StructWithDefaultCtor))]
        [InlineData(typeof(string[]), typeof(string))]
        [InlineData(typeof(List<string>), typeof(string))]
        [InlineData(typeof(IList<string>), typeof(string))]
        [InlineData(typeof(ImmutableArray<string>), typeof(string))]
        [InlineData(typeof(DerivedList<int>), typeof((int, Guid)))]
        [InlineData(typeof(DerivedListWithCustomConverter), null)]
        [InlineData(typeof(Dictionary<Guid, int>), typeof(int))]
        [InlineData(typeof(IReadOnlyDictionary<Guid, int>), typeof(int))]
        [InlineData(typeof(ImmutableDictionary<Guid, int>), typeof(int))]
        [InlineData(typeof(DerivedDictionary<int>), typeof(int))]
        [InlineData(typeof(DerivedDictionaryWithCustomConverter), null)]
        [InlineData(typeof(ArrayList), typeof(object))]
        [InlineData(typeof(Hashtable), typeof(object))]
        public void JsonTypeInfo_ElementType_ReturnsExpectedValue(Type type, Type? expectedKeyType)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            Assert.Equal(expectedKeyType, typeInfo.ElementType);
        }

        [Theory]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(ClassWithRequiredAndOptionalConstructorParameters))]
        public void RespectRequiredConstructorParameters_false_ReportsCorrespondingPropertiesAsNotRequired(Type type)
        {
            var options = new JsonSerializerOptions { RespectRequiredConstructorParameters = false };
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, options);

            Assert.NotEmpty(typeInfo.Properties);
            Assert.All(typeInfo.Properties, property =>
            {
                Assert.False(property.IsRequired);
            });
        }

        [Theory]
        [InlineData(typeof(ClassWithParameterizedCtor))]
        [InlineData(typeof(StructWithParameterizedCtor))]
        [InlineData(typeof(ClassWithRequiredAndOptionalConstructorParameters))]
        public void RespectRequiredConstructorParameters_true_ReportsCorrespondingPropertiesAsRequired(Type type)
        {
            var options = new JsonSerializerOptions { RespectRequiredConstructorParameters = true };
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, options);

            Assert.NotEmpty(typeInfo.Properties);
            Assert.All(typeInfo.Properties, property =>
            {
                bool isRequiredParam = property.AssociatedParameter is { HasDefaultValue: false, IsMemberInitializer: false };
                Assert.Equal(isRequiredParam, property.IsRequired);
            });
        }

        private static object? GetDefaultValue(ParameterInfo parameterInfo)
        {
            Type parameterType = parameterInfo.ParameterType;
            object? defaultValue = parameterInfo.DefaultValue;

            if (defaultValue is null)
            {
                return null;
            }

            // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
            if (defaultValue == DBNull.Value && parameterType != typeof(DBNull))
            {
                return null;
            }

            // Default values of enums or nullable enums are represented using the underlying type and need to be cast explicitly
            // cf. https://github.com/dotnet/runtime/issues/68647
            if (parameterType.IsEnum)
            {
                return Enum.ToObject(parameterType, defaultValue);
            }

            if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType && underlyingType.IsEnum)
            {
                return Enum.ToObject(underlyingType, defaultValue);
            }

            return defaultValue;
        }

        private static MemberInfo? ResolveMember(Type type, string name)
        {
            MemberInfo? result = type.GetMember(name, BindingFlags.Instance | BindingFlags.Public).FirstOrDefault();
            if (result is null && type.IsInterface)
            {
                return type.GetInterfaces().Select(i => ResolveMember(i, name)).FirstOrDefault(m => m is not null);
            }

            return result;
        }

        internal class ClassWithoutCtor
        {
            private ClassWithoutCtor() { }

            public int Value { get; set; }
            public string Value2 { get; set; }

            [JsonInclude]
            public bool Value3 = true;
        }

        internal interface IInterfaceWithProperties
        {
            public int Value { get; set; }
            public string Value2 { get; set; }
        }

        internal struct StructWithDefaultCtor
        {
            [JsonConstructor]
            public StructWithDefaultCtor()
            {
            }

            public int Value { get; set; }
            public string Value2 { get; set; }

            [JsonInclude]
            public bool Value3 = true;
        }

        internal class ClassWithDefaultCtor
        {
            [JsonConstructor]
            public ClassWithDefaultCtor()
            {
            }

            public int Value { get; set; }
            public string Value2 { get; set; }

            [JsonInclude]
            public bool Value3 = true;
        }

        internal class ClassWithParameterizedCtor
        {
            [JsonConstructor]
            public ClassWithParameterizedCtor(
                string x1, int x2, bool x3, BindingFlags x4,
                string x5 = "str", int x6 = 42, bool x7 = true, BindingFlags? x8 = BindingFlags.Instance)
            {
                X1 = x1;
                X2 = x2;
                X3 = x3;
                X4 = x4;
                X5 = x5;
                X6 = x6;
                X7 = x7;
                X8 = x8;
            }

            public string X1 { get; }
            public int X2 { get; }
            public bool X3 { get; }
            public BindingFlags X4 { get; }
            public string X5 { get; }
            public int X6 { get; }
            public bool X7 { get; }

            [JsonInclude]
            public BindingFlags? X8;

            public string ExtraProperty { get; set; }
        }

        internal struct StructWithParameterizedCtor
        {
            [JsonConstructor]
            public StructWithParameterizedCtor(
                string x1, int x2, bool x3, BindingFlags x4,
                string x5 = "str", int x6 = 42, bool x7 = true, BindingFlags? x8 = BindingFlags.Instance)
            {
                X1 = x1;
                X2 = x2;
                X3 = x3;
                X4 = x4;
                X5 = x5;
                X6 = x6;
                X7 = x7;
                X8 = x8;
            }

            public string X1 { get; }
            public int X2 { get; }
            public bool X3 { get; }
            public BindingFlags X4 { get; }
            public string X5 { get; }
            public int X6 { get; }
            public bool X7 { get; }

            [JsonInclude]
            public BindingFlags? X8;

            public string ExtraProperty { get; set; }
        }

        internal class ClassWithRequiredMember
        {
            public required int Value { get; set; }
        }

        internal class ClassWithInitOnlyProperty
        {
            public int Value { get; init; }
        }

        internal class ClassWithMultipleConstructors
        {
            public ClassWithMultipleConstructors() { }

            public ClassWithMultipleConstructors(int x) { }

            [JsonConstructor]
            public ClassWithMultipleConstructors(string value)
            {
                Value = value;
            }

            public string Value { get; set; }
        }

        internal abstract class BaseClassWithProperties
        {
            public int Value1 { get; }
            public virtual int Value2 { get; set; }
            public abstract int Value3 { get; set; }
        }

        internal class DerivedClassWithShadowingProperties : BaseClassWithProperties
        {
            [JsonConstructor]
            public DerivedClassWithShadowingProperties(string value1, int value2, int value3)
            {
                Value1 = value1;
                Value2 = value2;
                Value3 = value3;
            }

            public new string Value1 { get; set; }
            public override int Value2 { get; set; }
            public override int Value3 { get; set; }
        }

        internal interface IBaseInterface1
        {
            int Value1 { get; }
        }

        internal interface IBaseInterface2
        {
            int Value2 { get; set; }
        }

        internal interface IDerivedInterface : IBaseInterface1, IBaseInterface2
        {
            new string Value2 { get; set; }
            int Value3 { get; set; }
        }

        internal class DerivedList<T> : List<(T, Guid)>;

        [JsonConverter(typeof(CustomConverter))]
        internal class DerivedListWithCustomConverter : List<Guid>
        {
            public sealed class CustomConverter : JsonConverter<DerivedListWithCustomConverter>
            {
                public override DerivedListWithCustomConverter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
                public override void Write(Utf8JsonWriter writer, DerivedListWithCustomConverter value, JsonSerializerOptions options) => throw new NotImplementedException();
            }
        }

        internal class DerivedDictionary<T> : Dictionary<Guid, T>;

        [JsonConverter(typeof(CustomConverter))]
        internal class DerivedDictionaryWithCustomConverter : Dictionary<Guid, string>
        {
            public sealed class CustomConverter : JsonConverter<DerivedDictionaryWithCustomConverter>
            {
                public override DerivedDictionaryWithCustomConverter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
                public override void Write(Utf8JsonWriter writer, DerivedDictionaryWithCustomConverter value, JsonSerializerOptions options) => throw new NotImplementedException();
            }
        }

        internal class ClassWithRequiredAndOptionalConstructorParameters
        {
            [JsonConstructor]
            public ClassWithRequiredAndOptionalConstructorParameters(string? x, string? y = null)
            {
                X = x;
                Y = y;
            }
            public string? X { get; }
            public string? Y { get; }
        }
    }

    internal class WeatherForecastWithPOCOs
    {
        public DateTimeOffset Date { get; set; }
        public int TemperatureCelsius { get; set; }
        public string? Summary { get; set; }
        public string? SummaryField;
        public List<DateTimeOffset>? DatesAvailable { get; set; }
        public Dictionary<string, HighLowTemps>? TemperatureRanges { get; set; }
        public string[]? SummaryWords { get; set; }
    }

    public class HighLowTemps
    {
        public int High { get; set; }
        public int Low { get; set; }
    }

    [JsonSerializable(typeof(WeatherForecastWithPOCOs))]
    internal sealed partial class JsonContext : JsonSerializerContext;
}
