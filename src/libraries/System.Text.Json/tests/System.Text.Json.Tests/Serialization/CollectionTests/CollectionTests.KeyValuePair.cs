// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CollectionTests
    {
        [Fact]
        public static void ReadSimpleKeyValuePairFail()
        {
            // Invalid form: no Value
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": 123}"));

            // Invalid form: extra property
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Value"": 123, ""Value2"": 456}"));

            // Invalid form: does not contain both Key and Value properties
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Val"": 123"));
        }

        [Fact]
        public static void ReadListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = JsonSerializer.Deserialize<List<KeyValuePair<string, int>>>(@"[{""Key"": ""123"", ""Value"": 123},{""Key"": ""456"", ""Value"": 456}]");

            Assert.Equal(2, input.Count);
            Assert.Equal("123", input[0].Key);
            Assert.Equal(123, input[0].Value);
            Assert.Equal("456", input[1].Key);
            Assert.Equal(456, input[1].Value);
        }

        [Fact]
        public static void ReadKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = JsonSerializer.Deserialize<KeyValuePair<string, List<int>>>(@"{""Key"":""Key"", ""Value"":[1, 2, 3]}");

            Assert.Equal("Key", input.Key);
            Assert.Equal(3, input.Value.Count);
            Assert.Equal(1, input.Value[0]);
            Assert.Equal(2, input.Value[1]);
            Assert.Equal(3, input.Value[2]);
        }

        [Theory]
        [InlineData(@"{""Key"":""Key"", ""Value"":{""Key"":1, ""Value"":2}}")]
        [InlineData(@"{""Key"":""Key"", ""Value"":{""Value"":2, ""Key"":1}}")]
        [InlineData(@"{""Value"":{""Key"":1, ""Value"":2}, ""Key"":""Key""}")]
        [InlineData(@"{""Value"":{""Value"":2, ""Key"":1}, ""Key"":""Key""}")]
        public static void ReadKeyValuePairOfKeyValuePair(string json)
        {
            KeyValuePair<string, KeyValuePair<int, int>> input = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<int, int>>>(json);

            Assert.Equal("Key", input.Key);
            Assert.Equal(1, input.Value.Key);
            Assert.Equal(2, input.Value.Value);
        }

        [Fact]
        public static void ReadKeyValuePairWithNullValues()
        {
            {
                KeyValuePair<string, string> kvp = JsonSerializer.Deserialize<KeyValuePair<string, string>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, object> kvp = JsonSerializer.Deserialize<KeyValuePair<string, object>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, SimpleClassWithKeyValuePairs> kvp = JsonSerializer.Deserialize<KeyValuePair<string, SimpleClassWithKeyValuePairs>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, string>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, string>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, object>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, object>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> kvp = JsonSerializer.Deserialize<KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }
        }

        [Fact]
        public static void ReadClassWithNullKeyValuePairValues()
        {
            string json =
                    @"{" +
                        @"""KvpWStrVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWObjVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWClassVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":null" +
                        @"}," +
                        @"""KvpWStrKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}," +
                        @"""KvpWObjKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}," +
                        @"""KvpWClassKvpVal"":{" +
                            @"""Key"":""key""," +
                            @"""Value"":{" +
                                @"""Key"":""key""," +
                                @"""Value"":null" +
                            @"}" +
                        @"}" +
                    @"}";
            SimpleClassWithKeyValuePairs obj = JsonSerializer.Deserialize<SimpleClassWithKeyValuePairs>(json);

            Assert.Equal("key", obj.KvpWStrVal.Key);
            Assert.Equal("key", obj.KvpWObjVal.Key);
            Assert.Equal("key", obj.KvpWClassVal.Key);
            Assert.Equal("key", obj.KvpWStrKvpVal.Key);
            Assert.Equal("key", obj.KvpWObjKvpVal.Key);
            Assert.Equal("key", obj.KvpWClassKvpVal.Key);
            Assert.Equal("key", obj.KvpWStrKvpVal.Value.Key);
            Assert.Equal("key", obj.KvpWObjKvpVal.Value.Key);
            Assert.Equal("key", obj.KvpWClassKvpVal.Value.Key);

            Assert.Null(obj.KvpWStrVal.Value);
            Assert.Null(obj.KvpWObjVal.Value);
            Assert.Null(obj.KvpWClassVal.Value);
            Assert.Null(obj.KvpWStrKvpVal.Value.Value);
            Assert.Null(obj.KvpWObjKvpVal.Value.Value);
            Assert.Null(obj.KvpWClassKvpVal.Value.Value);
        }

        [Fact]
        public static void Kvp_NullKeyIsFine()
        {
            KeyValuePair<string, string> kvp = JsonSerializer.Deserialize<KeyValuePair<string, string>>(@"{""Key"":null,""Value"":null}");
            Assert.Null(kvp.Key);
            Assert.Null(kvp.Value);
        }

        [Fact]
        public static void WritePrimitiveKeyValuePair()
        {
            KeyValuePair<string, int> input = new KeyValuePair<string, int>("Key", 123);

            string json = JsonSerializer.Serialize(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":123}", json);
        }

        [Fact]
        public static void WriteListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("123", 123),
                new KeyValuePair<string, int>("456", 456)
            };

            string json = JsonSerializer.Serialize(input);
            Assert.Equal(@"[{""Key"":""123"",""Value"":123},{""Key"":""456"",""Value"":456}]", json);
        }

        [Fact]
        public static void WriteKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = new KeyValuePair<string, List<int>>("Key", new List<int> { 1, 2, 3 });

            string json = JsonSerializer.Serialize(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":[1,2,3]}", json);
        }

        [Fact]
        public static void WriteKeyValuePairOfKeyValuePair()
        {
            KeyValuePair<string, KeyValuePair<string, int>> input = new KeyValuePair<string, KeyValuePair<string, int>>(
                "Key", new KeyValuePair<string, int>("Key", 1));

            string json = JsonSerializer.Serialize(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":{""Key"":""Key"",""Value"":1}}", json);
        }

        [Fact]
        public static void WriteKeyValuePairWithNullValues()
        {
            {
                KeyValuePair<string, string> kvp = new KeyValuePair<string, string>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", JsonSerializer.Serialize(kvp));
            }

            {
                KeyValuePair<string, object> kvp = new KeyValuePair<string, object>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", JsonSerializer.Serialize(kvp));
            }

            {
                KeyValuePair<string, SimpleClassWithKeyValuePairs> kvp = new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", JsonSerializer.Serialize(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, string>> kvp = new KeyValuePair<string, KeyValuePair<string, string>>("key", new KeyValuePair<string, string>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", JsonSerializer.Serialize(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, object>> kvp = new KeyValuePair<string, KeyValuePair<string, object>>("key", new KeyValuePair<string, object>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", JsonSerializer.Serialize(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> kvp = new KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>("key", new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", JsonSerializer.Serialize(kvp));
            }
        }

        [Fact]
        public static void WriteClassWithNullKeyValuePairValues_NullWrittenAsEmptyObject()
        {
            var value = new SimpleClassWithKeyValuePairs()
            {
                KvpWStrVal = new KeyValuePair<string, string>("key", null),
                KvpWObjVal = new KeyValuePair<string, object>("key", null),
                KvpWClassVal = new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null),
                KvpWStrKvpVal = new KeyValuePair<string, KeyValuePair<string, string>>("key", new KeyValuePair<string, string>("key", null)),
                KvpWObjKvpVal = new KeyValuePair<string, KeyValuePair<string, object>>("key", new KeyValuePair<string, object>("key", null)),
                KvpWClassKvpVal = new KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>("key", new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null)),
            };

            string result = JsonSerializer.Serialize(value);

            // Roundtrip to ensure serialize was correct.
            value = JsonSerializer.Deserialize<SimpleClassWithKeyValuePairs>(result);
            Assert.Equal("key", value.KvpWStrVal.Key);
            Assert.Equal("key", value.KvpWObjVal.Key);
            Assert.Equal("key", value.KvpWClassVal.Key);
            Assert.Equal("key", value.KvpWStrKvpVal.Key);
            Assert.Equal("key", value.KvpWObjKvpVal.Key);
            Assert.Equal("key", value.KvpWClassKvpVal.Key);
            Assert.Equal("key", value.KvpWStrKvpVal.Value.Key);
            Assert.Equal("key", value.KvpWObjKvpVal.Value.Key);
            Assert.Equal("key", value.KvpWClassKvpVal.Value.Key);

            Assert.Null(value.KvpWStrVal.Value);
            Assert.Null(value.KvpWObjVal.Value);
            Assert.Null(value.KvpWClassVal.Value);
            Assert.Null(value.KvpWStrKvpVal.Value.Value);
            Assert.Null(value.KvpWObjKvpVal.Value.Value);
            Assert.Null(value.KvpWClassKvpVal.Value.Value);
        }

        [Fact]
        public static void HonorNamingPolicy()
        {
            var kvp = new KeyValuePair<string, int>("Hello, World!", 1);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy()
            };

            string serialized = JsonSerializer.Serialize(kvp, options);
            // We know serializer writes the key first.
            Assert.Equal(@"{""_Key"":""Hello, World!"",""_Value"":1}", serialized);

            kvp = JsonSerializer.Deserialize<KeyValuePair<string, int>>(serialized, options);
            Assert.Equal("Hello, World!", kvp.Key);
            Assert.Equal(1, kvp.Value);
        }

        [Fact]
        public static void HonorNamingPolicy_CaseInsensitive()
        {
            const string json = @"{""key"":""Hello, World!"",""value"":1}";

            // Baseline - with case-sensitive matching, the payload doesn't have mapping properties.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(json));

            // Test - with case-insensitivity on, we have property matches.
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            KeyValuePair<string, int> kvp = JsonSerializer.Deserialize<KeyValuePair<string, int>>(json, options);
            Assert.Equal("Hello, World!", kvp.Key);
            Assert.Equal(1, kvp.Value);
        }

        [Fact]
        public static void HonorCLRProperties()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy() // Key -> _Key, Value -> _Value
            };

            // Although the policy won't produce these strings, the serializer successfully parses the properties.
            // "Key" and "Value" are special cased to accomodate content serialized with previous
            // versions of the serializer (.NET Core 3.x/System.Text.Json 4.7.x).
            string json = @"{""Key"":""Hello, World!"",""Value"":1}";
            KeyValuePair<string, int> kvp = JsonSerializer.Deserialize<KeyValuePair<string, int>>(json, options);
            Assert.Equal("Hello, World!", kvp.Key);
            Assert.Equal(1, kvp.Value);

            // "Key" and "Value" matching is case sensitive.
            json = @"{""key"":""Hello, World!"",""value"":1}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(json, options));

            // "Key" and "Value" matching is case sensitive, even when case insensitivity is on.
            // Case sensitivity only applies to the result of converting the CLR property names
            // (Key -> _Key, Value -> _Value) with the naming policy.
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy(),
                PropertyNameCaseInsensitive = true
            };

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<string, int>>(json, options));
        }

        private class LeadingUnderscorePolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => "_" + name;
        }

        [Fact]
        public static void HonorCustomEncoder()
        {
            var kvp = new KeyValuePair<int, int>(1, 2);

            JsonNamingPolicy namingPolicy = new TrailingAngleBracketPolicy();

            // Baseline - properties serialized with default encoder if none specified.
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = namingPolicy,
            };

            Assert.Equal(@"{""Key\u003C"":1,""Value\u003C"":2}", JsonSerializer.Serialize(kvp, options));

            // Test - serializer honors custom encoder.
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = namingPolicy,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            Assert.Equal(@"{""Key<"":1,""Value<"":2}", JsonSerializer.Serialize(kvp, options));
        }

        private class TrailingAngleBracketPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name + "<";
        }

        [Theory]
        [InlineData(typeof(KeyNameNullPolicy), "Key")]
        [InlineData(typeof(ValueNameNullPolicy), "Value")]
        public static void InvalidPropertyNameFail(Type policyType, string offendingProperty)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = (JsonNamingPolicy)Activator.CreateInstance(policyType)
            };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<KeyValuePair<string, string>>("", options));
            string exAsStr = ex.ToString();
            Assert.Contains(offendingProperty, exAsStr);

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new KeyValuePair<string, string>("", ""), options));
        }

        private class KeyNameNullPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name == "Key" ? null : name;
        }

        private class ValueNameNullPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name == "Value" ? null : name;
        }

        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("[")]
        [InlineData("}")]
        [InlineData("{")]
        [InlineData("{}")]
        [InlineData("{Key")]
        [InlineData("{0")]
        [InlineData(@"{""Random"":")]
        [InlineData(@"{""Value"":1}")]
        [InlineData(@"{null:1}")]
        [InlineData(@"{""Value"":1,2")]
        [InlineData(@"{""Value"":1,""Random"":")]
        [InlineData(@"{""Key"":1,""Key"":1}")]
        [InlineData(@"{null:1,""Key"":1}")]
        [InlineData(@"{""Key"":1,""Key"":2}")]
        [InlineData(@"{""Value"":1,""Value"":1}")]
        [InlineData(@"{""Value"":1,null:1}")]
        [InlineData(@"{""Value"":1,""Value"":2}")]
        public static void InvalidJsonFail(string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<int, int>>(json));
        }

        [Theory]
        [InlineData(@"{""Key"":""1"",""Value"":2}", "$.Key")]
        [InlineData(@"{""Key"":1,""Value"":""2""}", "$.Value")]
        [InlineData(@"{""key"":1,""Value"":2}", "$.key")]
        [InlineData(@"{""Key"":1,""value"":2}", "$.value")]
        [InlineData(@"{""Extra"":3,""Key"":1,""Value"":2}", "$.Extra")]
        [InlineData(@"{""Key"":1,""Extra"":3,""Value"":2}", "$.Extra")]
        [InlineData(@"{""Key"":1,""Value"":2,""Extra"":3}", "$.Extra")]
        public static void JsonPathIsAccurate(string json, string expectedPath)
        {
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<int, int>>(json));
            Assert.Contains(expectedPath, ex.ToString());

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<int, int>>(json));
            Assert.Contains(expectedPath, ex.ToString());
        }

        [Theory]
        [InlineData(@"{""kEy"":""1"",""vAlUe"":2}", "$.kEy")]
        [InlineData(@"{""kEy"":1,""vAlUe"":""2""}", "$.vAlUe")]
        public static void JsonPathIsAccurate_CaseInsensitive(string json, string expectedPath)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<int, int>>(json, options));
            Assert.Contains(expectedPath, ex.ToString());
        }

        [Theory]
        [InlineData(@"{""_Key"":""1"",""_Value"":2}", "$._Key")]
        [InlineData(@"{""_Key"":1,""_Value"":""2""}", "$._Value")]
        public static void JsonPathIsAccurate_PropertyNamingPolicy(string json, string expectedPath)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = new LeadingUnderscorePolicy() };
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KeyValuePair<int, int>>(json, options));
            Assert.Contains(expectedPath, ex.ToString());
        }
    }
}
