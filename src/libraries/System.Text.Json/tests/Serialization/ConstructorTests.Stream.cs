// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        public void ReadSimpleObjectAsync()
        {
            async Task RunTest<T>(byte[] testData)
            {
                using (MemoryStream stream = new MemoryStream(testData))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    var obj = await JsonSerializer.DeserializeAsync<T>(stream, options);
                    ((ITestClass)obj).Verify();
                }
            }

            // Simple models can be deserialized.
            Task.Run(async () => await RunTest<Parameterized_IndexViewModel_Immutable>(Parameterized_IndexViewModel_Immutable.s_data));
            // Complex models can be deserialized.
            Task.Run(async () => await RunTest<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_data));
            Task.Run(async () => await RunTest<Parameterized_Class_With_ComplexTuple>(Parameterized_Class_With_ComplexTuple.s_data));
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            Task.Run(async () => await RunTest<Person_Class>(Person_Class.s_data));
            Task.Run(async () => await RunTest<Person_Struct>(Person_Struct.s_data));
            // JSON that doesn't bind to ctor args or properties are sent to ext data if avaiable.
            Task.Run(async () => await RunTest<Parameterized_Person>(Parameterized_Person.s_data));
            Task.Run(async () => await RunTest<Parameterized_Person_ObjExtData>(Parameterized_Person_ObjExtData.s_data));
            // Up to 64 ctor args are supported.
            Task.Run(async () => await RunTest<Class_With_Ctor_With_64_Params>(Class_With_Ctor_With_64_Params.Data));
            // Arg deserialization honors attributes on matching property.
            Task.Run(async () => await RunTest<Point_MembersHave_JsonPropertyName>(Point_MembersHave_JsonPropertyName.s_data));
            Task.Run(async () => await RunTest<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_data));
            Task.Run(async () => await RunTest<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_data));
            // Complex JSON as last argument works
            Task.Run(async () => await RunTest<Point_With_Array>(Point_With_Array.s_data));
            Task.Run(async () => await RunTest<Point_With_Dictionary>(Point_With_Dictionary.s_data));
            Task.Run(async () => await RunTest<Point_With_Object>(Point_With_Object.s_data));
        }

        [Fact]
        public void ReadSimpleObjectWithTrailingTriviaAsync()
        {
            async Task RunTest<T>(string testData)
            {
                byte[] data = Encoding.UTF8.GetBytes(testData + " /* Multi\r\nLine Comment */\t");
                using (MemoryStream stream = new MemoryStream(data))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                    };

                    var obj = await JsonSerializer.DeserializeAsync<T>(stream, options);
                    ((ITestClass)obj).Verify();
                }
            }

            // Simple models can be deserialized.
            Task.Run(async () => await RunTest<Parameterized_IndexViewModel_Immutable>(Parameterized_IndexViewModel_Immutable.s_json));
            // Complex models can be deserialized.
            Task.Run(async () => await RunTest<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json));
            Task.Run(async () => await RunTest<Parameterized_Class_With_ComplexTuple>(Parameterized_Class_With_ComplexTuple.s_json));
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            Task.Run(async () => await RunTest<Person_Class>(Person_Class.s_json));
            Task.Run(async () => await RunTest<Person_Struct>(Person_Struct.s_json));
            // JSON that doesn't bind to ctor args or properties are sent to ext data if avaiable.
            Task.Run(async () => await RunTest<Parameterized_Person>(Parameterized_Person.s_json));
            Task.Run(async () => await RunTest<Parameterized_Person_ObjExtData>(Parameterized_Person_ObjExtData.s_json));
            // Up to 64 ctor args are supported.
            Task.Run(async () => await RunTest<Class_With_Ctor_With_64_Params>(Encoding.UTF8.GetString(Class_With_Ctor_With_64_Params.Data)));
            // Arg deserialization honors attributes on matching property.
            Task.Run(async () => await RunTest<Point_MembersHave_JsonPropertyName>(Point_MembersHave_JsonPropertyName.s_json));
            Task.Run(async () => await RunTest<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_json));
            Task.Run(async () => await RunTest<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_json));
            // Complex JSON as last argument works
            Task.Run(async () => await RunTest<Point_With_Array>(Point_With_Array.s_json));
            Task.Run(async () => await RunTest<Point_With_Dictionary>(Point_With_Dictionary.s_json));
            Task.Run(async () => await RunTest<Point_With_Object>(Point_With_Object.s_json));
        }

        [Fact]
        public void Cannot_DeserializeAsync_ObjectWith_Ctor_With_65_Params()
        {
            async Task RunTest<T>()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                for (int i = 0; i < 64; i++)
                {
                    sb.Append($@"""Int{i}"":{i},");
                }
                sb.Append($@"""Int64"":64");
                sb.Append("}");

                string input = sb.ToString();

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync<T>(stream, options));
                }

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync<T>(stream, options));
                }
            }

            Task.Run(async () => await RunTest<Class_With_Ctor_With_65_Params>());
            Task.Run(async () => await RunTest<Struct_With_Ctor_With_65_Params>());
        }
    }
}
