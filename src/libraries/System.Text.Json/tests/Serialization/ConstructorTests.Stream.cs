// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ConstructorTests
    {
        [Theory]
        [MemberData(nameof(StreamTestData))]
        public static async Task ReadSimpleObjectAsync(Type type, byte[] testData)
        {
            using (MemoryStream stream = new MemoryStream(testData))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                var obj = await JsonSerializer.DeserializeAsync(stream, type, options);
                ((ITestClass)obj).Verify();
            }
        }

        [Theory]
        [MemberData(nameof(StreamTestData))]
        public static async Task ReadSimpleObjectWithTrailingTriviaAsync(Type type, byte[] testData)
        {
            string prefix = Encoding.UTF8.GetString(testData);

            byte[] data = Encoding.UTF8.GetBytes(prefix + " /* Multi\r\nLine Comment */\t");
            using (MemoryStream stream = new MemoryStream(data))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                };

                var obj = await JsonSerializer.DeserializeAsync(stream, type, options);
                ((ITestClass)obj).Verify();
            }
        }

        [Theory]
        [InlineData(typeof(Class_With_Ctor_With_65_Params))]
        [InlineData(typeof(Struct_With_Ctor_With_65_Params))]
        public static async Task Cannot_DeserializeAsync_ObjectWith_Ctor_With_65_Params(Type type)
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

                await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync(stream, type, options));
            }

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializer.DeserializeAsync(stream, type, options));
            }
        }

        private static IEnumerable<object[]> StreamTestData()
        {
            // Simple models can be deserialized.
            yield return new object[] { typeof(Parameterized_IndexViewModel_Immutable), Parameterized_IndexViewModel_Immutable.s_data };
            // Complex models can be deserialized.
            yield return new object[] { typeof(ClassWithConstructor_SimpleAndComplexParameters), ClassWithConstructor_SimpleAndComplexParameters.s_data };
            yield return new object[] { typeof(Parameterized_Class_With_ComplexTuple), Parameterized_Class_With_ComplexTuple.s_data };
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            yield return new object[] { typeof(Person_Class), Person_Class.s_data };
            yield return new object[] { typeof(Person_Struct), Person_Struct.s_data };
            // JSON that doesn't bind to ctor args or properties are sent to ext data if avaiable.
            yield return new object[] { typeof(Parameterized_Person), Parameterized_Person.s_data };
            yield return new object[] { typeof(Parameterized_Person_ObjExtData), Parameterized_Person_ObjExtData.s_data };
            // Up to 64 ctor args are supported.
            yield return new object[] { typeof(Class_With_Ctor_With_64_Params), Class_With_Ctor_With_64_Params.Data };
            // Arg deserialization honors attributes on matching property.
            yield return new object[] { typeof(Point_MembersHave_JsonPropertyName), Point_MembersHave_JsonPropertyName.s_data };
            yield return new object[] { typeof(Point_MembersHave_JsonConverter), Point_MembersHave_JsonConverter.s_data };
            yield return new object[] { typeof(Point_MembersHave_JsonIgnore), Point_MembersHave_JsonIgnore.s_data };
            // Complex JSON as last argument works
            yield return new object[] { typeof(Point_With_Array), Point_With_Array.s_data };
            yield return new object[] { typeof(Point_With_Dictionary), Point_With_Dictionary.s_data };
            yield return new object[] { typeof(Point_With_Object), Point_With_Object.s_data };
        }
    }
}
