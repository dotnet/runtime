// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CacheTests
    {
        // Use a new type that is not used in other tests so we can attempt race conditions on cached global state.
        public class TestClassForCachingTest : SimpleTestClass { }

        [Fact]
        public static void MultipleThreads()
        {
            void DeserializeObjectFlipped()
            {
                TestClassForCachingTest obj = JsonSerializer.Parse<TestClassForCachingTest>(SimpleTestClass.s_json_flipped);
                obj.Verify();
            };

            void DeserializeObjectNormal()
            {
                TestClassForCachingTest obj = JsonSerializer.Parse<TestClassForCachingTest>(SimpleTestClass.s_json);
                obj.Verify();
            };

            void SerializeObject()
            {
                var obj = new TestClassForCachingTest();
                obj.Initialize();
                JsonSerializer.ToString(obj);
            };

            Task[] tasks = new Task[4 * 3];
            for (int i = 0; i < tasks.Length; i += 3)
            {
                // Create race condition to populate the sorted property cache with different json ordering.
                tasks[i + 0] = Task.Run(() => DeserializeObjectFlipped());
                tasks[i + 1] = Task.Run(() => DeserializeObjectNormal());

                // Ensure no exceptions on serialization
                tasks[i + 2] = Task.Run(() => SerializeObject());
            };

            Task.WaitAll(tasks);
        }

        [Fact]
        public static void PropertyCacheWithMinInputsFirst()
        {
            // Use localized caching
            // Todo: localized caching not implemented yet. When implemented, add a run-time attribute to JsonSerializerOptions as that will create a separate cache held by JsonSerializerOptions.
            var options = new JsonSerializerOptions();

            string json = "{}";
            JsonSerializer.Parse<SimpleTestClass>(json, options);

            SimpleTestClass testObj = new SimpleTestClass();
            testObj.Initialize();
            testObj.Verify();

            json = JsonSerializer.ToString(testObj, options);
            testObj = JsonSerializer.Parse<SimpleTestClass>(json, options);
            testObj.Verify();
        }

        [Fact]
        public static void PropertyCacheWithMinInputsLast()
        {
            // Use localized caching
            // Todo: localized caching not implemented yet. When implemented, add a run-time attribute to JsonSerializerOptions as that will create a separate cache held by JsonSerializerOptions.
            var options = new JsonSerializerOptions();

            SimpleTestClass testObj = new SimpleTestClass();
            testObj.Initialize();
            testObj.Verify();

            string json = JsonSerializer.ToString(testObj, options);
            testObj = JsonSerializer.Parse<SimpleTestClass>(json, options);
            testObj.Verify();

            json = "{}";
            JsonSerializer.Parse<SimpleTestClass>(json, options);
        }
    }
}
