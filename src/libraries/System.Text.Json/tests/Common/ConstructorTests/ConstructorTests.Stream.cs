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
        [Theory]
        [MemberData(nameof(ReadSimpleObjectAsync_TestData))]
        public async Task ReadSimpleObjectAsync(Type type, string json)
        {
            if (StreamingSerializer is null)
            {
                return;
            }

            using Utf8MemoryStream stream = new(json);
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            object obj = await StreamingSerializer.DeserializeWrapper(stream, type, options);
            ((ITestClass)obj).Verify();
        }

        public static IEnumerable<object[]> ReadSimpleObjectAsync_TestData()
        {
            // Simple models can be deserialized.
            yield return new object[] { typeof(Parameterized_IndexViewModel_Immutable), Parameterized_IndexViewModel_Immutable.s_json };
            // Complex models can be deserialized.
            yield return new object[] { typeof(ObjWCtorMixedParams), ObjWCtorMixedParams.s_json };
            yield return new object[] { typeof(Parameterized_Class_With_ComplexTuple), Parameterized_Class_With_ComplexTuple.s_json };
            // JSON that doesn't bind to ctor args are matched with properties or ignored (as appropriate).
            yield return new object[] { typeof(Person_Class), Person_Class.s_json };
            yield return new object[] { typeof(Person_Struct), Person_Struct.s_json };
            // JSON that doesn't bind to ctor args or properties are sent to ext data if available.
            yield return new object[] { typeof(Parameterized_Person), Parameterized_Person.s_json };
            yield return new object[] { typeof(Parameterized_Person_ObjExtData), Parameterized_Person_ObjExtData.s_json };
            // Up to 64 ctor args are supported.
            yield return new object[] { typeof(Class_With_Ctor_With_64_Params), Class_With_Ctor_With_64_Params.Json };
            // Arg deserialization honors attributes on matching property.
            yield return new object[] { typeof(Point_MembersHave_JsonPropertyName), Point_MembersHave_JsonPropertyName.s_json };
            yield return new object[] { typeof(Point_MembersHave_JsonConverter), Point_MembersHave_JsonConverter.s_json };
            yield return new object[] { typeof(Point_MembersHave_JsonIgnore), Point_MembersHave_JsonIgnore.s_json };
            yield return new object[] { typeof(Point_MembersHave_JsonInclude), Point_MembersHave_JsonInclude.s_json };
            yield return new object[] { typeof(ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes), ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes.s_json };
            // Complex JSON as last argument works
            yield return new object[] { typeof(Point_With_Array), Point_With_Array.s_json };
            yield return new object[] { typeof(Point_With_Dictionary), Point_With_Dictionary.s_json };
            yield return new object[] { typeof(Point_With_Object), Point_With_Object.s_json };
        }

        [Theory]
        [MemberData(nameof(ReadSimpleObjectAsync_TestData))]
        public async Task ReadSimpleObjectWithTrailingTriviaAsync(Type type, string json)
        {
            if (StreamingSerializer is null)
            {
                return;
            }

            using MemoryStream stream = new Utf8MemoryStream(json + " /* Multi\r\nLine Comment */\t");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            object obj = await StreamingSerializer.DeserializeWrapper(stream, type, options);
            ((ITestClass)obj).Verify();
        }

        [Theory]
        [InlineData(typeof(Class_With_Ctor_With_65_Params))]
        [InlineData(typeof(Struct_With_Ctor_With_65_Params))]
        public async Task Can_DeserializeAsync_ObjectWith_Ctor_With_65_Params(Type type)
        {
            if (StreamingSerializer is null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < 64; i++)
            {
                sb.Append($@"""Int{i}"":{i},");
            }
            sb.Append($@"""Int64"":64");
            sb.Append("}");

            string json = sb.ToString();

            using (MemoryStream stream = new Utf8MemoryStream(json))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                object? value = await StreamingSerializer.DeserializeWrapper(stream, type, options);
                Assert.NotNull(value);
            }

            using (MemoryStream stream = new MemoryStream("{}"u8.ToArray()))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                object? value = await StreamingSerializer.DeserializeWrapper(stream, type, options);
                Assert.NotNull(value);
            }
        }

        [Fact]
        public async Task ExerciseStreamCodePaths()
        {
            if (StreamingSerializer is null)
            {
                return;
            }

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

                    ClassWithStrings obj = await StreamingSerializer.DeserializeWrapper<ClassWithStrings>(stream, options);
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
