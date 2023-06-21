// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task DictionaryKeyFilter_MetadataNamesDeserialize()
        {
            var options = new JsonSerializerOptions
            {
                DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames // Ignore metadata keys starting with $, such as `$schema`.
            };

            const string JsonString = @"[{""$metadata"":-1,""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4,""$metadata"":5}]";

            // Without filter, deserialize all keys.
            Dictionary<string, int>[] obj = await Serializer.DeserializeWrapper<Dictionary<string, int>[]>(JsonString);

            Assert.Equal(2, obj.Length);

            Assert.Equal(3, obj[0].Count);
            Assert.Equal(-1, obj[0]["$metadata"]);
            Assert.Equal(1, obj[0]["Key1"]);
            Assert.Equal(2, obj[0]["Key2"]);

            Assert.Equal(3, obj[1].Count);
            Assert.Equal(3, obj[1]["Key1"]);
            Assert.Equal(4, obj[1]["Key2"]);
            Assert.Equal(5, obj[1]["$metadata"]);

            // With key filter, ignore metadata keys.
            obj = await Serializer.DeserializeWrapper<Dictionary<string, int>[]>(JsonString, options);

            Assert.Equal(2, obj.Length);

            Assert.Equal(2, obj[0].Count);
            Assert.Equal(1, obj[0]["Key1"]);
            Assert.Equal(2, obj[0]["Key2"]);

            Assert.Equal(2, obj[1].Count);
            Assert.Equal(3, obj[1]["Key1"]);
            Assert.Equal(4, obj[1]["Key2"]);
        }

        [Fact]
        public async Task DictionaryKeyFilter_IgnoreOnSerialize()
        {
            var options = new JsonSerializerOptions()
            {
                DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames // Ignore metadata keys starting with $, such as `$schema`.
            };

            Dictionary<string, int>[] obj = new Dictionary<string, int>[]
            {
                new Dictionary<string, int>() { { "$metadata", -1 }, { "Key1", 1 }, { "Key2", 2 } },
                new Dictionary<string, int>() { { "Key1", 3 }, { "Key2", 4 }, { "$metadata", 5 } },
            };

            const string Json = @"[{""$metadata"":1,""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4,""$metadata"":5}]";

            // Without key filter, serialize keys as they are.
            string json = await Serializer.SerializeWrapper<object>(obj);
            Assert.Equal(Json, json);

            // Ensure we ignore key filter and serialize keys as they are.
            json = await Serializer.SerializeWrapper<object>(obj, options);
            Assert.Equal(Json, json);
        }

        [Fact]
        public async Task DictionaryKeyFilter_IgnoreForExtensionData()
        {
            var options = new JsonSerializerOptions
            {
                DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames // Ignore metadata keys starting with $, such as `$schema`.
            };

            const string JsonString = @"{""$metadata"":-1,""Key1"":1, ""Key2"":2,""$metadata3"":3,""$metadataUndefined"":," +
                @"""$metadataObject"":{},""$metadataArray"":[,{},[],""$metadataString"",4,true,false,null]," +
                @"""$metadataString"":""$metadataString"",""$metadataTrue"":true,""$metadataFalse"":false,""$metadataNull"":null}";

            // Ensure we ignore dictionary key filter for extension data and deserialize all keys.
            ClassWithExtensionData myClass = await Serializer.DeserializeWrapper<ClassWithExtensionData>(JsonString, options);
            Assert.Equal(-1, myClass.ExtensionData["$metadata"].GetInt32());
            Assert.Equal(1, myClass.ExtensionData["Key1"].GetInt32());
            Assert.Equal(2, myClass.ExtensionData["Key2"].GetInt32());
            Assert.Equal(3, myClass.ExtensionData["$metadata3"].GetInt32());
            Assert.Equal(JsonValueKind.Undefined, myClass.ExtensionData["$metadataUndefined"].ValueKind);
            Assert.Equal(JsonValueKind.Object, myClass.ExtensionData["$metadataObject"].ValueKind);
            Assert.Equal(JsonValueKind.Array, myClass.ExtensionData["$metadataArray"].ValueKind);
            Assert.Equal(8, myClass.ExtensionData["$metadataArray"].GetArrayLength());
            Assert.Equal(JsonValueKind.String, myClass.ExtensionData["$metadataString"].ValueKind);
            Assert.Equal("$metadataString", myClass.ExtensionData["$metadataString"].GetString());
            Assert.Equal(JsonValueKind.Number, myClass.ExtensionData["$metadataNumber"].ValueKind);
            Assert.Equal(4, myClass.ExtensionData["$metadataString"].GetInt32());
            Assert.Equal(JsonValueKind.True, myClass.ExtensionData["$metadataTrue"].ValueKind);
            Assert.Equal(JsonValueKind.False, myClass.ExtensionData["$metadataFalse"].ValueKind);
            Assert.Equal(JsonValueKind.Null, myClass.ExtensionData["$metadataNull"].ValueKind);
        }

        //[Fact]
        //public async Task DictionaryKeyFilter_MetadataNames_Any_Values()
        //{
        //    var options = new JsonSerializerOptions
        //    {
        //        DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames // Ignore metadata keys starting with $, such as `$schema`.
        //    };

        //    const string JsonString = @"{""$metadata"":-1,""Key1"":1, ""Key2"":2,""$metadata3"":3," +
        //        @"""$metadataArray"":[{},[]],""$metadataFalse"":false,""$metadataNull"":null," +
        //        @"""$metadataObject"":{},""$metadataString"":""$metadataString""," +
        //        @"""$metadataTrue"":true,""$metadataUndefined"":}";

        //    Assert.ThrowsAsync<InvalidCastException>(() => Serializer.SerializeWrapper(dict, options));

        //    // Without key filter, deserialize throw exception.
        //    Dictionary<string, int>[] obj = await Serializer.DeserializeWrapper<Dictionary<string, int>[]>(JsonString);

        //    // With key filter, ignore metadata keys.
        //    obj = await Serializer.DeserializeWrapper<Dictionary<string, int>[]>(JsonString, options);

        //    Assert.Equal(2, obj.Length);

        //    Assert.Equal(2, obj[0].Count);
        //    Assert.Equal(1, obj[0]["Key1"]);
        //    Assert.Equal(2, obj[0]["Key2"]);

        //    Assert.Equal(2, obj[1].Count);
        //    Assert.Equal(3, obj[1]["Key1"]);
        //    Assert.Equal(4, obj[1]["Key2"]);
        //}

        //        [Fact]
        //        public async Task CustomNameDeserialize()
        //        {
        //            var options = new JsonSerializerOptions
        //            {
        //                DictionaryKeyPolicy = new UppercaseNamingPolicy() // e.g. myint -> MYINT.
        //            };


        //            // Without key policy, deserialize keys as they are.
        //            Dictionary<string, int> obj = await Serializer.DeserializeWrapper<Dictionary<string, int>>(@"{""myint"":1}");
        //            Assert.Equal(1, obj["myint"]);

        //            // Ensure we ignore key policy and deserialize keys as they are.
        //            obj = await Serializer.DeserializeWrapper<Dictionary<string, int>>(@"{""myint"":1}", options);
        //            Assert.Equal(1, obj["myint"]);
        //        }

        //        [Fact]
        //#if BUILDING_SOURCE_GENERATOR_TESTS
        //        [ActiveIssue("Need extension data support.")]
        //#endif
        //        public async Task DeserializationWithJsonExtensionDataAttribute_IgoneDictionaryKeyPolicy()
        //        {
        //            var expectedJson = @"{""KeyInt"":1000,""KeyString"":""text"",""KeyBool"":true,""KeyObject"":{},""KeyList"":[],""KeyDictionary"":{}}";
        //            var obj = new ClassWithExtensionDataProperty();
        //            obj.Data = new Dictionary<string, object>()
        //            {
        //                { "KeyInt", 1000 },
        //                { "KeyString", "text" },
        //                { "KeyBool", true },
        //                { "KeyObject", new object() },
        //                { "KeyList", new List<string>() },
        //                { "KeyDictionary", new Dictionary<string, string>() }
        //            };
        //            string json = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions()
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            });
        //            Assert.Equal(expectedJson, json);
        //        }

        //        private class ClassWithExtensionDataProperty
        //        {
        //           [JsonExtensionData]
        //            public Dictionary<string, object> Data { get; set; }
        //        }

        //        [Fact]
        //        public async Task CamelCaseSerialize_ForTypedDictionary_ApplyDictionaryKeyPolicy()
        //        {
        //            const string JsonCamel = @"{""keyDict"":{""Name"":""text"",""Number"":1000,""isValid"":true,""Values"":[1,2,3]}}";
        //            var obj = new Dictionary<string, CustomClass>()
        //            {
        //                { "KeyDict", CreateCustomObject() }
        //            };
        //            var json = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions()
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            });

        //            Assert.Equal(JsonCamel, json);
        //        }

        //        public class CustomClass
        //        {
        //            public string Name { get; set; }
        //            public int Number { get; set; }
        //            public bool isValid { get; set; }
        //            public List<int> Values { get; set; }
        //        }

        //        private static CustomClass CreateCustomObject()
        //        {
        //            return new CustomClass { Name = "text", Number = 1000, isValid = true, Values = new List<int>() { 1, 2, 3 } };
        //        }

        //        [Fact]
        //        public async Task CamelCaseSerialize_ForNestedTypedDictionary_ApplyDictionaryKeyPolicy()
        //        {
        //            const string JsonCamel = @"{""keyDict"":{""nestedKeyDict"":{""Name"":""text"",""Number"":1000,""isValid"":true,""Values"":[1,2,3]}}}";
        //            var options = new JsonSerializerOptions
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            };
        //            var obj = new Dictionary<string, Dictionary<string, CustomClass>>(){
        //                { "KeyDict", new  Dictionary<string,CustomClass>()
        //                {{ "NestedKeyDict", CreateCustomObject() }}
        //            }};
        //            var json = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions()
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            });

        //            Assert.Equal(JsonCamel, json);
        //        }

        //        public class TestClassWithDictionary
        //        {
        //           public Dictionary<string, CustomClass> Data { get; set; }
        //		}

        //        [Fact]
        //        public async Task CamelCaseSerialize_ForClassWithDictionaryProperty_ApplyDictionaryKeyPolicy()
        //        {
        //            const string JsonCamel = @"{""Data"":{""keyObj"":{""Name"":""text"",""Number"":1000,""isValid"":true,""Values"":[1,2,3]}}}";
        //            var obj = new TestClassWithDictionary();
        //            obj.Data = new Dictionary<string, CustomClass> {
        //                {"KeyObj", CreateCustomObject() }
        //            };
        //            var json = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions()
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            });
        //            Assert.Equal(JsonCamel, json);
        //        }

        //        [Fact]
        //        public async Task CamelCaseSerialize_ForKeyValuePairWithDictionaryValue_ApplyDictionaryKeyPolicy()
        //        {
        //            const string JsonCamel = @"{""Key"":""KeyPair"",""Value"":{""keyDict"":{""Name"":""text"",""Number"":1000,""isValid"":true,""Values"":[1,2,3]}}}";
        //            var options = new JsonSerializerOptions
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            };
        //            var obj = new KeyValuePair<string, Dictionary<string, CustomClass>>
        //              ("KeyPair", new Dictionary<string, CustomClass> {
        //              {"KeyDict", CreateCustomObject() }
        //            });
        //            var json = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions()
        //            {
        //                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        //            });

        //            Assert.Equal(JsonCamel, json);
        //        }
    }
}
