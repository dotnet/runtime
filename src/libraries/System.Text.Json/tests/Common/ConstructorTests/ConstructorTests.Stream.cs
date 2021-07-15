// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", RuntimeConfiguration.Checked)]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task ReadSimpleObjectAsync()
        {
            async Task RunTestAsync<T>(byte[] testData)
            {
                using (MemoryStream stream = new MemoryStream(testData))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    var obj = await JsonSerializerWrapperForStream.DeserializeWrapper<T>(stream, options);
                    ((ITestClass)obj).Verify();
                }
            }

            // Array size is the count of the following tests.
            Task[] tasks = new Task[16];

            // Simple models can be deserialized.
            tasks[0] = Task.Run(async () => await RunTestAsync<Parameterized_IndexViewModel_Immutable>(Parameterized_IndexViewModel_Immutable.s_data));
            // Complex models can be deserialized.
            tasks[1] = Task.Run(async () => await RunTestAsync<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_data));
            tasks[2] = Task.Run(async () => await RunTestAsync<Parameterized_Class_With_ComplexTuple>(Parameterized_Class_With_ComplexTuple.s_data));
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            tasks[3] = Task.Run(async () => await RunTestAsync<Person_Class>(Person_Class.s_data));
            tasks[4] = Task.Run(async () => await RunTestAsync<Person_Struct>(Person_Struct.s_data));
            // JSON that doesn't bind to ctor args or properties are sent to ext data if avaiable.
            tasks[5] = Task.Run(async () => await RunTestAsync<Parameterized_Person>(Parameterized_Person.s_data));
            tasks[6] = Task.Run(async () => await RunTestAsync<Parameterized_Person_ObjExtData>(Parameterized_Person_ObjExtData.s_data));
            // Up to 64 ctor args are supported.
            tasks[7] = Task.Run(async () => await RunTestAsync<Class_With_Ctor_With_64_Params>(Class_With_Ctor_With_64_Params.Data));
            // Arg deserialization honors attributes on matching property.
            tasks[8] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonPropertyName>(Point_MembersHave_JsonPropertyName.s_data));
            tasks[9] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_data));
            tasks[10] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_data));
            tasks[11] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonInclude>(Point_MembersHave_JsonInclude.s_data));
            tasks[12] = Task.Run(async () => await RunTestAsync<ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes>(ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes.s_data));
            // Complex JSON as last argument works
            tasks[13] = Task.Run(async () => await RunTestAsync<Point_With_Array>(Point_With_Array.s_data));
            tasks[14] = Task.Run(async () => await RunTestAsync<Point_With_Dictionary>(Point_With_Dictionary.s_data));
            tasks[15] = Task.Run(async () => await RunTestAsync<Point_With_Object>(Point_With_Object.s_data));

            await Task.WhenAll(tasks);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task ReadSimpleObjectWithTrailingTriviaAsync()
        {
            async Task RunTestAsync<T>(string testData)
            {
                byte[] data = Encoding.UTF8.GetBytes(testData + " /* Multi\r\nLine Comment */\t");
                using (MemoryStream stream = new MemoryStream(data))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                    };

                    var obj = await JsonSerializerWrapperForStream.DeserializeWrapper<T>(stream, options);
                    ((ITestClass)obj).Verify();
                }
            }

            // Array size is the count of the following tests.
            Task[] tasks = new Task[14];

            // Simple models can be deserialized.
            tasks[0] = Task.Run(async () => await RunTestAsync<Parameterized_IndexViewModel_Immutable>(Parameterized_IndexViewModel_Immutable.s_json));
            // Complex models can be deserialized.
            tasks[1] = Task.Run(async () => await RunTestAsync<ClassWithConstructor_SimpleAndComplexParameters>(ClassWithConstructor_SimpleAndComplexParameters.s_json));
            tasks[2] = Task.Run(async () => await RunTestAsync<Parameterized_Class_With_ComplexTuple>(Parameterized_Class_With_ComplexTuple.s_json));
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            tasks[3] = Task.Run(async () => await RunTestAsync<Person_Class>(Person_Class.s_json));
            tasks[4] = Task.Run(async () => await RunTestAsync<Person_Struct>(Person_Struct.s_json));
            // JSON that doesn't bind to ctor args or properties are sent to ext data if avaiable.
            tasks[5] = Task.Run(async () => await RunTestAsync<Parameterized_Person>(Parameterized_Person.s_json));
            tasks[6] = Task.Run(async () => await RunTestAsync<Parameterized_Person_ObjExtData>(Parameterized_Person_ObjExtData.s_json));
            // Up to 64 ctor args are supported.
            tasks[7] = Task.Run(async () => await RunTestAsync<Class_With_Ctor_With_64_Params>(Encoding.UTF8.GetString(Class_With_Ctor_With_64_Params.Data)));
            // Arg8deserialization honors attributes on matching property.
            tasks[8] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonPropertyName>(Point_MembersHave_JsonPropertyName.s_json));
            tasks[9] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonConverter>(Point_MembersHave_JsonConverter.s_json));
            tasks[10] = Task.Run(async () => await RunTestAsync<Point_MembersHave_JsonIgnore>(Point_MembersHave_JsonIgnore.s_json));
            // Complex JSON as last argument works
            tasks[11] = Task.Run(async () => await RunTestAsync<Point_With_Array>(Point_With_Array.s_json));
            tasks[12] = Task.Run(async () => await RunTestAsync<Point_With_Dictionary>(Point_With_Dictionary.s_json));
            tasks[13] = Task.Run(async () => await RunTestAsync<Point_With_Object>(Point_With_Object.s_json));

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Cannot_DeserializeAsync_ObjectWith_Ctor_With_65_Params()
        {
            async Task RunTestAsync<T>()
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

                    await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForStream.DeserializeWrapper<T>(stream, options));
                }

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForStream.DeserializeWrapper<T>(stream, options));
                }
            }

            Task[] tasks = new Task[2];

            tasks[0] = Task.Run(async () => await RunTestAsync<Class_With_Ctor_With_65_Params>());
            tasks[1] = Task.Run(async () => await RunTestAsync<Struct_With_Ctor_With_65_Params>());

            await Task.WhenAll(tasks);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task ExerciseStreamCodePaths()
        {
            static string GetPropertyName(int index) =>
                new string(new char[] { Convert.ToChar(index + 65), 'V', 'a', 'l', 'u', 'e' });

            static byte[] GeneratePayload(int i, string value)
            {
                string whiteSpace = new string(' ', 16);

                StringBuilder sb;

                string prefix = "";

                sb = new StringBuilder();
                sb.Append("{");

                for (int j = 0; j < i; j++)
                {
                    sb.Append(prefix);
                    sb.Append($@"""{GetPropertyName(j)}"":""{value}""");
                    prefix = ",";
                }

                sb.Append(prefix);
                sb.Append($@"{whiteSpace}""{GetPropertyName(i)}"":{whiteSpace}""{value}""");
                prefix = ",";

                for (int j = 0; j < 10; j++)
                {
                    sb.Append(prefix);
                    string keyPair = $@"""rand"":[""{value}""]";
                    sb.Append($@"""Value{j}"":{{{keyPair},{keyPair},{keyPair}}}");
                }

                for (int j = i + 1; j < 20; j++)
                {
                    sb.Append(prefix);
                    sb.Append($@"""{GetPropertyName(j)}"":""{value}""");
                }

                sb.Append("}");

                return Encoding.UTF8.GetBytes(sb.ToString());
            }

            const string value = "ul4Oolt4VgbNm5Y1qPX911wxhyHFEQmmWBcIBR6BfUaNuIn3YOJ8vqtqz2WAh924rEILMzlh6JUhQDcmH00SI6Kv4iGTHQfGXxqWul4Oolt4VgbNm5Y1qPX911wxhyHFEQmmWBcIBR6";

            for (int i = 0; i < 20; i++)
            {
                using (MemoryStream stream = new MemoryStream(GeneratePayload(i, value)))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = 1
                    };

                    ClassWithStrings obj = await JsonSerializerWrapperForStream.DeserializeWrapper<ClassWithStrings>(stream, options);
                    obj.Verify(value);
                }
            }
        }

        public class ClassWithStrings
        {
            // Ctor args.

            // Ignored.

            [JsonIgnore]
            public string AValue { get; }
            [JsonIgnore]
            public string EValue { get; }
            [JsonIgnore]
            public string IValue { get; }
            [JsonIgnore]
            public string MValue { get; }
            [JsonIgnore]
            public string QValue { get; }

            // Populated.

            public string CValue { get; }
            public string GValue { get; }
            public string KValue { get; }
            public string OValue { get; }
            public string SValue { get; }

            // Properties.

            // Ignored - no setter.

            public string BValue { get; }
            public string FValue { get; }
            public string JValue { get; }
            public string NValue { get; }
            public string RValue { get; }

            // Populated.

            public string DValue { get; set; }
            public string HValue { get; set; }
            public string LValue { get; set; }
            public string PValue { get; set; }
            public string TValue { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

            public ClassWithStrings(
                string aValue,
                string cValue,
                string eValue,
                string gValue,
                string iValue,
                string kValue,
                string mValue,
                string oValue,
                string qValue,
                string sValue)
            {
                AValue = aValue;
                CValue = cValue;
                EValue = eValue;
                GValue = gValue;
                IValue = iValue;
                KValue = kValue;
                MValue = mValue;
                OValue = oValue;
                QValue = qValue;
                SValue = sValue;
            }

            public void Verify(string expectedStr)
            {
                // Ctor args.

                // Ignored
                Assert.Null(AValue);
                Assert.Null(EValue);
                Assert.Null(IValue);
                Assert.Null(MValue);
                Assert.Null(QValue);

                Assert.Equal(expectedStr, CValue);
                Assert.Equal(expectedStr, GValue);
                Assert.Equal(expectedStr, KValue);
                Assert.Equal(expectedStr, OValue);
                Assert.Equal(expectedStr, SValue);

                // Getter only members - skipped.
                Assert.Null(BValue);
                Assert.Null(FValue);
                Assert.Null(JValue);
                Assert.Null(NValue);
                Assert.Null(RValue);

                // Members with setters
                Assert.Equal(expectedStr, DValue);
                Assert.Equal(expectedStr, HValue);
                Assert.Equal(expectedStr, LValue);
                Assert.Equal(expectedStr, PValue);
                Assert.Equal(expectedStr, TValue);

                Assert.Equal(10, ExtensionData.Count);

                foreach (JsonElement value in ExtensionData.Values)
                {
                    string keyPair = $@"""rand"":[""{expectedStr}""]";
                    Assert.Equal($@"{{{keyPair},{keyPair},{keyPair}}}", value.GetRawText());
                }
            }
        }
    }
}
