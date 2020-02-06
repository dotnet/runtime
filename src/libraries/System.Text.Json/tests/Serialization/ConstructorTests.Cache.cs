// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Fact, OuterLoop]
        public static void MultipleThreadsLooping()
        {
            const int Iterations = 100;

            for (int i = 0; i < Iterations; i++)
            {
                MultipleThreads();
            }
        }

        [Fact]
        public static void MultipleThreads()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            // Verify the test class has >32 properties since that is a threshold for using the fallback dictionary.
            Assert.True(typeof(ClassWithConstructor_SimpleAndComplexParameters).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length > 32);

            void DeserializeObjectMinimal()
            {
                var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MyDecimal"" : 3.3}", options);
            };

            void DeserializeObjectFlipped()
            {
                var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(
                    ClassWithConstructor_SimpleAndComplexParameters.s_json_flipped, options);
                obj.Verify();
            };

            void DeserializeObjectNormal()
            {
                var obj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(
                    ClassWithConstructor_SimpleAndComplexParameters.s_json, options);
                obj.Verify();
            };

            void SerializeObject()
            {
                var obj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
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

            Task.WaitAll(tasks);
        }

        [Fact]
        public static void PropertyCacheWithMinInputsFirst()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            string json = "{}";
            JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            json = JsonSerializer.Serialize(testObj, options);
            testObj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();
        }

        [Fact]
        public static void PropertyCacheWithMinInputsLast()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            string json = JsonSerializer.Serialize(testObj, options);
            testObj = JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();

            json = "{}";
            JsonSerializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
        }

        // Use a common options instance to encourage additional metadata collisions across types. Also since
        // this options is not the default options instance the tests will not use previously cached metadata.
        private static JsonSerializerOptions s_options = new JsonSerializerOptions();

        [Theory]
        [MemberData(nameof(MultipleTypesTestData))]
        public static void MultipleTypes(ITestClass testObj, object[] args)
        {
            Type type = testObj.GetType();

            // Get the test json with the default options to avoid cache pollution of Deserialize() below.
            testObj.Initialize();
            testObj.Verify();
            string json = JsonSerializer.Serialize(testObj, type);

            void Serialize()
            {
                ITestClass localTestObj = (ITestClass)Activator.CreateInstance(type, args);
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

            Task.WaitAll(tasks);
        }

        public static IEnumerable<object[]> MultipleTypesTestData()
        {
            yield return new object[] { new Point_2D(1, 2), new object[] { 1, 2 } };
            yield return new object[] { new Point_3D(1, 2, 3), new object[] { 1, 2, 3 } };
            yield return new object[] { new Point_2D_With_ExtData(1, 2), new object[] { 1, 2 } };

            Guid id = Guid.Parse("270bb22b-4816-4bd9-9acd-8ec5b1a896d3");
            yield return new object[] { new Parameterized_Person_Simple(id), new object[] { id } };
            yield return new object[] { new Point_MembersHave_JsonPropertyName(1, 2), new object[] { 1, 2 } };
        }
    }
}
