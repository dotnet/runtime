// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact, OuterLoop]
        public void MultipleThreadsLooping()
        {
            const int Iterations = 100;

            for (int i = 0; i < Iterations; i++)
            {
                MultipleThreads();
            }
        }

        [Fact]
        public void MultipleThreads()
        {
            // Verify the test class has >32 properties since that is a threshold for using the fallback dictionary.
            Assert.True(typeof(ClassWithConstructor_SimpleAndComplexParameters).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length > 32);

            void DeserializeObject(string json, Type type, JsonSerializerOptions options)
            {
                var obj = Serializer.Deserialize(json, type, options);
                ((ITestClassWithParameterizedCtor)obj).Verify();
            }

            void DeserializeObjectMinimal(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json_minimal").GetValue(null);
                var obj = Serializer.Deserialize(json, type, options);
                ((ITestClassWithParameterizedCtor)obj).VerifyMinimal();
            };

            void DeserializeObjectFlipped(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json_flipped").GetValue(null);
                DeserializeObject(json, type, options);
            };

            void DeserializeObjectNormal(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json").GetValue(null);
                DeserializeObject(json, type, options);
            };

            void SerializeObject(Type type, JsonSerializerOptions options)
            {
                var obj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
                JsonSerializer.Serialize(obj, options);
            };

            void RunTest(Type type)
            {
                // Use local options to avoid obtaining already cached metadata from the default options.
                var options = new JsonSerializerOptions();

                const int ThreadCount = 8;
                const int ConcurrentTestsCount = 4;
                Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

                for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
                {
                    // Create race condition to populate the sorted property cache with different json ordering.
                    tasks[i + 0] = Task.Run(() => DeserializeObjectMinimal(type, options));
                    tasks[i + 1] = Task.Run(() => DeserializeObjectFlipped(type, options));
                    tasks[i + 2] = Task.Run(() => DeserializeObjectNormal(type, options));

                    // Ensure no exceptions on serialization
                    tasks[i + 3] = Task.Run(() => SerializeObject(type, options));
                };

                Task.WaitAll(tasks);
            }

            RunTest(typeof(ClassWithConstructor_SimpleAndComplexParameters));
            RunTest(typeof(Person_Class));
            RunTest(typeof(Parameterized_Class_With_ComplexTuple));
        }

        [Fact]
        public void PropertyCacheWithMinInputsFirst()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            string json = "{}";
            Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            json = JsonSerializer.Serialize(testObj, options);
            testObj = Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();
        }

        [Fact]
        public void PropertyCacheWithMinInputsLast()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            string json = JsonSerializer.Serialize(testObj, options);
            testObj = Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();

            json = "{}";
            Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
        }

        // Use a common options instance to encourage additional metadata collisions across types. Also since
        // this options is not the default options instance the tests will not use previously cached metadata.
        private JsonSerializerOptions s_options = new JsonSerializerOptions();

        [Fact]
        public void MultipleTypes()
        {
            void Serialize<T>(object[] args)
            {
                Type type = typeof(T);

                T localTestObj = (T)Activator.CreateInstance(type, args);
                ((ITestClass)localTestObj).Initialize();
                ((ITestClass)localTestObj).Verify();
                string json = JsonSerializer.Serialize(localTestObj, s_options);
            };

            void Deserialize<T>(string json)
            {
                ITestClass obj = (ITestClass)Serializer.Deserialize<T>(json, s_options);
                obj.Verify();
            };

            void RunTest<T>(T testObj, object[] args)
            {
                // Get the test json with the default options to avoid cache pollution of Deserialize() below.
                ((ITestClass)testObj).Initialize();
                ((ITestClass)testObj).Verify();
                string json = JsonSerializer.Serialize(testObj);

                const int ThreadCount = 12;
                const int ConcurrentTestsCount = 2;
                Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

                for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
                {
                    tasks[i + 0] = Task.Run(() => Deserialize<T>(json));
                    tasks[i + 1] = Task.Run(() => Serialize<T>(args));
                };

                Task.WaitAll(tasks);
            }

            RunTest<Point_2D>(new Point_2D(1, 2), new object[] { 1, 2 });
            RunTest(new Point_3D(1, 2, 3), new object[] { 1, 2, 3 });
            RunTest(new Point_2D_With_ExtData(1, 2), new object[] { 1, 2 });

            Guid id = Guid.Parse("270bb22b-4816-4bd9-9acd-8ec5b1a896d3");
            RunTest(new Parameterized_Person_Simple(id), new object[] { id });
            RunTest(new Point_MembersHave_JsonPropertyName(1, 2), new object[] { 1, 2 });
        }
    }
}
