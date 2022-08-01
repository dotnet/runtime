// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public virtual async Task ReadSimpleKeyValuePairPartialData()
        {
            KeyValuePair<string, int> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(@"{""Key"": ""123""}");
            Assert.Equal("123", kvp.Key);
            Assert.Equal(0, kvp.Value);

            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Value"": 123, ""Value2"": 456}");
            Assert.Equal("Key", kvp.Key);
            Assert.Equal(123, kvp.Value);

            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(@"{""Key"": ""Key"", ""Val"": 123}");
            Assert.Equal("Key", kvp.Key);
            Assert.Equal(0, kvp.Value);
        }

        [Fact]
        public async Task ReadListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = await Serializer.DeserializeWrapper<List<KeyValuePair<string, int>>>(@"[{""Key"": ""123"", ""Value"": 123},{""Key"": ""456"", ""Value"": 456}]");

            Assert.Equal(2, input.Count);
            Assert.Equal("123", input[0].Key);
            Assert.Equal(123, input[0].Value);
            Assert.Equal("456", input[1].Key);
            Assert.Equal(456, input[1].Value);
        }

        [Fact]
        public async Task ReadKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = await Serializer.DeserializeWrapper<KeyValuePair<string, List<int>>>(@"{""Key"":""Key"", ""Value"":[1, 2, 3]}");

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
        public async Task ReadKeyValuePairOfKeyValuePair(string json)
        {
            KeyValuePair<string, KeyValuePair<int, int>> input = await Serializer.DeserializeWrapper<KeyValuePair<string, KeyValuePair<int, int>>>(json);

            Assert.Equal("Key", input.Key);
            Assert.Equal(1, input.Value.Key);
            Assert.Equal(2, input.Value.Value);
        }

        [Fact]
        public async Task ReadKeyValuePairWithNullValues()
        {
            {
                KeyValuePair<string, string> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, string>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, object> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, object>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, SimpleClassWithKeyValuePairs> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, SimpleClassWithKeyValuePairs>>(@"{""Key"":""key"",""Value"":null}");
                Assert.Equal("key", kvp.Key);
                Assert.Null(kvp.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, string>> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, KeyValuePair<string, string>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, object>> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, KeyValuePair<string, object>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }

            {
                KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>>(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}");
                Assert.Equal("key", kvp.Key);
                Assert.Equal("key", kvp.Value.Key);
                Assert.Null(kvp.Value.Value);
            }
        }

        [Fact]
        public async Task ReadClassWithNullKeyValuePairValues()
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
            SimpleClassWithKeyValuePairs obj = await Serializer.DeserializeWrapper<SimpleClassWithKeyValuePairs>(json);

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
        public async Task Kvp_NullKeyIsFine()
        {
            KeyValuePair<string, string> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, string>>(@"{""Key"":null,""Value"":null}");
            Assert.Null(kvp.Key);
            Assert.Null(kvp.Value);
        }

        [Fact]
        public async Task WritePrimitiveKeyValuePair()
        {
            KeyValuePair<string, int> input = new KeyValuePair<string, int>("Key", 123);

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":123}", json);
        }

        [Fact]
        public async Task WriteListOfKeyValuePair()
        {
            List<KeyValuePair<string, int>> input = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("123", 123),
                new KeyValuePair<string, int>("456", 456)
            };

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal(@"[{""Key"":""123"",""Value"":123},{""Key"":""456"",""Value"":456}]", json);
        }

        [Fact]
        public async Task WriteKeyValuePairOfList()
        {
            KeyValuePair<string, List<int>> input = new KeyValuePair<string, List<int>>("Key", new List<int> { 1, 2, 3 });

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":[1,2,3]}", json);
        }

        [Fact]
        public async Task WriteKeyValuePairOfKeyValuePair()
        {
            KeyValuePair<string, KeyValuePair<string, int>> input = new KeyValuePair<string, KeyValuePair<string, int>>(
                "Key", new KeyValuePair<string, int>("Key", 1));

            string json = await Serializer.SerializeWrapper(input);
            Assert.Equal(@"{""Key"":""Key"",""Value"":{""Key"":""Key"",""Value"":1}}", json);
        }

        [Fact]
        public async Task WriteKeyValuePairWithNullValues()
        {
            {
                KeyValuePair<string, string> kvp = new KeyValuePair<string, string>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", await Serializer.SerializeWrapper(kvp));
            }

            {
                KeyValuePair<string, object> kvp = new KeyValuePair<string, object>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", await Serializer.SerializeWrapper(kvp));
            }

            {
                KeyValuePair<string, SimpleClassWithKeyValuePairs> kvp = new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null);
                Assert.Equal(@"{""Key"":""key"",""Value"":null}", await Serializer.SerializeWrapper(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, string>> kvp = new KeyValuePair<string, KeyValuePair<string, string>>("key", new KeyValuePair<string, string>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", await Serializer.SerializeWrapper(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, object>> kvp = new KeyValuePair<string, KeyValuePair<string, object>>("key", new KeyValuePair<string, object>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", await Serializer.SerializeWrapper(kvp));
            }

            {
                KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>> kvp = new KeyValuePair<string, KeyValuePair<string, SimpleClassWithKeyValuePairs>>("key", new KeyValuePair<string, SimpleClassWithKeyValuePairs>("key", null));
                Assert.Equal(@"{""Key"":""key"",""Value"":{""Key"":""key"",""Value"":null}}", await Serializer.SerializeWrapper(kvp));
            }
        }

        [Fact]
        public async Task WriteClassWithNullKeyValuePairValues_NullWrittenAsEmptyObject()
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

            string result = await Serializer.SerializeWrapper(value);

            // Roundtrip to ensure serialize was correct.
            value = await Serializer.DeserializeWrapper<SimpleClassWithKeyValuePairs>(result);
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
        public async Task HonorNamingPolicy()
        {
            var kvp = new KeyValuePair<string, int>("Hello, World!", 1);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy()
            };

            string serialized = await Serializer.SerializeWrapper(kvp, options);
            // We know serializer writes the key first.
            Assert.Equal(@"{""_Key"":""Hello, World!"",""_Value"":1}", serialized);

            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(serialized, options);
            Assert.Equal("Hello, World!", kvp.Key);
            Assert.Equal(1, kvp.Value);
        }

        [Fact]
        public virtual async Task HonorNamingPolicy_CaseInsensitive()
        {
            const string json = @"{""key"":""Hello, World!"",""value"":1}";

            // Baseline - with case-sensitive matching, the payload doesn't have mapping properties.
            KeyValuePair<string, int> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(json);
            Assert.Null(kvp.Key);
            Assert.Equal(0, kvp.Value);

            // Test - with case-insensitivity on, we have property matches.
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(json, options);
            Assert.Equal("Hello, World!", kvp.Key);
            Assert.Equal(1, kvp.Value);
        }

        [Fact]
        public virtual async Task HonorCLRProperties()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy() // Key -> _Key, Value -> _Value
            };

            // Since object converter (not KVP converter) is used, payloads not compliant with naming policy won't yield matches.
            string json = @"{""Key"":""Hello, World!"",""Value"":1}";
            KeyValuePair<string, int> kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(json, options);
            Assert.Null(kvp.Key);
            Assert.Equal(0, kvp.Value);

            // "Key" and "Value" matching is case sensitive.
            json = @"{""key"":""Hello, World!"",""value"":1}";
            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(json, options);
            Assert.Null(kvp.Key);
            Assert.Equal(0, kvp.Value);

            // "Key" and "Value" matching is case sensitive, even when case insensitivity is on.
            // Case sensitivity only applies to the result of converting the CLR property names
            // (Key -> _Key, Value -> _Value) with the naming policy.
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new LeadingUnderscorePolicy(),
                PropertyNameCaseInsensitive = true
            };

            kvp = await Serializer.DeserializeWrapper<KeyValuePair<string, int>>(json, options);
            Assert.Null(kvp.Key);
            Assert.Equal(0, kvp.Value);
        }

        public class LeadingUnderscorePolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => "_" + name;
        }

        [Fact]
        public async Task HonorCustomEncoder()
        {
            var kvp = new KeyValuePair<int, int>(1, 2);

            JsonNamingPolicy namingPolicy = new TrailingAngleBracketPolicy();

            // Baseline - properties serialized with default encoder if none specified.
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = namingPolicy,
            };

            Assert.Equal(@"{""Key\u003C"":1,""Value\u003C"":2}", await Serializer.SerializeWrapper(kvp, options));

            // Test - serializer honors custom encoder.
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = namingPolicy,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            Assert.Equal(@"{""Key<"":1,""Value<"":2}", await Serializer.SerializeWrapper(kvp, options));
        }

        private class TrailingAngleBracketPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name + "<";
        }

        [Theory]
        [InlineData(typeof(KeyNameNullPolicy), "Key")]
        [InlineData(typeof(ValueNameNullPolicy), "Value")]
        public async Task InvalidPropertyNameFail(Type policyType, string offendingProperty)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = (JsonNamingPolicy)Activator.CreateInstance(policyType)
            };

            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<string, string>>("{}", options));
            string exAsStr = ex.ToString();
            Assert.Contains(offendingProperty, exAsStr);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new KeyValuePair<string, string>("", ""), options));
        }

        public class KeyNameNullPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name == "Key" ? null : name;
        }

        public class ValueNameNullPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name == "Value" ? null : name;
        }

        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("[")]
        [InlineData("}")]
        [InlineData("{")]
        [InlineData("{Key")]
        [InlineData("{0")]
        [InlineData(@"{""Random"":")]
        [InlineData(@"{null:1}")]
        [InlineData(@"{""Value"":1,2")]
        [InlineData(@"{""Value"":1,""Random"":")]
        [InlineData(@"{null:1,""Key"":1}")]
        [InlineData(@"{""Value"":1,null:1}")]
        public async Task InvalidJsonFail(string json)
        {
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<int, int>>(json));
        }

        [Theory]
        [InlineData(@"{""Key"":""1"",""Value"":2}", "$.Key")]
        [InlineData(@"{""Key"":1,""Value"":""2""}", "$.Value")]
        public async Task JsonPathIsAccurate(string json, string expectedPath)
        {
            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<int, int>>(json));
            Assert.Contains(expectedPath, ex.ToString());

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<int, int>>(json));
            Assert.Contains(expectedPath, ex.ToString());
        }

        [Theory]
        [InlineData(@"{""kEy"":""1"",""vAlUe"":2}", "$.kEy")]
        [InlineData(@"{""kEy"":1,""vAlUe"":""2""}", "$.vAlUe")]
        public async Task JsonPathIsAccurate_CaseInsensitive(string json, string expectedPath)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<int, int>>(json, options));
            Assert.Contains(expectedPath, ex.ToString());
        }

        [Theory]
        [InlineData(@"{""_Key"":""1"",""_Value"":2}", "$._Key")]
        [InlineData(@"{""_Key"":1,""_Value"":""2""}", "$._Value")]
        public async Task JsonPathIsAccurate_PropertyNamingPolicy(string json, string expectedPath)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = new LeadingUnderscorePolicy() };
            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<KeyValuePair<int, int>>(json, options));
            Assert.Contains(expectedPath, ex.ToString());
        }

        [Theory]
        [InlineData(@"{}")]
        public virtual async Task EmptyJson_DeserializedTo_EmptyKeyValuePair(string json)
        {
            var result = await Serializer.DeserializeWrapper<KeyValuePair<string, string>>(json);
            Assert.IsType<KeyValuePair<string, string>>(result);
            Assert.Null(result.Key);
            Assert.Null(result.Value);
        }
    }
}
