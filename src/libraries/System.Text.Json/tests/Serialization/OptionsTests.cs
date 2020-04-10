// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class OptionsTests
    {
        private class TestConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void SetOptionsFail()
        {
            var options = new JsonSerializerOptions();

            // Verify these do not throw.
            options.Converters.Clear();
            TestConverter tc = new TestConverter();
            options.Converters.Add(tc);
            options.Converters.Insert(0, new TestConverter());
            options.Converters.Remove(tc);
            options.Converters.RemoveAt(0);

            // Add one item for later.
            options.Converters.Add(tc);

            // Verify converter collection throws on null adds.
            Assert.Throws<ArgumentNullException>(() => options.Converters.Add(null));
            Assert.Throws<ArgumentNullException>(() => options.Converters.Insert(0, null));
            Assert.Throws<ArgumentNullException>(() => options.Converters[0] = null);

            // Perform serialization.
            JsonSerializer.Deserialize<int>("1", options);

            // Verify defaults and ensure getters do not throw.
            Assert.False(options.AllowTrailingCommas);
            Assert.Equal(16 * 1024, options.DefaultBufferSize);
            Assert.Null(options.DictionaryKeyPolicy);
            Assert.Null(options.Encoder);
            Assert.False(options.IgnoreNullValues);
            Assert.Equal(0, options.MaxDepth);
            Assert.False(options.PropertyNameCaseInsensitive);
            Assert.Null(options.PropertyNamingPolicy);
            Assert.Equal(JsonCommentHandling.Disallow, options.ReadCommentHandling);
            Assert.False(options.WriteIndented);

            Assert.Equal(tc, options.Converters[0]);
            Assert.True(options.Converters.Contains(tc));
            options.Converters.CopyTo(new JsonConverter[1] { null }, 0);
            Assert.Equal(1, options.Converters.Count);
            Assert.False(options.Converters.Equals(tc));
            Assert.NotNull(options.Converters.GetEnumerator());
            Assert.Equal(0, options.Converters.IndexOf(tc));
            Assert.False(options.Converters.IsReadOnly);

            // Setters should always throw; we don't check to see if the value is the same or not.
            Assert.Throws<InvalidOperationException>(() => options.AllowTrailingCommas = options.AllowTrailingCommas);
            Assert.Throws<InvalidOperationException>(() => options.DefaultBufferSize = options.DefaultBufferSize);
            Assert.Throws<InvalidOperationException>(() => options.DictionaryKeyPolicy = options.DictionaryKeyPolicy);
            Assert.Throws<InvalidOperationException>(() => options.Encoder = JavaScriptEncoder.Default);
            Assert.Throws<InvalidOperationException>(() => options.IgnoreNullValues = options.IgnoreNullValues);
            Assert.Throws<InvalidOperationException>(() => options.MaxDepth = options.MaxDepth);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNamingPolicy = options.PropertyNamingPolicy);
            Assert.Throws<InvalidOperationException>(() => options.ReadCommentHandling = options.ReadCommentHandling);
            Assert.Throws<InvalidOperationException>(() => options.WriteIndented = options.WriteIndented);

            Assert.Throws<InvalidOperationException>(() => options.Converters[0] = tc);
            Assert.Throws<InvalidOperationException>(() => options.Converters.Clear());
            Assert.Throws<InvalidOperationException>(() => options.Converters.Add(tc));
            Assert.Throws<InvalidOperationException>(() => options.Converters.Insert(0, new TestConverter()));
            Assert.Throws<InvalidOperationException>(() => options.Converters.Remove(tc));
            Assert.Throws<InvalidOperationException>(() => options.Converters.RemoveAt(0));
        }

        [Fact]
        public static void DefaultBufferSizeFail()
        {
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = 0);
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = -1);
        }

        [Fact]
        public static void DefaultBufferSize()
        {
            var options = new JsonSerializerOptions();

            Assert.Equal(16 * 1024, options.DefaultBufferSize);

            options.DefaultBufferSize = 1;
            Assert.Equal(1, options.DefaultBufferSize);
        }

        [Fact]
        public static void AllowTrailingCommas()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>("[1,]"));

            var options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;

            int[] value = JsonSerializer.Deserialize<int[]>("[1,]", options);
            Assert.Equal(1, value[0]);
        }

        [Fact]
        public static void WriteIndented()
        {
            var obj = new BasicCompany();
            obj.Initialize();

            // Verify default value.
            string json = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Verify default value on options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.Serialize(obj, options);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Change the value on options.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(Environment.NewLine, json);
        }

        [Fact]
        public static void ExtensionDataUsesReaderOptions()
        {
            // We just verify trailing commas.
            const string json = @"{""MyIntMissing"":2,}";

            // Verify baseline without options.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithExtensionProperty>(json));

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithExtensionProperty>(json, options));

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            JsonSerializer.Deserialize<ClassWithExtensionProperty>(json, options);
        }

        [Fact]
        public static void ExtensionDataUsesWriterOptions()
        {
            // We just verify whitespace.

            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(@"{""MyIntMissing"":2}");

            // Verify baseline without options.
            string json = JsonSerializer.Serialize(obj);
            Assert.False(HasNewLine());

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.Serialize(obj, options);
            Assert.False(HasNewLine());

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.Serialize(obj, options);
            Assert.True(HasNewLine());

            bool HasNewLine()
            {
                int iEnd = json.IndexOf("2", json.IndexOf("MyIntMissing"));
                return json.Substring(iEnd + 1).StartsWith(Environment.NewLine);
            }
        }

        [Fact]
        public static void ReadCommentHandling()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>("/* commment */"));

            var options = new JsonSerializerOptions();

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>("/* commment */", options));

            options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            int value = JsonSerializer.Deserialize<int>("1 /* commment */", options);
            Assert.Equal(1, value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((int)JsonCommentHandling.Allow)]
        [InlineData(3)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 3)] // Other values, like byte.MaxValue + 1 overflows to 0 (i.e. JsonCommentHandling.Disallow), which is valid.
        [InlineData(byte.MaxValue + 4)]
        public static void ReadCommentHandlingDoesNotSupportAllow(int enumValue)
        {
            var options = new JsonSerializerOptions();

            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.ReadCommentHandling = (JsonCommentHandling)enumValue);
        }

        [Theory]
        [InlineData(-1)]
        public static void TestDepthInvalid(int depth)
        {
            var options = new JsonSerializerOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.MaxDepth = depth);
        }

        [Fact]
        public static void MaxDepthRead()
        {
            JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data);

            var options = new JsonSerializerOptions();

            JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data, options);

            options = new JsonSerializerOptions();
            options.MaxDepth = 1;

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data, options));
        }

        private class TestClassForEncoding
        {
            public string MyString { get; set; }
        }

        // This is a copy of the test data in System.Text.Json.Tests.JsonEncodedTextTests.JsonEncodedTextStringsCustom
        public static IEnumerable<object[]> JsonEncodedTextStringsCustom
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { "age", "\\u0061\\u0067\\u0065" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\"\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0032\\u0032\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9>>>>>\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\\u003E\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003e\\u003e\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\u00EA\u00EA\u00EA\u00EA\u00EA" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStringsCustom))]
        public static void CustomEncoderAllowLatin1Supplement(string message, string expectedMessage)
        {
            // Latin-1 Supplement block starts from U+0080 and ends at U+00FF
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.Latin1Supplement);

            var options = new JsonSerializerOptions();
            options.Encoder = encoder;

            var obj = new TestClassForEncoding();
            obj.MyString = message;

            string baselineJson = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(expectedMessage, baselineJson);

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(expectedMessage, json);

            obj = JsonSerializer.Deserialize<TestClassForEncoding>(json);
            Assert.Equal(obj.MyString, message);
        }

        public static IEnumerable<object[]> JsonEncodedTextStringsCustomAll
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "a\u0467\u0466a", "a\u0467\u0466a" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStringsCustomAll))]
        public static void JsonEncodedTextStringsCustomAllowAll(string message, string expectedMessage)
        {
            // Allow all unicode values (except forbidden characters which we don't have in test data here)
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            var options = new JsonSerializerOptions();
            options.Encoder = encoder;

            var obj = new TestClassForEncoding();
            obj.MyString = message;

            string baselineJson = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(expectedMessage, baselineJson);

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(expectedMessage, json);

            obj = JsonSerializer.Deserialize<TestClassForEncoding>(json);
            Assert.Equal(obj.MyString, message);
        }

        [Fact]
        public static void Options_GetConverterForObjectJsonElement_GivesCorrectConverter()
        {
            GenericObjectOrJsonElementConverterTestHelper<object>("ObjectConverter", new object(), "[3]", true);
            JsonElement element = JsonDocument.Parse("[3]").RootElement;
            GenericObjectOrJsonElementConverterTestHelper<JsonElement>("JsonElementConverter", element, "[3]", false);
        }

        private static void GenericObjectOrJsonElementConverterTestHelper<T>(string converterName, object objectValue, string stringValue, bool throws)
        {
            var options = new JsonSerializerOptions();

            JsonConverter<T> converter = (JsonConverter<T>)options.GetConverter(typeof(T));
            Assert.Equal(converterName, converter.GetType().Name);
            
            ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes(stringValue);
            Utf8JsonReader reader = new Utf8JsonReader(data);
            reader.Read();
            T readValue = converter.Read(ref reader, typeof(T), null);

            if (readValue is JsonElement element)
            {
                Assert.Equal(JsonValueKind.Array, element.ValueKind);
                JsonElement.ArrayEnumerator iterator = element.EnumerateArray();
                Assert.True(iterator.MoveNext());
                Assert.Equal(3, iterator.Current.GetInt32());
            }
            else
            {
                Assert.True(false, "Must be JsonElement");
            }

            using (var stream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(stream))
            {
                if (throws)
                {
                    Assert.Throws<InvalidOperationException>(() => converter.Write(writer, (T)objectValue, options));
                    Assert.Throws<InvalidOperationException>(() => converter.Write(writer, (T)objectValue, null));
                }
                else
                {
                    converter.Write(writer, (T)objectValue, options);
                    writer.Flush();
                    Assert.Equal(stringValue, Encoding.UTF8.GetString(stream.ToArray()));

                    writer.Reset(stream);
                    converter.Write(writer, (T)objectValue, null); // Test with null option
                    writer.Flush();
                    Assert.Equal(stringValue + stringValue, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }

        [Fact]
        public static void Options_GetConverter_GivesCorrectDefaultConverterAndReadWriteSuccess()
        {
            var options = new JsonSerializerOptions();
            GenericConverterTestHelper<bool>("BooleanConverter", true, "true", options);
            GenericConverterTestHelper<byte>("ByteConverter", (byte)128, "128", options);
            GenericConverterTestHelper<char>("CharConverter", 'A', "\"A\"", options);
            GenericConverterTestHelper<double>("DoubleConverter", 15.1d, "15.1", options);
            GenericConverterTestHelper<SampleEnum>("EnumConverter`1", SampleEnum.Two, "2", options);
            GenericConverterTestHelper<short>("Int16Converter", (short)5, "5", options);
            GenericConverterTestHelper<int>("Int32Converter", -100, "-100", options);
            GenericConverterTestHelper<long>("Int64Converter", (long)11111, "11111", options);
            GenericConverterTestHelper<sbyte>("SByteConverter", (sbyte)-121, "-121", options);
            GenericConverterTestHelper<float>("SingleConverter", 14.5f, "14.5", options);
            GenericConverterTestHelper<string>("StringConverter", "Hello", "\"Hello\"", options);
            GenericConverterTestHelper<ushort>("UInt16Converter", (ushort)1206, "1206", options);
            GenericConverterTestHelper<uint>("UInt32Converter", (uint)3333, "3333", options);
            GenericConverterTestHelper<ulong>("UInt64Converter", (ulong)44444, "44444", options);
            GenericConverterTestHelper<decimal>("DecimalConverter", 3.3m, "3.3", options);
            GenericConverterTestHelper<byte[]>("ByteArrayConverter", new byte[] { 1, 2, 3, 4 }, "\"AQIDBA==\"", options);
            GenericConverterTestHelper<DateTime>("DateTimeConverter", new DateTime(2018, 12, 3), "\"2018-12-03T00:00:00\"", options);
            GenericConverterTestHelper<DateTimeOffset>("DateTimeOffsetConverter", new DateTimeOffset(new DateTime(2018, 12, 3, 00, 00, 00, DateTimeKind.Utc)), "\"2018-12-03T00:00:00+00:00\"", options);
            Guid testGuid = new Guid();
            GenericConverterTestHelper<Guid>("GuidConverter", testGuid, $"\"{testGuid.ToString()}\"", options);
            GenericConverterTestHelper<KeyValuePair<string, string>>("KeyValuePairConverter`2", new KeyValuePair<string, string>("key", "value"), @"{""Key"":""key"",""Value"":""value""}", options);
            GenericConverterTestHelper<Uri>("UriConverter", new Uri("http://test.com"), "\"http://test.com\"", options);
            
        }

        [Fact]
        public static void Options_GetConverter_GivesCorrectCustomConverterAndReadWriteSuccess()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new CustomConverterTests.LongArrayConverter());
            GenericConverterTestHelper<long[]>("LongArrayConverter", new long[] { 1, 2, 3, 4 }, "\"1,2,3,4\"", options);
        }

        private static void GenericConverterTestHelper<T>(string converterName, object objectValue, string stringValue, JsonSerializerOptions options)
        {
            JsonConverter<T> converter = (JsonConverter<T>)options.GetConverter(typeof(T));

            Assert.True(converter.CanConvert(typeof(T)));
            Assert.Equal(converterName, converter.GetType().Name);

            ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes(stringValue);
            Utf8JsonReader reader = new Utf8JsonReader(data);
            reader.Read();

            T valueRead = converter.Read(ref reader, typeof(T), null); // Test with null option.
            Assert.Equal(objectValue, valueRead);

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                valueRead = converter.Read(ref reader, typeof(T), options);  // Test with given option if reader position haven't advanced.
                Assert.Equal(objectValue, valueRead);
            }

            using (var stream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, (T)objectValue, options);
                writer.Flush();
                Assert.Equal(stringValue, Encoding.UTF8.GetString(stream.ToArray()));

                writer.Reset(stream);
                converter.Write(writer, (T)objectValue, null); // Test with null option.
                writer.Flush();
                Assert.Equal(stringValue + stringValue, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void CopyConstructorTest_OriginalMutable()
        {
            JsonSerializerOptions options = CreateOptionsInstance();
            var newOptions = new JsonSerializerOptions(options);
            VerifyOptionsEqual(options, newOptions);

            // No exception is thrown on mutating the new options instance because it is "unlocked".
            newOptions.ReferenceHandling = ReferenceHandling.Preserve;
            newOptions.Converters.Add(new JsonStringEnumConverter());
            newOptions.Converters.Add(new JsonStringEnumConverter());

            // Changes to new options don't affect old options.
            Assert.Equal(ReferenceHandling.Default, options.ReferenceHandling);
            Assert.Equal(2, options.Converters.Count);
            Assert.Equal(4, newOptions.Converters.Count);

            // Changes to old options don't affect new options.
            options.DefaultBufferSize = 2;
            options.Converters.Add(new ConverterForInt32());

            Assert.Equal(20, newOptions.DefaultBufferSize);
            Assert.Equal(3, options.Converters.Count);
            Assert.Equal(4, newOptions.Converters.Count);
        }

        [Fact]
        public static void CopyConstructorTest_OriginalLocked()
        {
            JsonSerializerOptions options = CreateOptionsInstance();

            // Perform serialization with options, after which it will be locked.
            JsonSerializer.Serialize("1", options);
            Assert.Throws<InvalidOperationException>(() => options.ReferenceHandling = ReferenceHandling.Preserve);

            var newOptions = new JsonSerializerOptions(options);
            VerifyOptionsEqual(options, newOptions);

            // No exception is thrown on mutating the new options instance because it is "unlocked".
            newOptions.ReferenceHandling = ReferenceHandling.Preserve;
        }

        [Fact]
        public static void CopyConstructorTest_MaxDepth()
        {
            static void RunTest(int maxDepth, int effectiveMaxDepth)
            {
                var options = new JsonSerializerOptions { MaxDepth = maxDepth };
                var newOptions = new JsonSerializerOptions(options);

                Assert.Equal(maxDepth, options.MaxDepth);
                Assert.Equal(maxDepth, newOptions.MaxDepth);

                // Test for default effective max depth in exception message.
                var myList = new List<object>();
                myList.Add(myList);

                string effectiveMaxDepthAsStr = effectiveMaxDepth.ToString();

                JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(myList, options));
                Assert.Contains(effectiveMaxDepthAsStr, ex.ToString());

                ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(myList, newOptions));
                Assert.Contains(effectiveMaxDepthAsStr, ex.ToString());
            }

            // Zero max depth
            RunTest(0, 64);

            // Specified max depth
            RunTest(25, 25);
        }

        [Fact]
        public static void CopyConstructorTest_CopiesAllPublicProperties()
        {
            JsonSerializerOptions options = GetFullyPopulatedOptionsInstance();
            var newOptions = new JsonSerializerOptions(options);
            VerifyOptionsEqual_WithReflection(options, newOptions);
        }

        [Fact]
        public static void CopyConstructorTest_NullInput()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new JsonSerializerOptions(null));
            Assert.Contains("options", ex.ToString());
        }

        private static JsonSerializerOptions CreateOptionsInstance()
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultBufferSize = 20,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Default,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                MaxDepth = 32,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = new SimpleSnakeCasePolicy(),
                ReadCommentHandling = JsonCommentHandling.Disallow,
                ReferenceHandling = ReferenceHandling.Default,
                WriteIndented = true,
            };

            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new ConverterForInt32());

            return options;
        }

        private static void VerifyOptionsEqual(JsonSerializerOptions options, JsonSerializerOptions newOptions)
        {
            Assert.Equal(options.AllowTrailingCommas, newOptions.AllowTrailingCommas);
            Assert.Equal(options.DefaultBufferSize, newOptions.DefaultBufferSize);
            Assert.Equal(options.IgnoreNullValues, newOptions.IgnoreNullValues);
            Assert.Equal(options.IgnoreReadOnlyProperties, newOptions.IgnoreReadOnlyProperties);
            Assert.Equal(options.MaxDepth, newOptions.MaxDepth);
            Assert.Equal(options.PropertyNameCaseInsensitive, newOptions.PropertyNameCaseInsensitive);
            Assert.Equal(options.ReadCommentHandling, newOptions.ReadCommentHandling);
            Assert.Equal(options.WriteIndented, newOptions.WriteIndented);

            Assert.Same(options.DictionaryKeyPolicy, newOptions.DictionaryKeyPolicy);
            Assert.Same(options.Encoder, newOptions.Encoder);
            Assert.Same(options.PropertyNamingPolicy, newOptions.PropertyNamingPolicy);

            Assert.Equal(options.Converters.Count, newOptions.Converters.Count);
            for (int i = 0; i < options.Converters.Count; i++)
            {
                Assert.Same(options.Converters[i], newOptions.Converters[i]);
            }
        }

        private static JsonSerializerOptions GetFullyPopulatedOptionsInstance()
        {
            var options = new JsonSerializerOptions();

            foreach (PropertyInfo property in typeof(JsonSerializerOptions).GetProperties())
            {
                Type propertyType = property.PropertyType;

                if (propertyType == typeof(bool))
                {
                    property.SetValue(options, true);
                }
                if (propertyType == typeof(int))
                {
                    property.SetValue(options, 32);
                }
                else if (propertyType == typeof(IList<JsonConverter>))
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                    options.Converters.Add(new ConverterForInt32());
                }
                else if (propertyType == typeof(JavaScriptEncoder))
                {
                    options.Encoder = JavaScriptEncoder.Default;
                }
                else if (propertyType == typeof(JsonNamingPolicy))
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.DictionaryKeyPolicy = new SimpleSnakeCasePolicy();
                }
                else if (propertyType == typeof(ReferenceHandling))
                {
                    options.ReferenceHandling = ReferenceHandling.Preserve;
                }
                else if (propertyType.IsValueType)
                {
                    options.ReadCommentHandling = JsonCommentHandling.Disallow;
                }
                else
                {
                    // An exception thrown from here means this test should be updated
                    // to reflect any newly added properties on JsonSerializerOptions.
                    property.SetValue(options, Activator.CreateInstance(propertyType));
                }
            }

            return options;
        }

        private static void VerifyOptionsEqual_WithReflection(JsonSerializerOptions options, JsonSerializerOptions newOptions)
        {
            foreach (PropertyInfo property in typeof(JsonSerializerOptions).GetProperties())
            {
                Type propertyType = property.PropertyType;

                if (propertyType == typeof(bool))
                {
                    Assert.True((bool)property.GetValue(options));
                    Assert.Equal((bool)property.GetValue(options), (bool)property.GetValue(newOptions));
                }
                else if (propertyType == typeof(int))
                {
                    Assert.Equal(32, (int)property.GetValue(options));
                    Assert.Equal((int)property.GetValue(options), (int)property.GetValue(newOptions));
                }
                else if (typeof(IEnumerable).IsAssignableFrom(propertyType))
                {
                    var list1 = (IList<JsonConverter>)property.GetValue(options);
                    var list2 = (IList<JsonConverter>)property.GetValue(newOptions);

                    Assert.Equal(list1.Count, list2.Count);
                    for (int i = 0; i < list1.Count; i++)
                    {
                        Assert.Same(list1[i], list2[i]);
                    }
                }
                else if (propertyType.IsValueType)
                {
                    if (property.Name == "ReadCommentHandling")
                    {
                        Assert.Equal(options.ReadCommentHandling, newOptions.ReadCommentHandling);
                    }
                    else
                    {
                        Assert.True(false, $"Public option was added to JsonSerializerOptions but not copied in the copy ctor: {property.Name}");
                    }
                }
                else
                {
                    Assert.Same(property.GetValue(options), property.GetValue(newOptions));
                }
            }
        }
    }
}
