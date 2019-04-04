// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class CacheTests
    {
        [Fact]
        public static void MultipleThreads()
        {
            // Ensure no exceptions are thrown due to caching or other issues.
            void SerializeAndDeserializeObject(bool useEmptyJson)
            {
                // Use localized caching
                // Todo: localized caching not implemented yet. When implemented, add a run-time attribute to JsonSerializerOptions as that will create a separate cache held by JsonSerializerOptions.
                var options = new JsonSerializerOptions();

                string json;

                if (useEmptyJson)
                {
                    json = "{}";
                }
                else
                {
                    SimpleTestClass testObj = new SimpleTestClass();
                    testObj.Initialize();
                    testObj.Verify();

                    json = JsonSerializer.ToString(testObj, options);
                }

                SimpleTestClass testObjDeserialized = JsonSerializer.Parse<SimpleTestClass>(json, options);
                testObjDeserialized.Verify();
            };

            Task[] tasks = new Task[8];
            bool useEmptyJson = false;
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => SerializeAndDeserializeObject(useEmptyJson));
                useEmptyJson = !useEmptyJson;
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
