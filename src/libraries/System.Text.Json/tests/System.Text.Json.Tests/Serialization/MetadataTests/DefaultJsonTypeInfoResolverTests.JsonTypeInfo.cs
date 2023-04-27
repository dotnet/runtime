// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Tests;
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
            bool usingParameterizedConstructor = type.GetConstructors()
                .FirstOrDefault(ctor => ctor.GetParameters().Length != 0 && ctor.GetCustomAttribute<JsonConstructorAttribute>() != null) != null;

            DefaultJsonTypeInfoResolver r = new();
            JsonSerializerOptions o = new();
            o.Converters.Add(new CustomThrowingConverter<SomeClass>());

            JsonTypeInfo ti = r.GetTypeInfo(type, o);

            Assert.False(ti.IsReadOnly);
            Assert.Same(o, ti.Options);
            Assert.NotNull(ti.Properties);

            if (ti.Kind == JsonTypeInfoKind.Object && usingParameterizedConstructor)
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

        [Theory]
        [InlineData((JsonNumberHandling)(-1))]
        [InlineData((JsonNumberHandling)8)]
        [InlineData((JsonNumberHandling)int.MaxValue)]
        public static void NumberHandling_SetInvalidValue_ThrowsArgumentOutOfRangeException(JsonNumberHandling handling)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(Poco), new());
            Assert.Throws<ArgumentOutOfRangeException>(() => jsonTypeInfo.NumberHandling = handling);
        }

        [Theory]
        [InlineData(typeof(List<int>), JsonTypeInfoKind.Enumerable)]
        [InlineData(typeof(Dictionary<string, int>), JsonTypeInfoKind.Dictionary)]
        [InlineData(typeof(object), JsonTypeInfoKind.None)]
        [InlineData(typeof(string), JsonTypeInfoKind.None)]
        public static void AddingPropertyToNonObjectJsonTypeInfoKindThrows(Type type, JsonTypeInfoKind expectedKind)
        {
            JsonSerializerOptions options = new();
            DefaultJsonTypeInfoResolver resolver = new();
            JsonTypeInfo typeInfo = resolver.GetTypeInfo(type, options);
            Assert.Equal(expectedKind, typeInfo.Kind);

            JsonPropertyInfo property = typeInfo.CreateJsonPropertyInfo(typeof(int), "test");
            Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Add(property));
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

            Assert.True(typeInfo.IsReadOnly);
            Assert.True(typeInfo.Properties.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => typeInfo.CreateJsonPropertyInfo(typeof(string), "foo"));
            Assert.Throws<InvalidOperationException>(() => untyped.CreateObject = untyped.CreateObject);
            Assert.Throws<InvalidOperationException>(() => typeInfo.CreateObject = typeInfo.CreateObject);
            Assert.Throws<InvalidOperationException>(() => typeInfo.NumberHandling = typeInfo.NumberHandling);
            Assert.Throws<InvalidOperationException>(() => typeInfo.CreateJsonPropertyInfo(typeof(string), "foo"));
            Assert.Throws<InvalidOperationException>(() => typeInfo.UnmappedMemberHandling = typeInfo.UnmappedMemberHandling);
            Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Clear());
            Assert.Throws<InvalidOperationException>(() => typeInfo.PolymorphismOptions = null);
            Assert.Throws<InvalidOperationException>(() => typeInfo.PolymorphismOptions = new());
            Assert.Throws<InvalidOperationException>(() => typeInfo.PreferredPropertyObjectCreationHandling = null);
            Assert.Throws<InvalidOperationException>(() => typeInfo.OriginatingResolver = new DefaultJsonTypeInfoResolver());

            if (typeInfo.Properties.Count > 0)
            {
                JsonPropertyInfo prop = typeInfo.Properties[0];
                Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Add(prop));
                Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.Insert(0, prop));
                Assert.Throws<InvalidOperationException>(() => typeInfo.Properties.RemoveAt(0));
            }

            if (typeInfo.PolymorphismOptions is JsonPolymorphismOptions jpo)
            {
                Assert.True(jpo.DerivedTypes.IsReadOnly);
                Assert.Throws<InvalidOperationException>(() => jpo.IgnoreUnrecognizedTypeDiscriminators = true);
                Assert.Throws<InvalidOperationException>(() => jpo.TypeDiscriminatorPropertyName = "__case");
                Assert.Throws<InvalidOperationException>(() => jpo.UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor);
                Assert.Throws<InvalidOperationException>(() => jpo.DerivedTypes.Clear());
                Assert.Throws<InvalidOperationException>(() => jpo.DerivedTypes.Add(default));
                Assert.Throws<InvalidOperationException>(() => jpo.DerivedTypes.Insert(0, default));
            }

            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                Assert.NotNull(property.PropertyType);
                Assert.Null(property.CustomConverter);
                Assert.NotNull(property.Name);
                Assert.NotNull(property.Get);
                Assert.NotNull(property.Set);
                Assert.Null(property.ShouldSerialize);
                Assert.Null(typeInfo.NumberHandling);

                foreach (PropertyInfo propertyInfo in typeof(JsonPropertyInfo).GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    // We don't have any set only properties, if this ever changes we will need to update this code
                    if (propertyInfo.GetSetMethod() == null)
                    {
                        continue;
                    }

                    TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => propertyInfo.SetValue(property, propertyInfo.GetValue(property)));
                    Assert.NotNull(exception.InnerException);
                    Assert.IsType<InvalidOperationException>(exception.InnerException);
                }

                Assert.Throws<InvalidOperationException>(() => property.Name = null);
                Assert.Throws<InvalidOperationException>(() => property.ShouldSerialize = null);
                Assert.Throws<InvalidOperationException>(() => property.Get = null);
                Assert.Throws<InvalidOperationException>(() => property.Set = null);
                Assert.Throws<InvalidOperationException>(() => property.ObjectCreationHandling = null);
                Assert.Throws<InvalidOperationException>(() => property.IsExtensionData = true);
                Assert.Throws<InvalidOperationException>(() => property.IsRequired = true);
            }
        }

        [Theory]
        [InlineData(typeof(ICollection<int>), typeof(List<int>), false)]
        [InlineData(typeof(IList), typeof(List<int>), false)]
        [InlineData(typeof(IList<int>), typeof(List<int>), false)]
        [InlineData(typeof(List<int>), typeof(List<int>), false)]
        [InlineData(typeof(ISet<int>), typeof(HashSet<int>), false)]
        [InlineData(typeof(Queue<int>), typeof(Queue<int>), false)]
        [InlineData(typeof(Stack<int>), typeof(Stack<int>), false)]
        [InlineData(typeof(IDictionary), typeof(Hashtable), true)]
        [InlineData(typeof(IDictionary<string, int>), typeof(ConcurrentDictionary<string, int>), true)]
        public static void JsonTypeInfo_CreateObject_SupportedCollection(Type collectionType, Type runtimeType, bool isDictionary)
        {
            bool isDelegateInvoked = false;

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        jsonTypeInfo =>
                        {
                            if (jsonTypeInfo.Type == collectionType)
                            {
                                jsonTypeInfo.CreateObject = () =>
                                {
                                    isDelegateInvoked = true;
                                    return Activator.CreateInstance(runtimeType);
                                };
                            }
                        }
                    }
                }
            };

            string json = isDictionary ? "{}" : "[]";
            object result = JsonSerializer.Deserialize(json, collectionType, options);
            Assert.IsType(runtimeType, result);
            Assert.True(isDelegateInvoked);
        }

        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(ImmutableArray<int>))]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(IEnumerable<int>))]
        [InlineData(typeof(IAsyncEnumerable<int>))]
        [InlineData(typeof(IReadOnlyDictionary<string, int>))]
        [InlineData(typeof(ImmutableDictionary<string, int>))]
        public static void JsonTypeInfo_CreateObject_UnsupportedCollection_ThrowsInvalidOperationException(Type collectionType)
        {
            var options = new JsonSerializerOptions();
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(collectionType, options);
            Assert.Throws<InvalidOperationException>(() => jsonTypeInfo.CreateObject = () => new object());
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
        public static void AddRecursiveJsonPropertyInfoFromMetadataServices()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static typeInfo =>
                        {
                            if (typeInfo.Type != typeof(SomeClass))
                            {
                                return;
                            }

                            typeInfo.Properties.Clear();
                            JsonPropertyInfo propertyInfo = JsonMetadataServices.CreatePropertyInfo<SomeClass>(
                                typeInfo.Options,
                                new JsonPropertyInfoValues<SomeClass>()
                                {
                                    DeclaringType = typeof(SomeClass),
                                    PropertyName = "Next",
                                });

                            typeInfo.Properties.Add(propertyInfo);
                            Assert.Equal(JsonTypeInfoKind.Object, typeInfo.Kind);
                        }
                    }
                }
            };

            JsonTypeInfo<SomeClass> jsonTypeInfo = Assert.IsAssignableFrom<JsonTypeInfo<SomeClass>>(options.GetTypeInfo(typeof(SomeClass)));
            Assert.Equal(1, jsonTypeInfo.Properties.Count);

            JsonPropertyInfo propertyInfo = jsonTypeInfo.Properties[0];
            Assert.Equal(typeof(SomeClass), propertyInfo.PropertyType);
            Assert.Equal("Next", propertyInfo.Name);
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

        [Fact]
        public static void CreateJsonTypeInfoWithNullArgumentsThrows()
        {
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfo.CreateJsonTypeInfo(null, new JsonSerializerOptions()));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfo.CreateJsonTypeInfo(typeof(string), null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfo.CreateJsonTypeInfo(null, null));
            Assert.Throws<ArgumentNullException>(() => JsonTypeInfo.CreateJsonTypeInfo<string>(null));
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(Dictionary<,>))]
        [InlineData(typeof(List<>))]
        [InlineData(typeof(Nullable<>))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(RefStruct))]
        public static void CreateJsonTypeInfoWithInappropriateTypeThrows(Type type)
        {
            Assert.Throws<ArgumentException>(() => JsonTypeInfo.CreateJsonTypeInfo(type, new JsonSerializerOptions()));
        }

        ref struct RefStruct
        {
            public int Foo { get; set; }
        }

        [Fact]
        public static void CreateJsonTypeInfo_ThrowingConverterFactory_DoesNotWrapException()
        {
            var options = new JsonSerializerOptions { Converters = { new ClassWithThrowingConverterFactory.Converter() } };
            // Should not be wrapped in TargetInvocationException.
            Assert.Throws<NotFiniteNumberException>(() => JsonTypeInfo.CreateJsonTypeInfo(typeof(ClassWithThrowingConverterFactory), options));
        }

        public class ClassWithThrowingConverterFactory
        {
            public class Converter : JsonConverterFactory
            {
                public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(ClassWithThrowingConverterFactory);
                public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => throw new NotFiniteNumberException();
            }
        }

        [Fact]
        public static void CreateJsonPropertyInfoWithNullArgumentsThrows()
        {
            JsonTypeInfo ti = JsonTypeInfo.CreateJsonTypeInfo<MyClass>(new JsonSerializerOptions());
            Assert.Throws<ArgumentNullException>(() => ti.CreateJsonPropertyInfo(null, "test"));
            Assert.Throws<ArgumentNullException>(() => ti.CreateJsonPropertyInfo(typeof(string), null));
            Assert.Throws<ArgumentNullException>(() => ti.CreateJsonPropertyInfo(null, null));
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(Dictionary<,>))]
        [InlineData(typeof(List<>))]
        [InlineData(typeof(Nullable<>))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(RefStruct))]
        public static void CreateJsonPropertyInfoWithInappropriateTypeThrows(Type type)
        {
            JsonTypeInfo ti = JsonTypeInfo.CreateJsonTypeInfo<MyClass>(new JsonSerializerOptions());
            Assert.Throws<ArgumentException>(() => ti.CreateJsonPropertyInfo(type, "test"));
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
            yield return new object[] { new SomePolymorphicClass() };
        }

        [Fact]
        public static void JsonConstructorAttributeIsOverriddenWhenCreateObjectIsSet()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(ClassWithParameterizedConstructorAndReadOnlyProperties))
                {
                    Assert.Null(ti.CreateObject);
                    ti.CreateObject = () => new ClassWithParameterizedConstructorAndReadOnlyProperties(1, "test", dummyParam: true);
                }
            });

            JsonSerializerOptions o = new() { TypeInfoResolver = resolver };
            string json = """{"A":2,"B":"foo"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithParameterizedConstructorAndReadOnlyProperties>(json, o);

            Assert.NotNull(deserialized);
            Assert.Equal(1, deserialized.A);
            Assert.Equal("test", deserialized.B);
        }

        private class ClassWithParameterizedConstructorAndReadOnlyProperties
        {
            public int A { get; }
            public string B { get; }

            public ClassWithParameterizedConstructorAndReadOnlyProperties(int a, string b, bool dummyParam)
            {
                A = a;
                B = b;
            }

            [JsonConstructor]
            public ClassWithParameterizedConstructorAndReadOnlyProperties(int a, string b)
            {
                Assert.Fail("this ctor should not be used");
            }
        }

        [Fact]
        public static void JsonConstructorAttributeIsOverriddenAndPropertiesAreSetWhenCreateObjectIsSet()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(ClassWithParameterizedConstructorAndWritableProperties))
                {
                    Assert.Null(ti.CreateObject);
                    ti.CreateObject = () => new ClassWithParameterizedConstructorAndWritableProperties();
                }
            });

            JsonSerializerOptions o = new() { TypeInfoResolver = resolver };

            string json = """{"A":2,"B":"foo","C":"bar"}""";
            var deserialized = JsonSerializer.Deserialize<ClassWithParameterizedConstructorAndWritableProperties>(json, o);

            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.A);
            Assert.Equal("foo", deserialized.B);
            Assert.Equal("bar", deserialized.C);
        }

        private class ClassWithParameterizedConstructorAndWritableProperties
        {
            public int A { get; set; }
            public string B { get; set; }
            public string C { get; set; }

            public ClassWithParameterizedConstructorAndWritableProperties() { }

            [JsonConstructor]
            public ClassWithParameterizedConstructorAndWritableProperties(int a, string b)
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
        public static void JsonConstructorAttributeIsOverriddenAndPropertiesAreSetWhenCreateObjectIsSet_LargeConstructor()
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

        [Theory]
        [InlineData(typeof(ICollection<string>))]
        [InlineData(typeof(IList))]
        [InlineData(typeof(IList<bool>))]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(IDictionary<string, bool>))]
        [InlineData(typeof(ISet<Guid>))]
        public static void AbstractCollectionMetadata_SurfacesCreateObjectWhereApplicable(Type type)
        {
            var options = new JsonSerializerOptions();
            var resolver = new DefaultJsonTypeInfoResolver();

            JsonTypeInfo metadata = resolver.GetTypeInfo(type, options);
            Assert.NotNull(metadata.CreateObject);
            Assert.IsAssignableFrom(type, metadata.CreateObject());
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

        [Fact]
        public static void PropertyOrderIsRespected()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                {
                    Modifiers =
                    {
                        ti =>
                        {
                            if (ti.Type == typeof(ClassWithExplicitOrderOfProperties))
                            {
                                Assert.Equal(5, ti.Properties.Count);

                                Assert.Equal("A", ti.Properties[0].Name);
                                Assert.Equal("B", ti.Properties[1].Name);
                                Assert.Equal("C", ti.Properties[2].Name);
                                Assert.Equal("D", ti.Properties[3].Name);
                                Assert.Equal("E", ti.Properties[4].Name);

                                Assert.Equal(-2, ti.Properties[0].Order);
                                Assert.Equal(-1, ti.Properties[1].Order);
                                Assert.Equal(0, ti.Properties[2].Order);
                                Assert.Equal(1, ti.Properties[3].Order);
                                Assert.Equal(2, ti.Properties[4].Order);

                                // swapping A,B order values
                                (ti.Properties[0].Order, ti.Properties[1].Order) = (ti.Properties[1].Order, ti.Properties[0].Order);

                                // swapping E,C order values
                                (ti.Properties[2].Order, ti.Properties[4].Order) = (ti.Properties[4].Order, ti.Properties[2].Order);

                                // swapping B,D properties (has no effect on contract)
                                (ti.Properties[1], ti.Properties[3]) = (ti.Properties[3], ti.Properties[1]);
                            }
                        }
                    }
                }
            };

            ClassWithExplicitOrderOfProperties obj = new()
            {
                A = "a",
                B = "b",
                C = "c",
                D = "d",
                E = "e",
            };

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"B":"b","A":"a","E":"e","D":"d","C":"c"}""", json);
        }

        private class ClassWithExplicitOrderOfProperties
        {
            public string C { get; set; }

            [JsonPropertyOrder(1)]
            public string D { get; set; }

            [JsonPropertyOrder(2)]
            public string E { get; set; }

            [JsonPropertyOrder(-1)]
            public string B { get; set; }

            [JsonPropertyOrder(-2)]
            public string A { get; set; }
        }

        [Fact]
        public static void RecursiveTypeWithResolverResolvingOnlyThatType()
        {
            TestResolver resolver = new(ResolveTypeInfo);

            JsonSerializerOptions options = new();
            options.TypeInfoResolver = resolver;

            string json = JsonSerializer.Serialize(new RecursiveType() { Next = new RecursiveType() }, options);
            Assert.Equal("""{"Next":{"Next":null}}""", json);

            RecursiveType deserialized = JsonSerializer.Deserialize<RecursiveType>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Value);
            Assert.NotNull(deserialized.Next);
            Assert.Equal(1, deserialized.Next.Value);
            Assert.Null(deserialized.Next.Next);

            static JsonTypeInfo? ResolveTypeInfo(Type type, JsonSerializerOptions options)
            {
                int value = 1;
                if (type == typeof(RecursiveType))
                {
                    JsonTypeInfo<RecursiveType> ti = JsonTypeInfo.CreateJsonTypeInfo<RecursiveType>(options);
                    ti.CreateObject = () => new RecursiveType();
                    JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(RecursiveType), "Next");
                    prop.Get = (obj) => ((RecursiveType)obj).Next;
                    prop.Set = (obj, val) =>
                    {
                        RecursiveType recursiveObj = (RecursiveType)obj;
                        recursiveObj.Next = (RecursiveType)val;
                        recursiveObj.Value = value++;
                    };
                    ti.Properties.Add(prop);
                    return ti;
                }

                return null;
            }
        }

        [Fact]
        public static void RecursiveTypeWithResolverResolvingOnlyThatTypeThrowsWhenPropertyOfDifferentType()
        {
            TestResolver resolver = new(ResolveTypeInfo);

            JsonSerializerOptions options = new();
            options.TypeInfoResolver = resolver;

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new RecursiveType() { Next = new RecursiveType() }, options));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<RecursiveType>("""{"Next":{"Next":null}}""", options));

            static JsonTypeInfo? ResolveTypeInfo(Type type, JsonSerializerOptions options)
            {
                if (type == typeof(RecursiveType))
                {
                    JsonTypeInfo<RecursiveType> ti = JsonTypeInfo.CreateJsonTypeInfo<RecursiveType>(options);
                    ti.CreateObject = () => new RecursiveType();
                    {
                        JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(RecursiveType), "Next");
                        prop.Get = (obj) => ((RecursiveType)obj).Next;
                        prop.Set = (obj, val) => ((RecursiveType)obj).Next = (RecursiveType)val;
                        ti.Properties.Add(prop);
                    }
                    {
                        JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(int), "Value");
                        prop.Get = (obj) => ((RecursiveType)obj).Value;
                        prop.Set = (obj, val) => ((RecursiveType)obj).Value = (int)val;
                        ti.Properties.Add(prop);
                    }

                    return ti;
                }

                return null;
            }
        }

        [Fact]
        public static void RecursiveTypeWithResolverResolvingOnlyUsedTypes()
        {
            TestResolver resolver = new(ResolveTypeInfo);

            JsonSerializerOptions options = new();
            options.TypeInfoResolver = resolver;

            RecursiveType obj = new RecursiveType()
            {
                Value = 13,
                Next = new RecursiveType() { Value = 7 },
            };
            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"Next":{"Next":null,"Value":7},"Value":13}""", json);

            RecursiveType deserialized = JsonSerializer.Deserialize<RecursiveType>(json, options);
            Assert.NotNull(deserialized);
            Assert.Equal(13, deserialized.Value);
            Assert.NotNull(deserialized.Next);
            Assert.Equal(7, deserialized.Next.Value);
            Assert.Null(deserialized.Next.Next);

            static JsonTypeInfo? ResolveTypeInfo(Type type, JsonSerializerOptions options)
            {
                if (type == typeof(RecursiveType))
                {
                    JsonTypeInfo<RecursiveType> ti = JsonTypeInfo.CreateJsonTypeInfo<RecursiveType>(options);
                    ti.CreateObject = () => new RecursiveType();
                    {
                        JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(RecursiveType), "Next");
                        prop.Get = (obj) => ((RecursiveType)obj).Next;
                        prop.Set = (obj, val) => ((RecursiveType)obj).Next = (RecursiveType)val;
                        ti.Properties.Add(prop);
                    }
                    {
                        JsonPropertyInfo prop = ti.CreateJsonPropertyInfo(typeof(int), "Value");
                        prop.Get = (obj) => ((RecursiveType)obj).Value;
                        prop.Set = (obj, val) => ((RecursiveType)obj).Value = (int)val;
                        ti.Properties.Add(prop);
                    }
                    return ti;
                }

                if (type == typeof(int))
                {
                    return JsonTypeInfo.CreateJsonTypeInfo<int>(options);
                }

                return null;
            }
        }

        private class RecursiveType
        {
            public int Value { get; set; }
            public RecursiveType? Next { get; set; }
        }

        [Fact]
        public static void CreateJsonTypeInfo_ClassWithConverterAttribute_ShouldNotResolveConverterAttribute()
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(ClassWithConverterAttribute), JsonSerializerOptions.Default);
            Assert.Equal(typeof(ClassWithConverterAttribute), jsonTypeInfo.Type);
            Assert.IsNotType<ClassWithConverterAttribute.CustomConverter>(jsonTypeInfo.Converter);
        }

        [Fact]
        public static void DefaultJsonTypeInfoResolver_ClassWithConverterAttribute_ShouldResolveConverterAttribute()
        {
            var options = JsonSerializerOptions.Default;
            JsonTypeInfo jsonTypeInfo = options.TypeInfoResolver.GetTypeInfo(typeof(ClassWithConverterAttribute), options);
            Assert.Equal(typeof(ClassWithConverterAttribute), jsonTypeInfo.Type);
            Assert.IsType<ClassWithConverterAttribute.CustomConverter>(jsonTypeInfo.Converter);
        }

        [JsonConverter(typeof(CustomConverter))]
        public class ClassWithConverterAttribute
        {
            public class CustomConverter : JsonConverter<ClassWithConverterAttribute>
            {
                public override ClassWithConverterAttribute? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
                public override void Write(Utf8JsonWriter writer, ClassWithConverterAttribute value, JsonSerializerOptions options) => throw new NotImplementedException();
            }
        }

        [Fact]
        public static void ClassWithCallBacks_JsonTypeInfoCallbackDelegatesArePopulated()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            var jti = resolver.GetTypeInfo(typeof(ClassWithCallBacks), new());

            Assert.NotNull(jti.OnSerializing);
            Assert.NotNull(jti.OnSerialized);
            Assert.NotNull(jti.OnDeserializing);
            Assert.NotNull(jti.OnDeserialized);

            var value = new ClassWithCallBacks();
            jti.OnSerializing(value);
            Assert.Equal(1, value.IsOnSerializingInvocations);

            jti.OnSerialized(value);
            Assert.Equal(1, value.IsOnSerializedInvocations);

            jti.OnDeserializing(value);
            Assert.Equal(1, value.IsOnDeserializingInvocations);

            jti.OnDeserialized(value);
            Assert.Equal(1, value.IsOnDeserializedInvocations);
        }

        [Fact]
        public static void ClassWithCallBacks_CanCustomizeCallbacks()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static jti =>
                        {
                            if (jti.Type == typeof(ClassWithCallBacks))
                            {
                                jti.OnSerializing = null;
                                jti.OnSerialized = (obj => ((ClassWithCallBacks)obj).IsOnSerializedInvocations += 10);

                                jti.OnDeserializing = null;
                                jti.OnDeserialized = (obj => ((ClassWithCallBacks)obj).IsOnDeserializedInvocations += 7);
                            }
                        }
                    }
                }
            };

            var value = new ClassWithCallBacks();
            string json = JsonSerializer.Serialize(value, options);
            Assert.Equal("{}", json);

            Assert.Equal(0, value.IsOnSerializingInvocations);
            Assert.Equal(10, value.IsOnSerializedInvocations);

            value = JsonSerializer.Deserialize<ClassWithCallBacks>(json, options);
            Assert.Equal(0, value.IsOnDeserializingInvocations);
            Assert.Equal(7, value.IsOnDeserializedInvocations);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        public static void SettingCallbacksOnUnsupportedTypes_ThrowsInvalidOperationException(Type type)
        {
            var jti = JsonTypeInfo.CreateJsonTypeInfo(type, new());

            Assert.NotEqual(JsonTypeInfoKind.Object, jti.Kind);
            Assert.Throws<InvalidOperationException>(() => jti.OnSerializing = null);
            Assert.Throws<InvalidOperationException>(() => jti.OnSerializing = (obj => { }));
            Assert.Throws<InvalidOperationException>(() => jti.OnSerialized = null);
            Assert.Throws<InvalidOperationException>(() => jti.OnSerialized = (obj => { }));
            Assert.Throws<InvalidOperationException>(() => jti.OnDeserializing = null);
            Assert.Throws<InvalidOperationException>(() => jti.OnDeserializing = (obj => { }));
            Assert.Throws<InvalidOperationException>(() => jti.OnDeserialized = null);
            Assert.Throws<InvalidOperationException>(() => jti.OnDeserialized = (obj => { }));
        }

        public class ClassWithCallBacks :
            IJsonOnSerializing, IJsonOnSerialized,
            IJsonOnDeserializing, IJsonOnDeserialized
        {
            [JsonIgnore]
            public int IsOnSerializingInvocations { get; set; }
            [JsonIgnore]
            public int IsOnSerializedInvocations { get; set; }
            [JsonIgnore]
            public int IsOnDeserializingInvocations { get; set; }
            [JsonIgnore]
            public int IsOnDeserializedInvocations { get; set; }

            public void OnSerializing() => IsOnSerializingInvocations++;
            public void OnSerialized() => IsOnSerializedInvocations++;
            public void OnDeserializing() => IsOnDeserializingInvocations++;
            public void OnDeserialized() => IsOnDeserializedInvocations++;
        }

        [Theory]
        [InlineData(null)]
        [InlineData(JsonUnmappedMemberHandling.Skip)]
        [InlineData(JsonUnmappedMemberHandling.Disallow)]
        public static void UnmappedMemberHandling_ShouldGetSetValue(JsonUnmappedMemberHandling? handling)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(Poco), JsonSerializerOptions.Default);
            jsonTypeInfo.UnmappedMemberHandling = handling;
            Assert.Equal(handling, jsonTypeInfo.UnmappedMemberHandling);
            JsonSerializer.Serialize(new Poco(), jsonTypeInfo);
            Assert.Equal(handling, jsonTypeInfo.UnmappedMemberHandling);
        }

        [Theory]
        [InlineData((JsonUnmappedMemberHandling)(-1))]
        [InlineData((JsonUnmappedMemberHandling)2)]
        [InlineData((JsonUnmappedMemberHandling)int.MaxValue)]
        public static void UnmappedMemberHandling_SetInvalidValue_ThrowsArgumentOutOfRangeException(JsonUnmappedMemberHandling handling)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(Poco), new());
            Assert.Throws<ArgumentOutOfRangeException>(() => jsonTypeInfo.UnmappedMemberHandling = handling);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<string, int>))]
        public static void PreferredPropertyObjectCreationHandling_NonObjectKind_ThrowsInvalidOperationException(Type type)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, new());

            // Invalid kinds default to null and can be set to null.
            Assert.Null(jsonTypeInfo.PreferredPropertyObjectCreationHandling);
            jsonTypeInfo.PreferredPropertyObjectCreationHandling = null;
            Assert.Null(jsonTypeInfo.PreferredPropertyObjectCreationHandling);

            Assert.Throws<InvalidOperationException>(() => jsonTypeInfo.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Populate);
            Assert.Throws<InvalidOperationException>(() => jsonTypeInfo.PreferredPropertyObjectCreationHandling = JsonObjectCreationHandling.Replace);
            Assert.Null(jsonTypeInfo.PreferredPropertyObjectCreationHandling);
        }

        [Theory]
        [InlineData((JsonObjectCreationHandling)(-1))]
        [InlineData((JsonObjectCreationHandling)2)]
        [InlineData((JsonObjectCreationHandling)int.MaxValue)]
        public static void PreferredPropertyObjectCreationHandling_SetInvalidValue_ThrowsArgumentOutOfRangeException(JsonObjectCreationHandling handling)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(typeof(Poco), new());
            Assert.Throws<ArgumentOutOfRangeException>(() => jsonTypeInfo.PreferredPropertyObjectCreationHandling = handling);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void UnmappedMemberHandling_InvalidMetadataKind_ThrowsInvalidOperationException(Type type)
        {
            JsonTypeInfo jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, new());
            Assert.Throws<InvalidOperationException>(() => jsonTypeInfo.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void DefaultJsonTypeInfo_OriginatingResolver_GetterReturnsResolver(Type type)
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonSerializerOptions();

            JsonTypeInfo typeInfo = resolver.GetTypeInfo(type, options);
            Assert.Same(resolver, typeInfo.OriginatingResolver);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void OriginatingResolver_GetterReturnsTheSetValue(Type type)
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonSerializerOptions();

            JsonTypeInfo typeInfo = resolver.GetTypeInfo(type, options);
            typeInfo.OriginatingResolver = null;
            Assert.Null(typeInfo.OriginatingResolver);

            typeInfo.OriginatingResolver = JsonSerializerOptions.Default.TypeInfoResolver;
            Assert.Same(JsonSerializerOptions.Default.TypeInfoResolver, typeInfo.OriginatingResolver);
        }
    }
}
