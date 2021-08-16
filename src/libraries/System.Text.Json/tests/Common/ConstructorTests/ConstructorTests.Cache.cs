// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        [OuterLoop]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task MultipleThreadsLooping()
        {
            const int Iterations = 100;

            for (int i = 0; i < Iterations; i++)
            {
                await MultipleThreads();
            }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task MultipleThreads()
        {
            // Verify the test class has >32 properties since that is a threshold for using the fallback dictionary.
            Assert.True(typeof(ClassWithConstructor_SimpleAndComplexParameters).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length > 32);

            async Task DeserializeObjectAsync(string json, Type type, JsonSerializerOptions options)
            {
                var obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, type, options);
                ((ITestClassWithParameterizedCtor)obj).Verify();
            }

            async Task DeserializeObjectMinimalAsync(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json_minimal").GetValue(null);
                var obj = await JsonSerializerWrapperForString.DeserializeWrapper(json, type, options);
                ((ITestClassWithParameterizedCtor)obj).VerifyMinimal();
            };

            async Task DeserializeObjectFlippedAsync(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json_flipped").GetValue(null);
                await DeserializeObjectAsync(json, type, options);
            };

            async Task DeserializeObjectNormalAsync(Type type, JsonSerializerOptions options)
            {
                string json = (string)type.GetProperty("s_json").GetValue(null);
                await DeserializeObjectAsync(json, type, options);
            };

            async Task SerializeObject(Type type, JsonSerializerOptions options)
            {
                var obj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
                await JsonSerializerWrapperForString.SerializeWrapper(obj, options);
            };

            async Task RunTestAsync(Type type)
            {
                // Use local options to avoid obtaining already cached metadata from the default options.
                var options = new JsonSerializerOptions();

                const int ThreadCount = 8;
                const int ConcurrentTestsCount = 4;
                Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

                for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
                {
                    // Create race condition to populate the sorted property cache with different json ordering.
                    tasks[i + 0] = Task.Run(() => DeserializeObjectMinimalAsync(type, options));
                    tasks[i + 1] = Task.Run(() => DeserializeObjectFlippedAsync(type, options));
                    tasks[i + 2] = Task.Run(() => DeserializeObjectNormalAsync(type, options));

                    // Ensure no exceptions on serialization
                    tasks[i + 3] = Task.Run(() => SerializeObject(type, options));
                };

                await Task.WhenAll(tasks);
            }

            await RunTestAsync(typeof(ClassWithConstructor_SimpleAndComplexParameters));
            await RunTestAsync(typeof(Person_Class));
            await RunTestAsync(typeof(Parameterized_Class_With_ComplexTuple));
        }

        [Fact]
        public async Task PropertyCacheWithMinInputsFirst()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            string json = "{}";
            await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(json, options);

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            json = await JsonSerializerWrapperForString.SerializeWrapper(testObj, options);
            testObj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();
        }

        [Fact]
        public async Task PropertyCacheWithMinInputsLast()
        {
            // Use local options to avoid obtaining already cached metadata from the default options.
            var options = new JsonSerializerOptions();

            ClassWithConstructor_SimpleAndComplexParameters testObj = ClassWithConstructor_SimpleAndComplexParameters.GetInstance();
            testObj.Verify();

            string json = await JsonSerializerWrapperForString.SerializeWrapper(testObj, options);
            testObj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
            testObj.Verify();

            json = "{}";
            await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(json, options);
        }

        // Use a common options instance to encourage additional metadata collisions across types. Also since
        // this options is not the default options instance the tests will not use previously cached metadata.
        private JsonSerializerOptions s_options = new JsonSerializerOptions();

        [Fact]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", RuntimeConfiguration.Checked)]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task MultipleTypes()
        {
            async Task Serialize<T>(object[] args)
            {
                Type type = typeof(T);

                T localTestObj = (T)Activator.CreateInstance(type, args);
                ((ITestClass)localTestObj).Initialize();
                ((ITestClass)localTestObj).Verify();
                string json = await JsonSerializerWrapperForString.SerializeWrapper(localTestObj, s_options);
            };

            async Task DeserializeAsync<T>(string json)
            {
                ITestClass obj = (ITestClass)await JsonSerializerWrapperForString.DeserializeWrapper<T>(json, s_options);
                obj.Verify();
            };

            async Task RunTestAsync<T>(T testObj, object[] args)
            {
                // Get the test json with the default options to avoid cache pollution of DeserializeAsync() below.
                ((ITestClass)testObj).Initialize();
                ((ITestClass)testObj).Verify();
                string json = await JsonSerializerWrapperForString.SerializeWrapper(testObj);

                const int ThreadCount = 12;
                const int ConcurrentTestsCount = 2;
                Task[] tasks = new Task[ThreadCount * ConcurrentTestsCount];

                for (int i = 0; i < tasks.Length; i += ConcurrentTestsCount)
                {
                    tasks[i + 0] = Task.Run(() => DeserializeAsync<T>(json));
                    tasks[i + 1] = Task.Run(() => Serialize<T>(args));
                };

                await Task.WhenAll(tasks);
            }

            await RunTestAsync<Point_2D>(new Point_2D(1, 2), new object[] { 1, 2 });
            await RunTestAsync(new Point_3D(1, 2, 3), new object[] { 1, 2, 3 });
            await RunTestAsync(new Point_2D_With_ExtData(1, 2), new object[] { 1, 2 });

            Guid id = Guid.Parse("270bb22b-4816-4bd9-9acd-8ec5b1a896d3");
            await RunTestAsync(new Parameterized_Person_Simple(id), new object[] { id });
            await RunTestAsync(new Point_MembersHave_JsonPropertyName(1, 2), new object[] { 1, 2 });
        }
    }
}
