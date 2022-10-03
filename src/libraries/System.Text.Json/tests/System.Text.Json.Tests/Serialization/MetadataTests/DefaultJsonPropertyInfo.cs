// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Nodes.Tests;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class DefaultJsonPropertyInfoTests_DefaultJsonTypeInfoResolver : DefaultJsonPropertyInfoTests
    {
        protected override IJsonTypeInfoResolver CreateResolverWithModifiers(params Action<JsonTypeInfo>[] modifiers)
        {
            var resolver = new DefaultJsonTypeInfoResolver();

            foreach (var modifier in modifiers)
            {
                resolver.Modifiers.Add(modifier);
            }

            return resolver;
        }
    }

    public class DefaultJsonPropertyInfoTests_SerializerContextNoWrapping : DefaultJsonPropertyInfoTests
    {
        protected override IJsonTypeInfoResolver CreateResolverWithModifiers(params Action<JsonTypeInfo>[] modifiers)
        {
            if (modifiers.Length != 0)
            {
                throw new SkipTestException($"Testing non wrapped JsonSerializerContext but modifier is provided.");
            }

            return Context.Default;
        }
    }

    public class DefaultJsonPropertyInfoTests_SerializerContextWrapped : DefaultJsonPropertyInfoTests
    {
        protected override IJsonTypeInfoResolver CreateResolverWithModifiers(params Action<JsonTypeInfo>[] modifiers)
            => new ContextWithModifiers(Context.Default, modifiers);

        private class ContextWithModifiers : IJsonTypeInfoResolver
        {
            private IJsonTypeInfoResolver _context;
            private Action<JsonTypeInfo>[] _modifiers;

            public ContextWithModifiers(JsonSerializerContext context, Action<JsonTypeInfo>[] modifiers)
            {
                _context = context;
                _modifiers = modifiers;
            }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo? typeInfo = _context.GetTypeInfo(type, options);
                Assert.NotNull(typeInfo);

                foreach (var modifier in _modifiers)
                {
                    modifier(typeInfo);
                }

                return typeInfo;
            }
        }
    }

    public abstract partial class DefaultJsonPropertyInfoTests
    {
        protected abstract IJsonTypeInfoResolver CreateResolverWithModifiers(params Action<JsonTypeInfo>[] modifiers);

        private JsonSerializerOptions CreateOptionsWithModifiers(params Action<JsonTypeInfo>[] modifiers)
            => new JsonSerializerOptions()
            {
                TypeInfoResolver = CreateResolverWithModifiers(modifiers)
            };

        private JsonSerializerOptions CreateOptions() => CreateOptionsWithModifiers();

        [Fact]
        public void RequiredAttributesGetDetectedAndFailDeserializationWhenValuesNotPresent()
        {
            JsonSerializerOptions options = CreateOptions();

            JsonTypeInfo? typeInfo = options.GetTypeInfo(typeof(ClassWithRequiredCustomAttributes));
            Assert.NotNull(typeInfo);

            Assert.Equal(3, typeInfo.Properties.Count);

            Assert.Equal(nameof(ClassWithRequiredCustomAttributes.NonRequired), typeInfo.Properties[0].Name);
            Assert.False(typeInfo.Properties[0].IsRequired);

            Assert.Equal(nameof(ClassWithRequiredCustomAttributes.RequiredA), typeInfo.Properties[1].Name);
            Assert.True(typeInfo.Properties[1].IsRequired);

            Assert.Equal(nameof(ClassWithRequiredCustomAttributes.RequiredB), typeInfo.Properties[2].Name);
            Assert.True(typeInfo.Properties[2].IsRequired);

            ClassWithRequiredCustomAttributes obj = new();
            string json = """{"NonRequired":null,"RequiredA":null,"RequiredB":null}""";
            Assert.Equal(json, JsonSerializer.Serialize(obj, options));

            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>(json, options);
            Assert.Null(deserialized.NonRequired);
            Assert.Null(deserialized.RequiredA);
            Assert.Null(deserialized.RequiredB);

            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("{}", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo"}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);


            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo", "RequiredB":"bar"}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo", "RequiredA":null}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);
        }

        [ConditionalFact]
        public void RequiredMemberCanBeModifiedToNonRequired()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributes))
                {
                    JsonPropertyInfo prop = ti.Properties[1];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributes.RequiredA), prop.Name);
                    Assert.True(prop.IsRequired);
                    prop.IsRequired = false;
                    Assert.False(prop.IsRequired);
                }
            });

            ClassWithRequiredCustomAttributes obj = new();
            string json = """{"NonRequired":null,"RequiredA":null,"RequiredB":null}""";
            Assert.Equal(json, JsonSerializer.Serialize(obj, options));

            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>(json, options);
            Assert.Null(deserialized.NonRequired);
            Assert.Null(deserialized.RequiredA);
            Assert.Null(deserialized.RequiredB);

            deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo", "RequiredB":"bar"}""", options);
            Assert.Equal("foo", deserialized.NonRequired);
            Assert.Null(deserialized.RequiredA);
            Assert.Equal("bar", deserialized.RequiredB);

            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("{}", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo"}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo", "RequiredA":null}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);
        }

        [ConditionalFact]
        public void NonRequiredMemberCanBeModifiedToRequired()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributes))
                {
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributes.NonRequired), prop.Name);
                    Assert.False(prop.IsRequired);
                    prop.IsRequired = true;
                    Assert.True(prop.IsRequired);
                }
            });

            ClassWithRequiredCustomAttributes obj = new();
            string json = """{"NonRequired":null,"RequiredA":null,"RequiredB":null}""";
            Assert.Equal(json, JsonSerializer.Serialize(obj, options));

            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>(json, options);
            Assert.Null(deserialized.NonRequired);
            Assert.Null(deserialized.RequiredA);
            Assert.Null(deserialized.RequiredB);

            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("{}", options));
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":"foo"}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"RequiredA":null}""", options));
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);

            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributes>("""{"NonRequired":null,"RequiredA":null}""", options));
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.NonRequired), exception.Message);
            Assert.DoesNotContain(nameof(ClassWithRequiredCustomAttributes.RequiredA), exception.Message);
            Assert.Contains(nameof(ClassWithRequiredCustomAttributes.RequiredB), exception.Message);
        }

        [Fact]
        public void RequiredExtensionDataPropertyThrows()
        {
            JsonSerializerOptions options = CreateOptions();
            ClassWithRequiredCustomAttributeAndDataExtensionProperty obj = new();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj, options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>("""{"Data":{}}""", options));
        }

        [ConditionalFact]
        public void RequiredExtensionDataPropertyCanBeFixedToNotBeRequiredWithResolver()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributeAndDataExtensionProperty))
                {
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributeAndDataExtensionProperty.Data), prop.Name);
                    Assert.True(prop.IsRequired);
                    prop.IsRequired = false;
                    Assert.False(prop.IsRequired);
                }
            });

            ClassWithRequiredCustomAttributeAndDataExtensionProperty obj = new()
            {
                Data = new Dictionary<string, object>()
                {
                    ["foo"] = "bar"
                }
            };

            string json = """{"foo":"bar"}""";
            Assert.Equal(json, JsonSerializer.Serialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>(obj, options));
            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>(json, options);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Data);
            Assert.Equal("bar", ((JsonElement)deserialized.Data["foo"]).GetString());
        }

        [ConditionalFact]
        public void RequiredExtensionDataPropertyCanBeFixedToNotBeExtensionDataWithResolver()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributeAndDataExtensionProperty))
                {
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributeAndDataExtensionProperty.Data), prop.Name);
                    Assert.True(prop.IsExtensionData);
                    prop.IsExtensionData = false;
                    Assert.False(prop.IsExtensionData);
                }
            });

            ClassWithRequiredCustomAttributeAndDataExtensionProperty obj = new()
            {
                Data = new Dictionary<string, object>()
                {
                    ["foo"] = "bar"
                }
            };

            string json = """{"Data":{"foo":"bar"}}""";
            Assert.Equal(json, JsonSerializer.Serialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>(obj, options));
            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>(json, options);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Data);
            Assert.Equal("bar", ((JsonElement)deserialized.Data["foo"]).GetString());
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndDataExtensionProperty>("{}", options));
        }

        [Fact]
        public void RequiredReadOnlyPropertyThrows()
        {
            JsonSerializerOptions options = CreateOptions();
            ClassWithRequiredCustomAttributeAndReadOnlyProperty obj = new();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj, options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>("""{"Data":{}}""", options));
        }

        [ConditionalFact]
        public void RequiredReadOnlyPropertyCanBeFixedToNotBeRequiredWithResolver()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributeAndReadOnlyProperty))
                {
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributeAndReadOnlyProperty.SomeProperty), prop.Name);
                    Assert.True(prop.IsRequired);
                    Assert.Null(prop.Set);
                    prop.IsRequired = false;
                    Assert.False(prop.IsRequired);
                }
            });

            ClassWithRequiredCustomAttributeAndReadOnlyProperty obj = new();

            string json = """{"SomeProperty":"SomePropertyInitialValue"}""";
            Assert.Equal(json, JsonSerializer.Serialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(obj, options));

            json = """{"SomeProperty":"SomeOtherValue"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal("SomePropertyInitialValue", deserialized.SomeProperty);

            json = "{}";
            deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal("SomePropertyInitialValue", deserialized.SomeProperty);
        }

        [ConditionalFact]
        public void RequiredReadOnlyPropertyCanBeFixedToBeWritableWithResolver()
        {
            JsonSerializerOptions options = CreateOptionsWithModifiers(ti =>
            {
                if (ti.Type == typeof(ClassWithRequiredCustomAttributeAndReadOnlyProperty))
                {
                    JsonPropertyInfo prop = ti.Properties[0];
                    Assert.Equal(nameof(ClassWithRequiredCustomAttributeAndReadOnlyProperty.SomeProperty), prop.Name);
                    Assert.True(prop.IsRequired);
                    Assert.Null(prop.Set);
                    prop.Set = (obj, value) => ((ClassWithRequiredCustomAttributeAndReadOnlyProperty)obj).SetSomeProperty((string)value);
                    Assert.NotNull(prop.Set);
                }
            });

            ClassWithRequiredCustomAttributeAndReadOnlyProperty obj = new();

            string json = """{"SomeProperty":"SomePropertyInitialValue"}""";
            Assert.Equal(json, JsonSerializer.Serialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(obj, options));

            json = """{"SomeProperty":"SomeOtherValue"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal("SomeOtherValue", deserialized.SomeProperty);

            json = "{}";
            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithRequiredCustomAttributeAndReadOnlyProperty>(json, options));
            Assert.Contains("SomeProperty", exception.Message);
        }

        [Fact]
        public void Polymorphic_BaseClassWithoutRequiredDerivedClassWithRequiredProperty()
        {
            JsonSerializerOptions options = CreateOptions();

            BaseClassWithoutRequiredProperties obj = new DerivedClassWithRequiredProperty()
            {
                NonRequiredProperty = "non-required",
                RequiredProperty = "required",
            };

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"$type":"derived","RequiredProperty":"required","NonRequiredProperty":"non-required"}""", json);

            var deserialized = JsonSerializer.Deserialize<BaseClassWithoutRequiredProperties>(json, options);
            Assert.Equal(obj.NonRequiredProperty, deserialized.NonRequiredProperty);
            Assert.Equal(((DerivedClassWithRequiredProperty)obj).RequiredProperty, ((DerivedClassWithRequiredProperty)deserialized).RequiredProperty);

            json = """{"$type":"derived","NonRequiredProperty":"non-required"}""";
            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseClassWithoutRequiredProperties>(json, options));
            Assert.Contains(nameof(DerivedClassWithRequiredProperty.RequiredProperty), exception.Message);
            Assert.DoesNotContain(nameof(BaseClassWithoutRequiredProperties.NonRequiredProperty), exception.Message);

            json = """{"NonRequiredProperty":"non-required"}""";
            deserialized = JsonSerializer.Deserialize<BaseClassWithoutRequiredProperties>(json, options);
            Assert.Equal(obj.NonRequiredProperty, deserialized.NonRequiredProperty);
            Assert.IsNotType<DerivedClassWithRequiredProperty>(deserialized);
        }

        [Fact]
        public void Polymorphic_BaseClassWithRequiredDerivedClassWithoutRequiredProperty()
        {
            JsonSerializerOptions options = CreateOptions();

            BaseClassWithRequiredProperties obj = new DerivedClassWithoutRequiredProperty()
            {
                NonRequiredProperty = "non-required",
                RequiredProperty = "required",
            };

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"$type":"derived","NonRequiredProperty":"non-required","RequiredProperty":"required"}""", json);

            var deserialized = JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options);
            Assert.Equal(obj.RequiredProperty, deserialized.RequiredProperty);
            Assert.Equal(((DerivedClassWithoutRequiredProperty)obj).NonRequiredProperty, ((DerivedClassWithoutRequiredProperty)deserialized).NonRequiredProperty);

            json = """{"$type":"derived","NonRequiredProperty":"non-required"}""";
            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options));
            Assert.Contains(nameof(BaseClassWithRequiredProperties.RequiredProperty), exception.Message);
            Assert.DoesNotContain(nameof(DerivedClassWithoutRequiredProperty.NonRequiredProperty), exception.Message);

            json = """{"RequiredProperty":"required"}""";
            deserialized = JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options);
            Assert.Equal(obj.RequiredProperty, deserialized.RequiredProperty);
            Assert.IsNotType<DerivedClassWithoutRequiredProperty>(deserialized);

            json = "{}";
            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options));
            Assert.Contains(nameof(BaseClassWithRequiredProperties.RequiredProperty), exception.Message);
        }

        [Fact]
        public void Polymorphic_BaseClassWithRequiredDerivedHidingRequiredPropertyWithNonRequired()
        {
            JsonSerializerOptions options = CreateOptions();

            JsonTypeInfo? typeInfo = options.GetTypeInfo(typeof(DerivedClassHidingRequiredPropertyWithNonRequired));
            Assert.NotNull(typeInfo);
            Assert.Equal(1, typeInfo.Properties.Count);

            JsonPropertyInfo propertyInfo = typeInfo.Properties[0];
            Assert.Equal(nameof(DerivedClassHidingRequiredPropertyWithNonRequired.RequiredProperty), propertyInfo.Name);
            Assert.False(propertyInfo.IsRequired);

            typeInfo = options.GetTypeInfo(typeof(BaseClassWithRequiredProperties));
            Assert.NotNull(typeInfo);
            Assert.Equal(1, typeInfo.Properties.Count);

            propertyInfo = typeInfo.Properties[0];
            Assert.Equal(nameof(BaseClassWithRequiredProperties.RequiredProperty), propertyInfo.Name);
            Assert.True(propertyInfo.IsRequired);

            BaseClassWithRequiredProperties obj = new DerivedClassHidingRequiredPropertyWithNonRequired()
            {
                RequiredProperty = "hidden-with-non-required",
            };

            obj.RequiredProperty = "base-required";

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"$type":"derived-hiding","RequiredProperty":"hidden-with-non-required"}""", json);

            var deserialized = JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options);
            Assert.Null(deserialized.RequiredProperty);
            Assert.Equal(((DerivedClassHidingRequiredPropertyWithNonRequired)obj).RequiredProperty, ((DerivedClassHidingRequiredPropertyWithNonRequired)deserialized).RequiredProperty);

            json = """{"$type":"derived-hiding"}""";
            deserialized = JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options);
            Assert.Null(deserialized.RequiredProperty);
            Assert.Null(((DerivedClassHidingRequiredPropertyWithNonRequired)deserialized).RequiredProperty);

            json = """{"RequiredProperty":"base-required"}""";
            deserialized = JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options);
            Assert.Equal(obj.RequiredProperty, deserialized.RequiredProperty);
            Assert.IsNotType<DerivedClassHidingRequiredPropertyWithNonRequired>(deserialized);

            json = "{}";
            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BaseClassWithRequiredProperties>(json, options));
            Assert.Contains(nameof(BaseClassWithRequiredProperties.RequiredProperty), exception.Message);
        }

        [Fact]
        public void StructWithRequiredPropertiesDoesWorkCorrectlyWithJsonRequiredCustomAttribute()
        {
            JsonSerializerOptions options = CreateOptions();

            StructWithRequiredProperties obj = new()
            {
                PropertyA = 123,
                PropertyB = 456,
            };

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"PropertyA":123,"PropertyB":456}""", json);

            var deserialized = JsonSerializer.Deserialize<StructWithRequiredProperties>(json, options);
            Assert.Equal(obj.PropertyA, deserialized.PropertyA);
            Assert.Equal(obj.PropertyB, deserialized.PropertyB);

            json = """{"PropertyA":123}""";
            JsonException exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithRequiredProperties>(json, options));
            Assert.DoesNotContain(nameof(StructWithRequiredProperties.PropertyA), exception.Message);
            Assert.Contains(nameof(StructWithRequiredProperties.PropertyB), exception.Message);

            json = "{}";
            exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithRequiredProperties>(json, options));
            Assert.Contains(nameof(StructWithRequiredProperties.PropertyA), exception.Message);
            Assert.Contains(nameof(StructWithRequiredProperties.PropertyB), exception.Message);
        }

        public class ClassWithRequiredCustomAttributes
        {
            [JsonPropertyOrder(0)]
            public string NonRequired { get; set; }

            [JsonPropertyOrder(1)]
            [JsonRequired]
            public string RequiredA { get; set; }

            [JsonPropertyOrder(2)]
            [JsonRequired]
            public string RequiredB { get; set; }
        }

        public class ClassWithRequiredCustomAttributeAndDataExtensionProperty
        {
            [JsonRequired]
            [JsonExtensionData]
            public Dictionary<string, object>? Data { get; set; }
        }

        public class ClassWithRequiredCustomAttributeAndReadOnlyProperty
        {
            [JsonRequired]
            public string SomeProperty { get; private set; } = "SomePropertyInitialValue";

            public void SetSomeProperty(string value)
            {
                SomeProperty = value;
            }
        }

        [JsonDerivedType(typeof(DerivedClassWithRequiredProperty), "derived")]
        public class BaseClassWithoutRequiredProperties
        {
            public string NonRequiredProperty { get; set; }
        }

        public class DerivedClassWithRequiredProperty : BaseClassWithoutRequiredProperties
        {
            [JsonRequired]
            public string RequiredProperty { get; set; }
        }

        [JsonDerivedType(typeof(DerivedClassWithoutRequiredProperty), "derived")]
        [JsonDerivedType(typeof(DerivedClassHidingRequiredPropertyWithNonRequired), "derived-hiding")]
        public class BaseClassWithRequiredProperties
        {
            [JsonRequired]
            public string RequiredProperty { get; set; }
        }

        public class DerivedClassWithoutRequiredProperty : BaseClassWithRequiredProperties
        {
            public string NonRequiredProperty { get; set; }
        }

        public class DerivedClassHidingRequiredPropertyWithNonRequired : BaseClassWithRequiredProperties
        {
            public new string RequiredProperty { get; set; }
        }

        public struct StructWithRequiredProperties
        {
            [JsonRequired]
            public int PropertyA { get; set; }

            [JsonRequired]
            public int PropertyB { get; set; }
        }

        [JsonSerializable(typeof(ClassWithRequiredCustomAttributes))]
        [JsonSerializable(typeof(ClassWithRequiredCustomAttributeAndDataExtensionProperty))]
        [JsonSerializable(typeof(ClassWithRequiredCustomAttributeAndReadOnlyProperty))]
        [JsonSerializable(typeof(BaseClassWithoutRequiredProperties))]
        [JsonSerializable(typeof(BaseClassWithRequiredProperties))]
        [JsonSerializable(typeof(StructWithRequiredProperties))]
        internal partial class Context : JsonSerializerContext
        {
        }
    }
}
