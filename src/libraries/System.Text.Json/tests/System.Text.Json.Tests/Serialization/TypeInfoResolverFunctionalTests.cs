// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class TypeInfoResolverFunctionalTests
    {
        [Fact]
        public static void AddPrefixToEveryPropertyOfClass()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        prop.Name = "renamed_" + prop.Name;
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""renamed_TestProperty"":42,""renamed_TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void AppendCharacterWhenSerializingField()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    // Because IncludeFields is false
                    Assert.Equal(1, ti.Properties.Count);
                    JsonPropertyInfo field = ti.CreateJsonPropertyInfo(typeof(string), "TestField");
                    field.Get = (o) =>
                    {
                        var obj = (TestClass)o;
                        return obj.TestField + "X";
                    };
                    field.Set = (o, val) =>
                    {
                        var obj = (TestClass)o;
                        var value = (string)val;
                        // We append 'X' on serialization
                        // therefore on deserialization we remove last character
                        obj.TestField = value.Substring(0, value.Length - 1);
                    };
                    ti.Properties.Add(field);
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":42,""TestField"":""test valueX""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void DoNotSerializeValue42()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.PropertyType == typeof(int))
                        {
                            prop.ShouldSerialize = (o, val) =>
                            {
                                return (int)val != 42;
                            };
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 43,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":43,""TestField"":""test value""}", json);

            originalObj.TestProperty = 42;
            json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(0, deserialized.TestProperty);
        }

        [Fact]
        public static void DoNotSerializePropertyWithNameButDeserializeIt()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClass.TestProperty))
                        {
                            prop.Get = null;
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestField"":""test value""}", json);

            json = @"{""TestProperty"":42,""TestField"":""test value""}";

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void DoNotDeserializePropertyWithNameButSerializeIt()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClass.TestProperty))
                        {
                            prop.Set = null;
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":42,""TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(0, deserialized.TestProperty);
        }

        [Fact]
        public static void SetCustomNumberHandlingForAProperty()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClass.TestProperty))
                        {
                            prop.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":""42"",""TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void SetCustomConverterForAProperty()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClass.TestProperty))
                        {
                            prop.CustomConverter = new PlusOneConverter();
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":43,""TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void UntypedCreateObjectWithDefaults()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    ti.CreateObject = () =>
                    {
                        return new TestClass()
                        {
                            TestField = "test value",
                            TestProperty = 42,
                        };
                    };
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value 2",
                TestProperty = 45,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":45,""TestField"":""test value 2""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);

            json = @"{}";
            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal("test value", deserialized.TestField);
            Assert.Equal(42, deserialized.TestProperty);

            json = @"{""TestField"":""test value 2""}";
            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(42, deserialized.TestProperty);
        }

        [Fact]
        public static void TypedCreateObjectWithDefaults()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    JsonTypeInfo<TestClass> typedTi = ti as JsonTypeInfo<TestClass>;
                    Assert.NotNull(typedTi);
                    typedTi.CreateObject = () =>
                    {
                        return new TestClass()
                        {
                            TestField = "test value",
                            TestProperty = 42,
                        };
                    };
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value 2",
                TestProperty = 45,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":45,""TestField"":""test value 2""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);

            json = @"{}";
            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal("test value", deserialized.TestField);
            Assert.Equal(42, deserialized.TestProperty);

            json = @"{""TestField"":""test value 2""}";
            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(42, deserialized.TestProperty);
        }

        [Fact]
        public static void SetCustomNumberHandlingForAType()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    ti.NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 42,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""TestProperty"":""42"",""TestField"":""test value""}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void CombineCustomResolverWithDefault()
        {
            TestResolver resolver = new TestResolver((Type type, JsonSerializerOptions options) =>
            {
                if (type != typeof(TestClass))
                    return null;

                JsonTypeInfo<TestClass> ti = JsonTypeInfo.CreateJsonTypeInfo<TestClass>(options);
                ti.CreateObject = () => new TestClass()
                {
                    TestField = string.Empty,
                    TestProperty = 42,
                };

                JsonPropertyInfo field = ti.CreateJsonPropertyInfo(typeof(string), "MyTestField");
                field.Get = (o) =>
                {
                    TestClass obj = (TestClass)o;
                    return obj.TestField ?? string.Empty;
                };

                field.Set = (o, val) =>
                {
                    TestClass obj = (TestClass)o;
                    string value = (string?)val ?? string.Empty;
                    obj.TestField = value;
                };

                field.ShouldSerialize = (o, val) => (string)val != string.Empty;

                JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(int), "MyTestProperty");
                prop.Get = (o) =>
                {
                    TestClass obj = (TestClass)o;
                    return obj.TestProperty;
                };

                prop.Set = (o, val) =>
                {
                    TestClass obj = (TestClass)o;
                    obj.TestProperty = (int)val;
                };

                prop.ShouldSerialize = (o, val) => (int)val != 42;

                ti.Properties.Add(field);
                ti.Properties.Add(prop);
                return ti;
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(resolver, options.TypeInfoResolver);

            TestClass originalObj = new TestClass()
            {
                TestField = "test value",
                TestProperty = 45,
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""MyTestField"":""test value"",""MyTestProperty"":45}", json);

            TestClass deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);

            originalObj.TestField = null;
            json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""MyTestProperty"":45}", json);

            originalObj.TestField = string.Empty;
            json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""MyTestProperty"":45}", json);

            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);

            originalObj.TestField = "test value";
            originalObj.TestProperty = 42;
            json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""MyTestField"":""test value""}", json);
            deserialized = JsonSerializer.Deserialize<TestClass>(json, options);
            Assert.Equal(originalObj.TestField, deserialized.TestField);
            Assert.Equal(originalObj.TestProperty, deserialized.TestProperty);
        }

        [Fact]
        public static void DataContractResolverScenario()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new DataContractResolver() };

            var value = new DataContractResolver.TestClass { String = "str", Boolean = true, Int = 42, Ignored = "ignored" };
            string json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{"intValue":42,"boolValue":true,"stringValue":"str"}""", json);

            DataContractResolver.TestClass result = JsonSerializer.Deserialize<DataContractResolver.TestClass>(json, options);
            Assert.Equal("str", result.String);
            Assert.Equal(42, result.Int);
            Assert.True(result.Boolean);
        }

        internal class DataContractResolver : DefaultJsonTypeInfoResolver
        {
            [DataContract]
            public class TestClass
            {
                [JsonIgnore] // ignored by the custom resolver
                [DataMember(Name = "stringValue", Order = 2)]
                public string String { get; set; }

                [JsonPropertyName("BOOL_VALUE")] // ignored by the custom resolver
                [DataMember(Name = "boolValue", Order = 1)]
                public bool Boolean { get; set; }

                [JsonPropertyOrder(int.MaxValue)] // ignored by the custom resolver
                [DataMember(Name = "intValue", Order = 0)]
                public int Int { get; set; }

                [IgnoreDataMember]
                public string Ignored { get; set; }
            }

            public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

                if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object &&
                    type.GetCustomAttribute<DataContractAttribute>() is not null)
                {
                    jsonTypeInfo.Properties.Clear(); // TODO should not require clearing

                    IEnumerable<(PropertyInfo propInfo, DataMemberAttribute attr)> properties = type
                        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(propInfo => propInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is null)
                        .Select(propInfo => (propInfo, attr: propInfo.GetCustomAttribute<DataMemberAttribute>()))
                        .OrderBy(entry => entry.attr?.Order ?? 0);

                    foreach ((PropertyInfo propertyInfo, DataMemberAttribute? attr) in properties)
                    {
                        JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propertyInfo.PropertyType, attr?.Name ?? propertyInfo.Name);
                        jsonPropertyInfo.Get =
                            propertyInfo.CanRead
                            ? propertyInfo.GetValue
                            : null;

                        jsonPropertyInfo.Set = propertyInfo.CanWrite
                            ? propertyInfo.SetValue
                            : null;

                        jsonTypeInfo.Properties.Add(jsonPropertyInfo);
                    }
                }

                return jsonTypeInfo;
            }
        }

        [Fact]
        public static void SpecifiedContractResolverScenario()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new SpecifiedContractResolver() };

            var value = new SpecifiedContractResolver.TestClass { String = "str", Int = 42 };
            string json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{}""", json);

            value.IntSpecified = true;
            json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{"Int":42}""", json);

            value.StringSpecified = true;
            json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{"String":"str","Int":42}""", json);
        }

        internal class SpecifiedContractResolver : DefaultJsonTypeInfoResolver
        {
            public class TestClass
            {
                public string String { get; set; }
                [JsonIgnore]
                public bool StringSpecified { get; set; }

                public int Int { get; set; }
                [JsonIgnore]
                public bool IntSpecified { get; set; }
            }
            public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

                foreach (JsonPropertyInfo property in jsonTypeInfo.Properties)
                {
                    PropertyInfo? specifiedProperty = type.GetProperty(property.Name + "Specified", BindingFlags.Instance | BindingFlags.Public);

                    if (specifiedProperty != null && specifiedProperty.CanRead && specifiedProperty.PropertyType == typeof(bool))
                    {
                        property.ShouldSerialize = (obj, _) => (bool)specifiedProperty.GetValue(obj);
                    }
                }

                return jsonTypeInfo;
            }
        }

        [Fact]
        public static void FieldContractResolverScenario()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new FieldContractResolver() };

            var value = FieldContractResolver.TestClass.Create("str", 42, true);
            string json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{"_string":"str","_int":42,"_bool":true}""", json);

            FieldContractResolver.TestClass result = JsonSerializer.Deserialize<FieldContractResolver.TestClass>(json, options);
            Assert.Equal(value, result);
        }

        internal class FieldContractResolver : DefaultJsonTypeInfoResolver
        {
            public class TestClass
            {
                private string _string;
                private int _int;
                private bool _bool;

                public static TestClass Create(string @string, int @int, bool @bool)
                    => new TestClass { _string = @string, _int = @int, _bool = @bool };

                // Should be ignored by the serializer
                public bool Boolean
                {
                    get => _bool;
                    set => throw new NotSupportedException();
                }

                public override int GetHashCode() => (_string, _int, _bool).GetHashCode();
                public override bool Equals(object? other)
                    => other is TestClass tc && (_string, _int, _bool) == (tc._string, tc._int, tc._bool);
            }

            public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

                if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object)
                {
                    jsonTypeInfo.Properties.Clear();

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
                        jsonPropertyInfo.Get = field.GetValue;
                        jsonPropertyInfo.Set = field.SetValue;

                        jsonTypeInfo.Properties.Add(jsonPropertyInfo);
                    }
                }

                return jsonTypeInfo;
            }
        }

        internal class TestClass
        {
            public int TestProperty { get; set; }
            public string TestField;
        }

        // adds one on write, subtracts one on read
        internal class PlusOneConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(typeof(int), typeToConvert);
                return reader.GetInt32() - 1;
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value + 1);
            }
        }
    }
}
