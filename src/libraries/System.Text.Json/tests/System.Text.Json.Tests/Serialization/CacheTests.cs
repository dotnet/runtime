// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.RemoteExecutor;

namespace System.Text.Json.Serialization.Tests
{
    public static class CacheTests
    {
        [Fact, OuterLoop]
        public static async Task MultipleThreads_SameType_DifferentJson_Looping()
        {
            const int Iterations = 100;

            for (int i = 0; i < Iterations; i++)
            {
                await MultipleThreads_SameType_DifferentJson();
            }
        }

        [Fact]
        public static async Task MultipleThreads_SameType_DifferentJson()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            // Verify the test class has >64 properties since that is a threshold for using the fallback dictionary.
            Assert.True(typeof(SimpleTestClass).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length > 64);

            void DeserializeObjectMinimal()
            {
                SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(@"{""MyDecimal"" : 3.3}", options);
            };

            void DeserializeObjectFlipped()
            {
                SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(SimpleTestClass.s_json_flipped, options);
                obj.Verify();
            };

            void DeserializeObjectNormal()
            {
                SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(SimpleTestClass.s_json, options);
                obj.Verify();
            };

            void SerializeObject()
            {
                var obj = new SimpleTestClass();
                obj.Initialize();
                JsonSerializer.Serialize(obj, options);
            };

            const int ThreadCount = 8;
            const int ConcurrentTestsCount = 4;
            Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

            for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
            {
                // Create race condition to populate the sorted property cache with different json ordering.
                tasks[i + 0] = Task.Run(() => DeserializeObjectMinimal());
                tasks[i + 1] = Task.Run(() => DeserializeObjectFlipped());
                tasks[i + 2] = Task.Run(() => DeserializeObjectNormal());

                // Ensure no exceptions on serialization
                tasks[i + 3] = Task.Run(() => SerializeObject());
            };

            await Task.WhenAll(tasks);
        }

        [Fact, OuterLoop]
        public static async Task MultipleThreads_DifferentTypes_Looping()
        {
            const int Iterations = 100;

            for (int i = 0; i < Iterations; i++)
            {
                await MultipleThreads_DifferentTypes();
            }
        }

        [Fact]
        public static async Task MultipleThreads_DifferentTypes()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            const int TestClassCount = 2;

            var testObjects = new ITestClass[TestClassCount]
            {
                new SimpleTestClassWithNulls(),
                new SimpleTestClass(),
            };

            foreach (ITestClass obj in testObjects)
            {
                obj.Initialize();
            }

            void Test(int i)
            {
                Type testClassType = testObjects[i].GetType();

                string json = JsonSerializer.Serialize(testObjects[i], testClassType, options);

                ITestClass obj = (ITestClass)JsonSerializer.Deserialize(json, testClassType, options);
                obj.Verify();
            };

            const int OuterCount = 12;
            Task[] tasks = new Task[OuterCount * TestClassCount];

            for (int i = 0; i < tasks.Length; i += TestClassCount)
            {
                tasks[i + 0] = Task.Run(() => Test(TestClassCount - 1));
                tasks[i + 1] = Task.Run(() => Test(TestClassCount - 2));
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public static void PropertyCacheWithMinInputsFirst()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            string json = "{}";
            JsonSerializer.Deserialize<SimpleTestClass>(json, options);

            SimpleTestClass testObj = new SimpleTestClass();
            testObj.Initialize();
            testObj.Verify();

            json = JsonSerializer.Serialize(testObj, options);
            testObj = JsonSerializer.Deserialize<SimpleTestClass>(json, options);
            testObj.Verify();
        }

        [Fact]
        public static void PropertyCacheWithMinInputsLast()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            SimpleTestClass testObj = new SimpleTestClass();
            testObj.Initialize();
            testObj.Verify();

            string json = JsonSerializer.Serialize(testObj, options);
            testObj = JsonSerializer.Deserialize<SimpleTestClass>(json, options);
            testObj.Verify();

            json = "{}";
            JsonSerializer.Deserialize<SimpleTestClass>(json, options);
        }

        // Use a common options instance to encourage additional metadata collisions across types. Also since
        // this options is not the default options instance the tests will not use previously cached metadata.
        private static JsonSerializerOptions s_options = new JsonSerializerOptions { IncludeFields = true };

        [Theory]
        [MemberData(nameof(WriteSuccessCases))]
        public static async Task MultipleTypes(ITestClass testObj)
        {
            Type type = testObj.GetType();

            // Get the test json with the default options to avoid cache pollution of Deserialize() below.
            testObj.Initialize();
            testObj.Verify();
            var options = new JsonSerializerOptions { IncludeFields = true };
            string json = JsonSerializer.Serialize(testObj, type, options);

            void Serialize()
            {
                ITestClass localTestObj = (ITestClass)Activator.CreateInstance(type);
                localTestObj.Initialize();
                localTestObj.Verify();
                string json = JsonSerializer.Serialize(localTestObj, type, s_options);
            };

            void Deserialize()
            {
                ITestClass obj = (ITestClass)JsonSerializer.Deserialize(json, type, s_options);
                obj.Verify();
            };

            const int ThreadCount = 12;
            const int ConcurrentTestsCount = 2;
            Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

            for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
            {
                tasks[i + 0] = Task.Run(() => Deserialize());
                tasks[i + 1] = Task.Run(() => Serialize());
            };

            await Task.WhenAll(tasks);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void JsonSerializerOptionsUpdateHandler_ClearingDoesntPreventSerialization()
        {
            // This test uses reflection to:
            // - Access JsonSerializerOptions._cachingContext.Count
            // - Access JsonSerializerOptionsUpdateHandler.ClearCache
            //
            // If either of them changes, this test will need to be kept in sync.

            RemoteExecutor.Invoke(static () =>
                {
                    var options = new JsonSerializerOptions();

                    Func<JsonSerializerOptions, int> getCount = CreateCacheCountAccessor();

                    Assert.Equal(0, getCount(options));

                    SimpleTestClass testObj = new SimpleTestClass();
                    testObj.Initialize();
                    JsonSerializer.Serialize<SimpleTestClass>(testObj, options);
                    Assert.NotEqual(0, getCount(options));

                    Type updateHandler = typeof(JsonSerializerOptions).Assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler", throwOnError: true, ignoreCase: false);
                    MethodInfo clearCache = updateHandler.GetMethod("ClearCache");
                    Assert.NotNull(clearCache);
                    clearCache.Invoke(null, new object[] { null });
                    Assert.Equal(0, getCount(options));

                    JsonSerializer.Serialize<SimpleTestClass>(testObj, options);
                    Assert.NotEqual(0, getCount(options));
                }).Dispose();

            static Func<JsonSerializerOptions, int> CreateCacheCountAccessor()
            {
                FieldInfo cacheField = typeof(JsonSerializerOptions).GetField("_cachingContext", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(cacheField);
                PropertyInfo countProperty = cacheField.FieldType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(countProperty);
                return options =>
                {
                    object? cache = cacheField.GetValue(options);
                    return cache is null ? 0 : (int)countProperty.GetValue(cache);
                };
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/66232", TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(GetJsonSerializerOptions))]
        public static void JsonSerializerOptions_ReuseConverterCaches()
        {
            // This test uses reflection to:
            // - Access JsonSerializerOptions._cachingContext._options
            // - Access JsonSerializerOptions.EqualityComparer.AreEquivalent
            //
            // If either of them changes, this test will need to be kept in sync.

            RemoteExecutor.Invoke(static () =>
            {
                Func<JsonSerializerOptions, JsonSerializerOptions?> getCacheOptions = CreateCacheOptionsAccessor();
                IEqualityComparer<JsonSerializerOptions> equalityComparer = CreateEqualityComparerAccessor();

                foreach (var args in GetJsonSerializerOptions())
                {
                    var options = (JsonSerializerOptions)args[0];
                    Assert.Null(getCacheOptions(options));

                    JsonSerializer.Serialize(42, options);

                    JsonSerializerOptions originalCacheOptions = getCacheOptions(options);
                    Assert.NotNull(originalCacheOptions);
                    Assert.True(equalityComparer.Equals(options, originalCacheOptions));
                    Assert.Equal(equalityComparer.GetHashCode(options), equalityComparer.GetHashCode(originalCacheOptions));

                    for (int i = 0; i < 5; i++)
                    {
                        var options2 = new JsonSerializerOptions(options);
                        Assert.Null(getCacheOptions(options2));

                        JsonSerializer.Serialize(42, options2);

                        Assert.True(equalityComparer.Equals(options2, originalCacheOptions));
                        Assert.Equal(equalityComparer.GetHashCode(options2), equalityComparer.GetHashCode(originalCacheOptions));
                        Assert.Same(originalCacheOptions, getCacheOptions(options2));
                    }
                }
            }).Dispose();

            static Func<JsonSerializerOptions, JsonSerializerOptions?> CreateCacheOptionsAccessor()
            {
                FieldInfo cacheField = typeof(JsonSerializerOptions).GetField("_cachingContext", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(cacheField);
                PropertyInfo optionsField = cacheField.FieldType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(optionsField);
                return options =>
                {
                    object? cache = cacheField.GetValue(options);
                    return cache is null ? null : (JsonSerializerOptions)optionsField.GetValue(cache);
                };
            }
        }

        public static IEnumerable<object[]> GetJsonSerializerOptions()
        {
            yield return new[] { new JsonSerializerOptions() };
            yield return new[] { new JsonSerializerOptions(JsonSerializerDefaults.Web) };
            yield return new[] { new JsonSerializerOptions { WriteIndented = true } };
            yield return new[] { new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } } };
        }

        [Fact]
        public static void JsonSerializerOptions_EqualityComparer_ChangingAnySettingShouldReturnFalse()
        {
            // This test uses reflection to:
            // - Access JsonSerializerOptions.EqualityComparer.AreEquivalent
            // - All public setters in JsonSerializerOptions
            //
            // If either of them changes, this test will need to be kept in sync.
            IEqualityComparer<JsonSerializerOptions> equalityComparer = CreateEqualityComparerAccessor();

            (PropertyInfo prop, object value)[] propertySettersAndValues = GetPropertiesWithSettersAndNonDefaultValues().ToArray();

            // Ensure we're testing equality for all JsonSerializerOptions settings
            foreach (PropertyInfo prop in GetAllPublicPropertiesWithSetters().Except(propertySettersAndValues.Select(x => x.prop)))
            {
                Assert.Fail($"{nameof(GetPropertiesWithSettersAndNonDefaultValues)} missing property declaration for {prop.Name}, please update the method.");
            }

            Assert.True(equalityComparer.Equals(JsonSerializerOptions.Default, JsonSerializerOptions.Default));
            Assert.Equal(equalityComparer.GetHashCode(JsonSerializerOptions.Default), equalityComparer.GetHashCode(JsonSerializerOptions.Default));

            foreach ((PropertyInfo prop, object? value) in propertySettersAndValues)
            {
                var options = new JsonSerializerOptions();
                prop.SetValue(options, value);

                Assert.True(equalityComparer.Equals(options, options));
                Assert.Equal(equalityComparer.GetHashCode(options), equalityComparer.GetHashCode(options));

                Assert.False(equalityComparer.Equals(JsonSerializerOptions.Default, options));
                Assert.NotEqual(equalityComparer.GetHashCode(JsonSerializerOptions.Default), equalityComparer.GetHashCode(options));
            }

            static IEnumerable<(PropertyInfo, object)> GetPropertiesWithSettersAndNonDefaultValues()
            {
                yield return (GetProp(nameof(JsonSerializerOptions.AllowTrailingCommas)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.DefaultBufferSize)), 42);
                yield return (GetProp(nameof(JsonSerializerOptions.Encoder)), JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
                yield return (GetProp(nameof(JsonSerializerOptions.DictionaryKeyPolicy)), JsonNamingPolicy.CamelCase);
                yield return (GetProp(nameof(JsonSerializerOptions.IgnoreNullValues)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.DefaultIgnoreCondition)), JsonIgnoreCondition.WhenWritingDefault);
                yield return (GetProp(nameof(JsonSerializerOptions.NumberHandling)), JsonNumberHandling.AllowReadingFromString);
                yield return (GetProp(nameof(JsonSerializerOptions.IgnoreReadOnlyProperties)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.IgnoreReadOnlyFields)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.IncludeFields)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.MaxDepth)), 11);
                yield return (GetProp(nameof(JsonSerializerOptions.PropertyNamingPolicy)), JsonNamingPolicy.CamelCase);
                yield return (GetProp(nameof(JsonSerializerOptions.PropertyNameCaseInsensitive)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.ReadCommentHandling)), JsonCommentHandling.Skip);
                yield return (GetProp(nameof(JsonSerializerOptions.UnknownTypeHandling)), JsonUnknownTypeHandling.JsonNode);
                yield return (GetProp(nameof(JsonSerializerOptions.WriteIndented)), true);
                yield return (GetProp(nameof(JsonSerializerOptions.ReferenceHandler)), ReferenceHandler.Preserve);
                yield return (GetProp(nameof(JsonSerializerOptions.TypeInfoResolver)), new DefaultJsonTypeInfoResolver());

                static PropertyInfo GetProp(string name)
                {
                    PropertyInfo property = typeof(JsonSerializerOptions).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    Assert.True(property.CanWrite);
                    return property;
                }
            }

            static IEnumerable<PropertyInfo> GetAllPublicPropertiesWithSetters()
                => typeof(JsonSerializerOptions)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);
        }

        [Fact]
        public static void JsonSerializerOptions_EqualityComparer_ApplyingJsonSerializerContextShouldReturnFalse()
        {
            // This test uses reflection to:
            // - Access JsonSerializerOptions.EqualityComparer
            //
            // If either of them changes, this test will need to be kept in sync.

            IEqualityComparer<JsonSerializerOptions> equalityComparer = CreateEqualityComparerAccessor();
            var options1 = new JsonSerializerOptions { WriteIndented = true };
            var options2 = new JsonSerializerOptions { WriteIndented = true };

            Assert.True(equalityComparer.Equals(options1, options2));
            Assert.Equal(equalityComparer.GetHashCode(options1), equalityComparer.GetHashCode(options2));

            _ = new MyJsonContext(options1); // Associate copy with a JsonSerializerContext
            Assert.False(equalityComparer.Equals(options1, options2));
            Assert.NotEqual(equalityComparer.GetHashCode(options1), equalityComparer.GetHashCode(options2));
        }

        private class MyJsonContext : JsonSerializerContext
        {
            public MyJsonContext(JsonSerializerOptions options) : base(options) { }

            public override JsonTypeInfo? GetTypeInfo(Type _) => null;

            protected override JsonSerializerOptions? GeneratedSerializerOptions => Options;
        }

        public static IEqualityComparer<JsonSerializerOptions> CreateEqualityComparerAccessor()
        {
            Type equalityComparerType = typeof(JsonSerializerOptions).GetNestedType("EqualityComparer", BindingFlags.NonPublic);
            Assert.NotNull(equalityComparerType);
            return (IEqualityComparer<JsonSerializerOptions>)Activator.CreateInstance(equalityComparerType, nonPublic: true);
        }

        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                return TestData.WriteSuccessCases;
            }
        }
    }
}
