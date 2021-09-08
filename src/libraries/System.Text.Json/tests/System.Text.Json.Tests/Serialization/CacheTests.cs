// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58204", TestPlatforms.iOS | TestPlatforms.tvOS)]
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

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static async Task JsonSerializerOptionsUpdateHandler_ClearingDoesntPreventSerialization()
        {
            // This test uses reflection to:
            // - Access JsonSerializerOptions._classes
            // - Access JsonSerializerOptionsUpdateHandler.ClearCache
            //
            // If either of them changes, this test will need to be kept in sync.

            var options = new JsonSerializerOptions();

            FieldInfo classesField = options.GetType().GetField("_classes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(classesField);
            IDictionary classes = (IDictionary)classesField.GetValue(options);
            Assert.Equal(0, classes.Count);

            SimpleTestClass testObj = new SimpleTestClass();
            testObj.Initialize();
            await JsonSerializer.SerializeAsync<SimpleTestClass>(new MemoryStream(), testObj, options);
            Assert.NotEqual(0, classes.Count);

            Type updateHandler = typeof(JsonSerializerOptions).Assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler", throwOnError: true, ignoreCase: false);
            MethodInfo clearCache = updateHandler.GetMethod("ClearCache");
            Assert.NotNull(clearCache);
            clearCache.Invoke(null, new object[] { null });
            Assert.Equal(0, classes.Count);

            await JsonSerializer.SerializeAsync<SimpleTestClass>(new MemoryStream(), testObj, options);
            Assert.NotEqual(0, classes.Count);
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
