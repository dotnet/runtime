// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        public static void SetCustomConverterForIntProperty()
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
        public static void SetCustomConverterForListProperty()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClassWithLists))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClassWithLists.ListProperty1))
                        {
                            prop.CustomConverter = new AddListEntryConverter();
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClassWithLists originalObj = new TestClassWithLists()
            {
                ListProperty1 = new List<int> { 2, 3 },
                ListProperty2 = new List<int> { 4, 5, 6 },
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal(@"{""ListProperty1"":[2,3,-1],""ListProperty2"":[4,5,6]}", json);

            TestClassWithLists deserialized = JsonSerializer.Deserialize<TestClassWithLists>(json, options);
            Assert.Equal(originalObj.ListProperty1, deserialized.ListProperty1);
            Assert.Equal(originalObj.ListProperty2, deserialized.ListProperty2);
        }

        [Fact]
        public static void SetCustomConverterForDictionaryProperty()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClassWithDictionaries))
                {
                    Assert.Equal(JsonTypeInfoKind.Object, ti.Kind);
                    foreach (var prop in ti.Properties)
                    {
                        if (prop.Name == nameof(TestClassWithDictionaries.DictionaryProperty1))
                        {
                            prop.CustomConverter = new AddDictionaryEntryConverter();
                        }
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClassWithDictionaries originalObj = new TestClassWithDictionaries()
            {
                DictionaryProperty1 = new Dictionary<string, int>
                {
                    ["test1"] = 4,
                    ["test2"] = 5,
                },
                DictionaryProperty2 = new Dictionary<string, int>
                {
                    ["foo"] = 1,
                    ["bar"] = 8,
                },
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal("""{"DictionaryProperty1":{"test1":4,"test2":5,"*test*":-1},"DictionaryProperty2":{"foo":1,"bar":8}}""", json);

            TestClassWithDictionaries deserialized = JsonSerializer.Deserialize<TestClassWithDictionaries>(json, options);
            Assert.Equal(originalObj.DictionaryProperty1, deserialized.DictionaryProperty1);
            Assert.Equal(originalObj.DictionaryProperty2, deserialized.DictionaryProperty2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateObjectWithDefaults(bool useTypedCreateObject)
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(TestClass))
                {
                    Func<TestClass> createObj = () => new TestClass()
                    {
                        TestField = "test value",
                        TestProperty = 42,
                    };

                    if (useTypedCreateObject)
                    {
                        JsonTypeInfo<TestClass> typedTi = ti as JsonTypeInfo<TestClass>;
                        Assert.NotNull(typedTi);
                        typedTi.CreateObject = createObj;
                    }
                    else
                    {
                        // we want to make sure Func is not a cast to the untyped one
                        ti.CreateObject = () => createObj();
                    }
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateObjectForListWithDefaults(bool useTypedCreateObject)
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(List<int>))
                {
                    Func<List<int>> createObj = () => new List<int> { 99 };

                    if (useTypedCreateObject)
                    {
                        JsonTypeInfo<List<int>> typedTi = ti as JsonTypeInfo<List<int>>;
                        Assert.NotNull(typedTi);
                        typedTi.CreateObject = createObj;
                    }
                    else
                    {
                        // we want to make sure Func is not a cast to the untyped one
                        ti.CreateObject = () => createObj();
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClassWithLists originalObj = new TestClassWithLists()
            {
                ListProperty1 = new List<int> { 2, 3 },
                ListProperty2 = new List<int> { },
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal("""{"ListProperty1":[2,3],"ListProperty2":[]}""", json);

            TestClassWithLists deserialized = JsonSerializer.Deserialize<TestClassWithLists>(json, options);
            Assert.Equal(new List<int> { 99, 2, 3 }, deserialized.ListProperty1);
            Assert.Equal(new List<int> { 99 }, deserialized.ListProperty2);

            json = @"{}";
            deserialized = JsonSerializer.Deserialize<TestClassWithLists>(json, options);
            Assert.Null(deserialized.ListProperty1);
            Assert.Null(deserialized.ListProperty2);

            json = """{"ListProperty2":[ 123 ]}""";
            deserialized = JsonSerializer.Deserialize<TestClassWithLists>(json, options);
            Assert.Null(deserialized.ListProperty1);
            Assert.Equal(new List<int> { 99, 123 }, deserialized.ListProperty2);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateObjectForDictionaryWithDefaults(bool useTypedCreateObject)
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(Dictionary<string, int>))
                {
                    Func<Dictionary<string, int>> createObj = () => new Dictionary<string, int> { ["*test*"] = -1 };

                    if (useTypedCreateObject)
                    {
                        JsonTypeInfo<Dictionary<string, int>> typedTi = ti as JsonTypeInfo<Dictionary<string, int>>;
                        Assert.NotNull(typedTi);
                        typedTi.CreateObject = createObj;
                    }
                    else
                    {
                        // we want to make sure Func is not a cast to the untyped one
                        ti.CreateObject = () => createObj();
                    }
                }
            });

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IncludeFields = true;
            options.TypeInfoResolver = resolver;

            TestClassWithDictionaries originalObj = new()
            {
                DictionaryProperty1 = new Dictionary<string, int> { ["test1"] = 2, ["test2"] = 3 },
                DictionaryProperty2 = new Dictionary<string, int>(),
            };

            string json = JsonSerializer.Serialize(originalObj, options);
            Assert.Equal("""{"DictionaryProperty1":{"test1":2,"test2":3},"DictionaryProperty2":{}}""", json);

            TestClassWithDictionaries deserialized = JsonSerializer.Deserialize<TestClassWithDictionaries>(json, options);
            Assert.Equal(new Dictionary<string, int> { ["*test*"] = -1, ["test1"] = 2, ["test2"] = 3 }, deserialized.DictionaryProperty1);
            Assert.Equal(new Dictionary<string, int> { ["*test*"] = -1 }, deserialized.DictionaryProperty2);

            json = @"{}";
            deserialized = JsonSerializer.Deserialize<TestClassWithDictionaries>(json, options);
            Assert.Null(deserialized.DictionaryProperty1);
            Assert.Null(deserialized.DictionaryProperty2);

            json = """{"DictionaryProperty2":{"foo":123}}""";
            deserialized = JsonSerializer.Deserialize<TestClassWithDictionaries>(json, options);
            Assert.Null(deserialized.DictionaryProperty1);
            Assert.Equal(new Dictionary<string, int> { ["*test*"] = -1, ["foo"] = 123 }, deserialized.DictionaryProperty2);
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

            JsonSerializerOptions options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
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
                    jsonTypeInfo.Properties.Clear();

                    foreach (PropertyInfo propInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (propInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() is not null)
                        {
                            continue;
                        }

                        DataMemberAttribute? attr = propInfo.GetCustomAttribute<DataMemberAttribute>();
                        JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propInfo.PropertyType, attr?.Name ?? propInfo.Name);
                        jsonPropertyInfo.Order = attr?.Order ?? 0;
                        jsonPropertyInfo.Get =
                            propInfo.CanRead
                            ? propInfo.GetValue
                            : null;

                        jsonPropertyInfo.Set = propInfo.CanWrite
                            ? propInfo.SetValue
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

        internal class TestClassWithLists
        {
            public List<int> ListProperty1 { get; set; }
            public List<int> ListProperty2 { get; set; }
        }

        internal class TestClassWithDictionaries
        {
            public Dictionary<string, int> DictionaryProperty1 { get; set; }
            public Dictionary<string, int> DictionaryProperty2 { get; set; }
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

        // adds list entry in the end on write, removes one on read
        internal class AddListEntryConverter : JsonConverter<List<int>>
        {
            public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(typeof(List<int>), typeToConvert);
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);

                List<int> list = new();
                int? lastEntry = null;
                while (true)
                {
                    Assert.True(reader.Read());

                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (lastEntry.HasValue)
                    {
                        // note: we never add last entry
                        list.Add(lastEntry.Value);
                    }

                    Assert.Equal(JsonTokenType.Number, reader.TokenType);
                    lastEntry = reader.GetInt32();
                }

                Assert.True(lastEntry.HasValue);
                Assert.Equal(-1, lastEntry.Value);

                return list;
            }

            public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (int element in value)
                {
                    writer.WriteNumberValue(element);
                }

                writer.WriteNumberValue(-1);
                writer.WriteEndArray();
            }
        }

        // Adds extra dictionary entry on write, removes it on read
        internal class AddDictionaryEntryConverter : JsonConverter<Dictionary<string, int>>
        {
            public override Dictionary<string, int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(typeof(Dictionary<string, int>), typeToConvert);
                Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

                Dictionary<string, int> dict = new();
                KeyValuePair<string, int>? lastEntry = null;

                while (true)
                {
                    Assert.True(reader.Read());

                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (lastEntry.HasValue)
                    {
                        // note: we never add last entry
                        dict.Add(lastEntry.Value.Key, lastEntry.Value.Value);
                    }

                    Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
                    string? key = reader.GetString();
                    Assert.NotNull(key);
                    Assert.True(reader.Read());

                    Assert.Equal(JsonTokenType.Number, reader.TokenType);
                    lastEntry = new KeyValuePair<string, int>(key, reader.GetInt32());
                }

                Assert.True(lastEntry.HasValue);
                Assert.Equal("*test*", lastEntry.Value.Key);
                Assert.Equal(-1, lastEntry.Value.Value);

                return dict;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, int> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (var kv in value)
                {
                    writer.WritePropertyName(kv.Key);
                    writer.WriteNumberValue(kv.Value);
                }

                writer.WritePropertyName("*test*");
                writer.WriteNumberValue(-1);
                writer.WriteEndObject();
            }
        }
    }
}
