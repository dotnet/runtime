// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullTests
    {
        [Fact]
        public static void ClassWithNullProperty()
        {
            TestClassWithNull obj = JsonSerializer.Deserialize<TestClassWithNull>(TestClassWithNull.s_json);
            obj.Verify();
        }

        [Fact]
        public static void RootObjectIsNull()
        {
            {
                TestClassWithNull obj = JsonSerializer.Deserialize<TestClassWithNull>("null");
                Assert.Null(obj);
            }

            {
                object obj = JsonSerializer.Deserialize<object>("null");
                Assert.Null(obj);
            }

            {
                string obj = JsonSerializer.Deserialize<string>("null");
                Assert.Null(obj);
            }

            {
                IEnumerable<int> obj = JsonSerializer.Deserialize<IEnumerable<int>>("null");
                Assert.Null(obj);
            }

            {
                Dictionary<string, object> obj = JsonSerializer.Deserialize<Dictionary<string, object>>("null");
                Assert.Null(obj);
            }

        }

        [Fact]
        public static void RootArrayIsNull()
        {
            {
                int[] obj = JsonSerializer.Deserialize<int[]>("null");
                Assert.Null(obj);
            }

            {
                object[] obj = JsonSerializer.Deserialize<object[]>("null");
                Assert.Null(obj);
            }

            {
                TestClassWithNull[] obj = JsonSerializer.Deserialize<TestClassWithNull[]>("null");
                Assert.Null(obj);
            }
        }

        [Fact]
        public static void DefaultIgnoreNullValuesOnRead()
        {
            TestClassWithInitializedProperties obj = JsonSerializer.Deserialize<TestClassWithInitializedProperties>(TestClassWithInitializedProperties.s_null_json);
            Assert.Null(obj.MyString);
            Assert.Null(obj.MyInt);
            Assert.Null(obj.MyDateTime);
            Assert.Null(obj.MyIntArray);
            Assert.Null(obj.MyIntList);
            Assert.Null(obj.MyNullableIntList);
            Assert.Null(obj.MyObjectList[0]);
            Assert.Null(obj.MyListList[0][0]);
            Assert.Null(obj.MyDictionaryList[0]["key"]);
            Assert.Null(obj.MyStringDictionary["key"]);
            Assert.Null(obj.MyNullableDateTimeDictionary["key"]);
            Assert.Null(obj.MyObjectDictionary["key"]);
            Assert.Null(obj.MyStringDictionaryDictionary["key"]["key"]);
            Assert.Null(obj.MyListDictionary["key"][0]);
            Assert.Null(obj.MyObjectDictionaryDictionary["key"]["key"]);
        }

        [Fact]
        public static void EnableIgnoreNullValuesOnRead()
        {
            var options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;

            TestClassWithInitializedProperties obj = JsonSerializer.Deserialize<TestClassWithInitializedProperties>(TestClassWithInitializedProperties.s_null_json, options);

            Assert.Equal("Hello", obj.MyString);
            Assert.Equal(1, obj.MyInt);
            Assert.Equal(new DateTime(1995, 4, 16), obj.MyDateTime);
            Assert.Equal(1, obj.MyIntArray[0]);
            Assert.Equal(1, obj.MyIntList[0]);
            Assert.Equal(1, obj.MyNullableIntList[0]);

            Assert.Null(obj.MyObjectList[0]);
            Assert.Null(obj.MyObjectList[0]);
            Assert.Null(obj.MyListList[0][0]);
            Assert.Null(obj.MyDictionaryList[0]["key"]);
            Assert.Null(obj.MyNullableDateTimeDictionary["key"]);
            Assert.Null(obj.MyStringDictionary["key"]);
            Assert.Null(obj.MyObjectDictionary["key"]);
            Assert.Null(obj.MyStringDictionaryDictionary["key"]["key"]);
            Assert.Null(obj.MyListDictionary["key"][0]);
            Assert.Null(obj.MyObjectDictionaryDictionary["key"]["key"]);
        }

        [Fact]
        public static void ParseNullArgumentFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<string>((string)null));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize("1", (Type)null));
        }

        [Fact]
        public static void NullLiteralObjectInput()
        {
            {
                string obj = JsonSerializer.Deserialize<string>("null");
                Assert.Null(obj);
            }

            {
                string obj = JsonSerializer.Deserialize<string>(@"""null""");
                Assert.Equal("null", obj);
            }
        }

        [Fact]
        public static void NullAcceptsLeadingAndTrailingTrivia()
        {
            {
                TestClassWithNull obj = JsonSerializer.Deserialize<TestClassWithNull>(" null");
                Assert.Null(obj);
            }

            {
                object obj = JsonSerializer.Deserialize<object>("null ");
                Assert.Null(obj);
            }

            {
                object obj = JsonSerializer.Deserialize<object>(" null\t");
                Assert.Null(obj);
            }
        }

        [Fact]
        public static void NullReadTestChar()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("null"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("\"\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>(""));   // Empty JSON is invalid
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("1234")); // Can't convert a JSON number to JSON string/char
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("\"stringTooLong\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("\"\u0059B\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("\"\uD800\uDC00\""));
            Assert.Equal('a', JsonSerializer.Deserialize<char>("\"a\""));
            Assert.Equal('Y', JsonSerializer.Deserialize<char>("\"\u0059\""));
        }

        [Fact]
        public static void ParseNullStringToStructShouldThrowJsonException()
        {
            string nullString = "null";
            byte[] nullStringAsBytes = Encoding.UTF8.GetBytes(nullString);

            Utf8JsonReader reader = new Utf8JsonReader(nullStringAsBytes);

            JsonTestHelper.AssertThrows<JsonException>(reader, (reader) => JsonSerializer.Deserialize<SimpleStruct>(ref reader));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleStruct>(nullStringAsBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleStruct>(nullString));

            // null can be assigned to nullable structs.
            Assert.Null(JsonSerializer.Deserialize<SimpleStruct?>(nullStringAsBytes));
            Assert.Null(JsonSerializer.Deserialize<SimpleStruct?>(nullString));
        }

        [Fact]
        public static async Task ParseNullStringShouldThrowJsonExceptionAsync()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("null")))
            { 
                await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.DeserializeAsync<SimpleStruct>(stream));

                // null can be assigned to nullable structs.
                stream.Position = 0;
                Assert.Null(await JsonSerializer.DeserializeAsync<SimpleStruct?>(stream));
            }
        }

        [Fact]
        public static void DeserializeDictionaryWithNullValues()
        {
            {
                Dictionary<string, string> dict = JsonSerializer.Deserialize<Dictionary<string, string>>(@"{""key"":null}");
                Assert.Null(dict["key"]);
            }

            {
                Dictionary<string, object> dict = JsonSerializer.Deserialize<Dictionary<string, object>>(@"{""key"":null}");
                Assert.Null(dict["key"]);
            }

            {
                Dictionary<string, Dictionary<string, string>> dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(@"{""key"":null}");
                Assert.Null(dict["key"]);
            }

            {
                Dictionary<string, Dictionary<string, object>> dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(@"{""key"":null}");
                Assert.Null(dict["key"]);
            }
        }

        [Fact]
        public static void InvalidRootOnRead()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<int[,]>("null"));

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };

            // We still throw when we have an unsupported root.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<int[,]>("null", options));
        }
    }
}
