// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class DefaultJsonTypeInfoResolverTests
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(SomeClass))]
        [InlineData(typeof(StructWithFourArgs))]
        [InlineData(typeof(Dictionary<string, int>))]
        [InlineData(typeof(DictionaryWrapper))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(ListWrapper))]
        public static void TypeInfoPropertiesDefaults(Type type)
        {
            bool usingParametrizedConstructor = type.GetConstructors()
                .FirstOrDefault(ctor => ctor.GetParameters().Length != 0 && ctor.GetCustomAttribute<JsonConstructorAttribute>() != null) != null;

            DefaultJsonTypeInfoResolver r = new();
            JsonSerializerOptions o = new();
            o.Converters.Add(new CustomThrowingConverter<SomeClass>());

            JsonTypeInfo ti = r.GetTypeInfo(type, o);

            Assert.Same(o, ti.Options);
            Assert.NotNull(ti.Properties);

            if (ti.Kind == JsonTypeInfoKind.Object && usingParametrizedConstructor)
            {
                Assert.Null(ti.CreateObject);
                Func<object> createObj = () => Activator.CreateInstance(type);
                ti.CreateObject = createObj;
                Assert.Same(createObj, ti.CreateObject);
            }
            else if (ti.Kind == JsonTypeInfoKind.None)
            {
                Assert.Null(ti.CreateObject);
                Assert.Throws<InvalidOperationException>(() => ti.CreateObject = () => Activator.CreateInstance(type));
            }
            else
            {
                Assert.NotNull(ti.CreateObject);
                Func<object> createObj = () => Activator.CreateInstance(type);
                ti.CreateObject = createObj;
                Assert.Same(createObj, ti.CreateObject);
            }

            JsonPropertyInfo property = ti.CreateJsonPropertyInfo(typeof(string), "foo");
            Assert.NotNull(property);

            if (ti.Kind == JsonTypeInfoKind.Object)
            {
                Assert.InRange(ti.Properties.Count, 1, 10);
                Assert.False(ti.Properties.IsReadOnly);
                ti.Properties.Add(property);
                ti.Properties.Remove(property);
            }
            else
            {
                Assert.Equal(0, ti.Properties.Count);
                Assert.True(ti.Properties.IsReadOnly);
                Assert.Throws<InvalidOperationException>(() => ti.Properties.Add(property));
                Assert.Throws<InvalidOperationException>(() => ti.Properties.Insert(0, property));
                Assert.Throws<InvalidOperationException>(() => ti.Properties.Clear());
            }

            Assert.Null(ti.NumberHandling);
            JsonNumberHandling numberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString;
            ti.NumberHandling = numberHandling;
            Assert.Equal(numberHandling, ti.NumberHandling);

            InvokeGeneric(type, nameof(TypeInfoPropertiesDefaults_Generic), ti);
        }

        private static void TypeInfoPropertiesDefaults_Generic<T>(JsonTypeInfo<T> ti)
        {
            if (ti.Kind == JsonTypeInfoKind.None)
            {
                Assert.Null(ti.CreateObject);
                Assert.Throws<InvalidOperationException>(() => ti.CreateObject = () => (T)Activator.CreateInstance(typeof(T)));
            }
            else
            {
                bool createObjCalled = false;
                Assert.NotNull(ti.CreateObject);
                Func<T> createObj = () =>
                {
                    createObjCalled = true;
                    return default(T);
                };

                ti.CreateObject = createObj;
                Assert.Same(createObj, ti.CreateObject);

                JsonTypeInfo untyped = ti;
                if (typeof(T).IsValueType)
                {
                    Assert.NotSame(createObj, untyped.CreateObject);
                }
                else
                {
                    Assert.Same(createObj, untyped.CreateObject);
                }

                Assert.Same(untyped.CreateObject, untyped.CreateObject);
                Assert.Same(createObj, ti.CreateObject);
                untyped.CreateObject();
                Assert.True(createObjCalled);

                ti.CreateObject = null;
                Assert.Null(ti.CreateObject);
                Assert.Null(untyped.CreateObject);

                bool untypedCreateObjCalled = false;
                Func<object> untypedCreateObj = () =>
                {
                    untypedCreateObjCalled = true;
                    return default(T);
                };
                untyped.CreateObject = untypedCreateObj;
                Assert.Same(untypedCreateObj, untyped.CreateObject);
                Assert.Same(ti.CreateObject, ti.CreateObject);
                Assert.NotSame(untypedCreateObj, ti.CreateObject);

                ti.CreateObject();
                Assert.True(untypedCreateObjCalled);

                untyped.CreateObject = null;
                Assert.Null(ti.CreateObject);
                Assert.Null(untyped.CreateObject);
            }
        }

        [Fact]
        public static void TypeInfoKindNoneNumberHandlingDirect()
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(int))
                {
                    ti.NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            string json = JsonSerializer.Serialize(13, o);
            Assert.Equal(@"""13""", json);

            var deserialized = JsonSerializer.Deserialize<int>(json, o);
            Assert.Equal(13, deserialized);
        }

        [Fact]
        public static void TypeInfoKindNoneNumberHandlingDirectThroughObject()
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(int))
                {
                    ti.NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            string json = JsonSerializer.Serialize<object>(13, o);
            Assert.Equal(@"""13""", json);

            var deserialized = JsonSerializer.Deserialize<object>(json, o);
            Assert.Equal("13", ((JsonElement)deserialized).GetString());
        }

        [Fact]
        public static void TypeInfoKindNoneNumberHandling()
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(int) || ti.Type == typeof(object))
                {
                    ti.NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            SomeClass testObj = new SomeClass()
            {
                ObjProp = 45,
                IntProp = 13,
            };

            string json = JsonSerializer.Serialize(testObj, o);
            Assert.Equal(@"{""ObjProp"":""45"",""IntProp"":""13""}", json);

            var deserialized = JsonSerializer.Deserialize<SomeClass>(json, o);
            Assert.Equal(testObj.ObjProp.ToString(), ((JsonElement)deserialized.ObjProp).GetString());
            Assert.Equal(testObj.IntProp, deserialized.IntProp);
        }

        [Fact]
        public static void RecursiveTypeNumberHandling()
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(SomeRecursiveClass))
                {
                    ti.NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            SomeRecursiveClass testObj = new SomeRecursiveClass()
            {
                IntProp = 13,
                RecursiveProperty = new SomeRecursiveClass()
                {
                    IntProp = 14,
                },
            };

            string json = JsonSerializer.Serialize(testObj, o);
            Assert.Equal(@"{""IntProp"":""13"",""RecursiveProperty"":{""IntProp"":""14"",""RecursiveProperty"":null}}", json);

            var deserialized = JsonSerializer.Deserialize<SomeRecursiveClass>(json, o);
            Assert.Equal(testObj.IntProp, deserialized.IntProp);
            Assert.NotNull(testObj.RecursiveProperty);
            Assert.Equal(testObj.RecursiveProperty.IntProp, deserialized.RecursiveProperty.IntProp);
            Assert.Null(testObj.RecursiveProperty.RecursiveProperty);
        }

        [Theory]
        [InlineData(typeof(SomeClass), typeof(object))]
        [InlineData(typeof(object), typeof(string))]
        [InlineData(typeof(object), typeof(int))]
        [InlineData(typeof(string), typeof(int))]
        [InlineData(typeof(int), typeof(string))]
        [InlineData(typeof(int), typeof(double))]
        public static void TypeInfoOfWrongTypeOnObject(Type expectedType, Type actualType)
        {
            DefaultJsonTypeInfoResolver dr = new();
            TestResolver r = new((type, options) =>
            {
                if (type == expectedType)
                {
                    return dr.GetTypeInfo(actualType, options);
                }

                return dr.GetTypeInfo(type, options);
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            SomeClass testObj = new()
            {
                ObjProp = "test",
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(testObj, o));
        }

        [Fact]
        public static void TypeInfoOfWrongOptions()
        {
            JsonSerializerOptions wrongOptions = new();
            DefaultJsonTypeInfoResolver dr = new();
            TestResolver r = new((type, options) =>
            {
                if (type == typeof(int))
                {
                    return dr.GetTypeInfo(type, wrongOptions);
                }

                return dr.GetTypeInfo(type, options);
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            SomeClass testObj = new()
            {
                IntProp = 17,
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(testObj, o));
        }

        [Theory]
        [InlineData(typeof(SomeClass), typeof(object))]
        [InlineData(typeof(object), typeof(string))]
        [InlineData(typeof(object), typeof(int))]
        [InlineData(typeof(int), typeof(string))]
        [InlineData(typeof(int), typeof(double))]
        public static void TypeInfoOfWrongTypeDirectCall(Type expectedType, Type actualType)
        {
            DefaultJsonTypeInfoResolver dr = new();
            TestResolver r = new((type, options) =>
            {
                if (type == expectedType)
                {
                    return dr.GetTypeInfo(actualType, options);
                }

                return dr.GetTypeInfo(type, options);
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            object testObj = Activator.CreateInstance(expectedType);

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(testObj, expectedType, o));
        }

        [Theory]
        [MemberData(nameof(GetTypeInfoTestData))]
        public static void TypeInfoIsImmutableAfterFirstUsage<T>(T testObj)
        {
            JsonTypeInfo untyped = null;
            DefaultJsonTypeInfoResolver dr = new();
            TestResolver r = new((typeToResolve, options) =>
            {
                var ret = dr.GetTypeInfo(typeToResolve, options);
                if (typeToResolve == typeof(T))
                {
                    Assert.Null(untyped);
                    untyped = ret;
                }

                return ret;
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            Assert.NotNull(JsonSerializer.Serialize(testObj, typeof(T), o));
            Assert.NotNull(untyped);

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)untyped;

            if (typeInfo.Kind == JsonTypeInfoKind.None)
            {
                Assert.Null(typeInfo.CreateObject);
                Assert.Null(untyped.CreateObject);
            }
            else
            {
                Assert.NotNull(typeInfo.CreateObject);
                Assert.NotNull(untyped.CreateObject);
            }

            Assert.Null(typeInfo.NumberHandling);

            TestTypeInfoImmutability(typeInfo);
        }

        private static void TestTypeInfoImmutability<T>(JsonTypeInfo<T> typeInfo)
        {
            JsonTypeInfo untyped = typeInfo;
            Assert.Equal(typeof(T), typeInfo.Type);
            Assert.True(typeInfo.Converter.CanConvert(typeof(T)));

            JsonPropertyInfo prop = typeInfo.CreateJsonPropertyInfo(typeof(string), "foo");
            Assert.Throws<InvalidOperationException>(() => untyped.CreateObject = untyped.CreateObject);
            Assert.Throws<InvalidOperationException>(() => typeInfo.CreateObject = typeInfo.CreateObject);
            Assert.Throws<InvalidOperationException>(() => typeInfo.NumberHandling = typeInfo.NumberHandling);
            Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Clear());
            Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Add(prop));
            Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Insert(0, prop));

            foreach (var property in typeInfo.Properties)
            {
                Assert.NotNull(property.PropertyType);
                Assert.Null(property.CustomConverter);
                Assert.NotNull(property.Name);
                Assert.NotNull(property.Get);
                Assert.NotNull(property.Set);
                Assert.Null(property.ShouldSerialize);
                Assert.Null(typeInfo.NumberHandling);

                Assert.Throws<InvalidOperationException>(() => property.CustomConverter = property.CustomConverter);
                Assert.Throws<InvalidOperationException>(() => property.Name = property.Name);
                Assert.Throws<InvalidOperationException>(() => property.Get = property.Get);
                Assert.Throws<InvalidOperationException>(() => property.Set = property.Set);
                Assert.Throws<InvalidOperationException>(() => property.ShouldSerialize = property.ShouldSerialize);
                Assert.Throws<InvalidOperationException>(() => property.NumberHandling = property.NumberHandling);
            }
        }

        [Theory]
        [InlineData(typeof(object), JsonTypeInfoKind.None)]
        [InlineData(typeof(string), JsonTypeInfoKind.None)]
        [InlineData(typeof(int), JsonTypeInfoKind.None)]
        [InlineData(typeof(SomeRecursiveClass) /* custom converter */, JsonTypeInfoKind.None)]
        [InlineData(typeof(SomeClass), JsonTypeInfoKind.Object)]
        [InlineData(typeof(DefaultJsonTypeInfoResolverTests), JsonTypeInfoKind.Object)]
        [InlineData(typeof(StructWithFourArgs), JsonTypeInfoKind.Object)]
        [InlineData(typeof(Dictionary<string, string>), JsonTypeInfoKind.Dictionary)]
        [InlineData(typeof(DictionaryWrapper), JsonTypeInfoKind.Dictionary)]
        [InlineData(typeof(List<int>), JsonTypeInfoKind.Enumerable)]
        [InlineData(typeof(ListWrapper), JsonTypeInfoKind.Enumerable)]
        [InlineData(typeof(int[]), JsonTypeInfoKind.Enumerable)]
        public static void JsonTypeInfoKindIsReportedCorrectly(Type type, JsonTypeInfoKind expectedJsonTypeInfoKind)
        {
            InvokeGeneric(type, nameof(JsonTypeInfoKindIsReportedCorrectly_Generic), expectedJsonTypeInfoKind);
        }

        private static void JsonTypeInfoKindIsReportedCorrectly_Generic<T>(JsonTypeInfoKind expectedJsonTypeInfoKind)
        {
            DefaultJsonTypeInfoResolver r = new();
            JsonSerializerOptions o = new();
            o.Converters.Add(new CustomThrowingConverter<SomeRecursiveClass>());
            JsonTypeInfo ti = r.GetTypeInfo(typeof(T), o);
            Assert.Equal(expectedJsonTypeInfoKind, ti.Kind);

            ti = JsonTypeInfo.CreateJsonTypeInfo(typeof(T), o);
            Assert.Equal(expectedJsonTypeInfoKind, ti.Kind);

            ti = JsonTypeInfo.CreateJsonTypeInfo<T>(o);
            Assert.Equal(expectedJsonTypeInfoKind, ti.Kind);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void JsonTypeInfoAddDuplicatedPropertyNames(bool ignoreDuplicatedProperty)
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(MyClass))
                {
                    JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(uint), ti.Properties[0].Name);
                    uint valueHolder = 7;

                    if (!ignoreDuplicatedProperty)
                    {
                        prop.Get = (o) => valueHolder;
                        prop.Set = (o, val) => valueHolder = (uint)val;
                    }

                    ti.Properties.Add(prop);
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            MyClass obj = new()
            {
                Value = "foo",
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize<MyClass>(obj, o));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void JsonTypeInfoRenameToDuplicatePropertyNames(bool ignoreDuplicatedProperty)
        {
            DefaultJsonTypeInfoResolver r = new();
            r.Modifiers.Add((ti) =>
            {
                if (ti.Type == typeof(MyClass))
                {
                    if (ignoreDuplicatedProperty)
                    {
                        ti.Properties[1].Get = null;
                        ti.Properties[1].Set = null;
                    }

                    ti.Properties[1].Name = ti.Properties[0].Name;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = r;

            MyClass obj = new()
            {
                Value = "foo",
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize<MyClass>(obj, o));
        }

        [Fact]
        public static void AddJsonPropertyInfoCreatedFromDifferentJsonTypeInfoInstance()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            JsonSerializerOptions options = new();
            JsonTypeInfo[] typeInfos = new[]
            {
                // we add double so that we check between instances of the same internal type as well
                JsonTypeInfo.CreateJsonTypeInfo<SomeClass>(options),
                JsonTypeInfo.CreateJsonTypeInfo<SomeClass>(options),
                JsonTypeInfo.CreateJsonTypeInfo<SomeOtherClass>(options),
                resolver.GetTypeInfo(typeof(SomeClass), options),
                resolver.GetTypeInfo(typeof(SomeClass), options),
                resolver.GetTypeInfo(typeof(SomeOtherClass), options),
                ((IJsonTypeInfoResolver)new SomeClassContext()).GetTypeInfo(typeof(SomeClass), options),
                ((IJsonTypeInfoResolver)new SomeClassContext()).GetTypeInfo(typeof(SomeClass), options),
                ((IJsonTypeInfoResolver)new SomeClassContext()).GetTypeInfo(typeof(SomeOtherClass), options),
                new SomeClassContext(options).SomeClass // this binds to options and therefore we cannot add more of these
            };

            foreach (var typeInfo1 in typeInfos)
            {
                foreach (var typeInfo2 in typeInfos)
                {
                    if (ReferenceEquals(typeInfo1, typeInfo2))
                        continue;

                    Assert.Throws<InvalidOperationException>(() => typeInfo1.Properties.Add(typeInfo2.CreateJsonPropertyInfo(typeof(int), "test")));
                }
            }
        }

        [Fact]
        public static void AddJsonPropertyInfoFromMetadataServices()
        {
            JsonSerializerOptions options = new();
            JsonTypeInfo typeInfo1 = JsonTypeInfo.CreateJsonTypeInfo<SomeClass>(options);
            JsonTypeInfo typeInfo2 = JsonTypeInfo.CreateJsonTypeInfo<SomeClass>(options);

            JsonPropertyInfo propertyInfo = JsonMetadataServices.CreatePropertyInfo<int>(
                options,
                new JsonPropertyInfoValues<int>()
                {
                    DeclaringType = typeof(SomeClass),
                    PropertyName = "test",
                });

            typeInfo1.Properties.Add(propertyInfo);
            Assert.Equal(1, typeInfo1.Properties.Count);
            Assert.Same(propertyInfo, typeInfo1.Properties[0]);

            Assert.Throws<InvalidOperationException>(() => typeInfo2.Properties.Add(propertyInfo));
            Assert.Equal(0, typeInfo2.Properties.Count);
        }

        [Fact]
        public static void AddingNullJsonPropertyInfoIsNotPossible()
        {
            JsonSerializerOptions options = new();
            JsonTypeInfo typeInfo = JsonTypeInfo.CreateJsonTypeInfo<SomeClass>(options);
            Assert.Throws<ArgumentNullException>(() => typeInfo.Properties.Add(null));
            Assert.Empty(typeInfo.Properties);
            Assert.Throws<ArgumentNullException>(() => typeInfo.Properties.Insert(0, null));
            Assert.Empty(typeInfo.Properties);

            typeInfo.Properties.Add(typeInfo.CreateJsonPropertyInfo(typeof(int), "test"));
            Assert.Throws<ArgumentNullException>(() => typeInfo.Properties[0] = null);
            Assert.Equal(1, typeInfo.Properties.Count);
            Assert.NotNull(typeInfo.Properties[0]);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(SomeRecursiveClass))]
        [InlineData(typeof(SomeClass))]
        [InlineData(typeof(DefaultJsonTypeInfoResolverTests))]
        [InlineData(typeof(StructWithFourArgs))]
        [InlineData(typeof(Dictionary<string, string>))]
        [InlineData(typeof(DictionaryWrapper))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(ListWrapper))]
        [InlineData(typeof(int[]))]
        public static void CreateJsonTypeInfo(Type type)
        {
            InvokeGeneric(type, nameof(CreateJsonTypeInfo_Generic));
        }

        private static void CreateJsonTypeInfo_Generic<T>()
        {
            TestCreateJsonTypeInfo((o) => (JsonTypeInfo<T>)JsonTypeInfo.CreateJsonTypeInfo(typeof(T), o));
            TestCreateJsonTypeInfo((o) => JsonTypeInfo.CreateJsonTypeInfo<T>(o));

            static void TestCreateJsonTypeInfo(Func<JsonSerializerOptions, JsonTypeInfo<T>> getTypeInfo)
            {
                JsonSerializerOptions o = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
                TestCreateJsonTypeInfoInstance(o, getTypeInfo(o));

                o = new JsonSerializerOptions() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
                var conv = new DummyConverter<T>();
                o.Converters.Add(conv);
                JsonTypeInfo<T> ti = getTypeInfo(o);
                Assert.Same(conv, ti.Converter);
                Assert.Equal(JsonTypeInfoKind.None, ti.Kind);
                TestCreateJsonTypeInfoInstance(o, ti);
            }

            static void TestCreateJsonTypeInfoInstance(JsonSerializerOptions o, JsonTypeInfo<T> ti)
            {
                Assert.Equal(typeof(T), ti.Type);
                Assert.NotNull(ti.Converter);
                Assert.True(ti.Converter.CanConvert(typeof(T)));

                JsonSerializer.Serialize(default(T), ti);

                JsonTypeInfo untyped = ti;
                Assert.Null(ti.CreateObject);
                Assert.Null(untyped.CreateObject);

                TestTypeInfoImmutability(ti);
            }
        }

        public static IEnumerable<object[]> GetTypeInfoTestData()
        {
            yield return new object[] { "test" };
            yield return new object[] { 13 };
            yield return new object[] { new SomeClass { IntProp = 17 } };
            yield return new object[] { new SomeRecursiveClass() };
        }

        [Fact]
        public static void JsonConstructorAttributeIsOverriddenWhenCreateObjectIsSet()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(ClassWithParametrizedConstructorAndReadOnlyProperties))
                {
                    Assert.Null(ti.CreateObject);
                    ti.CreateObject = () => new ClassWithParametrizedConstructorAndReadOnlyProperties(1, "test", dummyParam: true);
                }
            });

            JsonSerializerOptions o = new() { TypeInfoResolver = resolver };
            string json = """{"A":2,"B":"foo"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithParametrizedConstructorAndReadOnlyProperties>(json, o);

            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized.A);
            Assert.Equal("test", deserialized.B);
        }

        private class ClassWithParametrizedConstructorAndReadOnlyProperties
        {
            public int A { get; }
            public string B { get; }

            public ClassWithParametrizedConstructorAndReadOnlyProperties(int a, string b, bool dummyParam)
            {
                A = a;
                B = b;
            }

            [JsonConstructor]
            public ClassWithParametrizedConstructorAndReadOnlyProperties(int a, string b)
            {
                Assert.Fail("this ctor should not be used");
            }
        }

        [Fact]
        public static void JsonConstructorAttributeIsOverridenAndPropertiesAreSetWhenCreateObjectIsSet()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(ClassWithParametrizedConstructorAndWritableProperties))
                {
                    Assert.Null(ti.CreateObject);
                    ti.CreateObject = () => new ClassWithParametrizedConstructorAndWritableProperties();
                }
            });

            JsonSerializerOptions o = new() { TypeInfoResolver = resolver };

            string json = """{"A":2,"B":"foo","C":"bar"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithParametrizedConstructorAndWritableProperties>(json, o);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.A);
            Assert.Equal("foo", deserialized.B);
            Assert.Equal("bar", deserialized.C);
        }

        private class ClassWithParametrizedConstructorAndWritableProperties
        {
            public int A { get; set; }
            public string B { get; set; }
            public string C { get; set; }

            public ClassWithParametrizedConstructorAndWritableProperties() { }

            [JsonConstructor]
            public ClassWithParametrizedConstructorAndWritableProperties(int a, string b)
            {
                Assert.Fail("this ctor should not be used");
            }
        }

        [Fact]
        public static void SerializingTypeWithCustomNonSerializablePropertyAndJsonConstructorWorksCorrectly()
        {
            var resolver = new DefaultJsonTypeInfoResolver { Modifiers = { ContractModifier } };
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            string json = JsonSerializer.Serialize(new PocoWithConstructor("str"), options);
            Assert.Equal("{}", json);

            static void ContractModifier(JsonTypeInfo jti)
            {
                if (jti.Type == typeof(PocoWithConstructor))
                {
                    jti.Properties.Add(jti.CreateJsonPropertyInfo(typeof(string), "someOtherName"));
                }
            }
        }

        [Fact]
        public static void SerializingTypeWithCustomSerializablePropertyAndJsonConstructorWorksCorrectly()
        {
            var resolver = new DefaultJsonTypeInfoResolver { Modifiers = { ContractModifier } };
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            string json = JsonSerializer.Serialize(new PocoWithConstructor("str"), options);
            Assert.Equal("""{"test":"asd"}""", json);

            static void ContractModifier(JsonTypeInfo jti)
            {
                if (jti.Type == typeof(PocoWithConstructor))
                {
                    JsonPropertyInfo pi = jti.CreateJsonPropertyInfo(typeof(string), "test");
                    pi.Get = (o) => "asd";
                    jti.Properties.Add(pi);
                }
            }
        }

        [Fact]
        public static void SerializingTypeWithCustomPropertyAndJsonConstructorBindsParameter()
        {
            var resolver = new DefaultJsonTypeInfoResolver { Modifiers = { ContractModifier } };
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            string json = """{"parameter":"asd"}""";
            PocoWithConstructor deserialized = JsonSerializer.Deserialize<PocoWithConstructor>(json, options);
            Assert.Equal("asd", deserialized.ParameterValue);

            static void ContractModifier(JsonTypeInfo jti)
            {
                if (jti.Type == typeof(PocoWithConstructor))
                {
                    jti.Properties.Add(jti.CreateJsonPropertyInfo(typeof(string), "parameter"));
                }
            }
        }

        private class PocoWithConstructor
        {
            internal string ParameterValue { get; set; }

            public PocoWithConstructor(string parameter)
            {
                ParameterValue = parameter;
            }
        }

        [Fact]
        public static void JsonConstructorAttributeIsOverridenAndPropertiesAreSetWhenCreateObjectIsSet_LargeConstructor()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(ClassWithLargeParameterizedConstructor))
                {
                    Assert.Null(ti.CreateObject);
                    ti.CreateObject = () => new ClassWithLargeParameterizedConstructor();
                }
            });

            JsonSerializerOptions o = new() { TypeInfoResolver = resolver };

            string json = """{"A":2,"B":"foo","C":"bar","E":true}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithLargeParameterizedConstructor>(json, o);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.A);
            Assert.Equal("foo", deserialized.B);
            Assert.Equal("bar", deserialized.C);
            Assert.True(deserialized.E);
        }

        private class ClassWithLargeParameterizedConstructor
        {
            public int A { get; set; }
            public string B { get; set; }
            public string C { get; set; }
            public string D { get; set; }
            public bool E { get; set; }
            public int F { get; set; }

            public ClassWithLargeParameterizedConstructor() { }

            [JsonConstructor]
            public ClassWithLargeParameterizedConstructor(int a, string b, string c, string d, bool e, int f)
            {
                Assert.Fail("this ctor should not be used");
            }
        }
    }
}
