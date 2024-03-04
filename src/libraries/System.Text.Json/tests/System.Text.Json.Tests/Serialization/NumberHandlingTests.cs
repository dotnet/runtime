// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class NumberHandlingTestsDynamic : NumberHandlingTests
    {
        public NumberHandlingTestsDynamic() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public class NumberHandlingTests_AsyncStreamOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_AsyncStreamOverload() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class NumberHandlingTests_SyncStreamOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_SyncStreamOverload() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class NumberHandlingTests_SyncOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_SyncOverload() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public class NumberHandlingTests_Document : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public class NumberHandlingTests_Element : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public class NumberHandlingTests_Node : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public class NumberHandlingTests_Pipe : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Pipe() : base(JsonSerializerWrapper.PipeSerializer) { }
    }

    public abstract class NumberHandlingTests_OverloadSpecific
    {
        private JsonSerializerWrapper Serializer { get; }

        public NumberHandlingTests_OverloadSpecific(JsonSerializerWrapper deserializer)
        {
            Serializer = deserializer;
        }

        [Theory]
        [MemberData(nameof(NumberHandling_ForPropsReadAfter_DeserializingCtorParams_TestData))]
        public async Task NumberHandling_ForPropsReadAfter_DeserializingCtorParams(string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            Result result = await Serializer.DeserializeWrapper<Result>(json, options);
            JsonTestHelper.AssertJsonEqual(json, await Serializer.SerializeWrapper(result, options));
        }

        public static IEnumerable<object[]> NumberHandling_ForPropsReadAfter_DeserializingCtorParams_TestData()
        {
            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""userPlayCount"": ""123"",
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""userPlayCount"": ""123"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""userPlayCount"": ""123"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        },
                        ""userPlayCount"": ""123""
                    }
                }"
            };
        }

        public class Result
        {
            public Album Album { get; init; }
        }

        public class Album
        {
            public Album(string name, string artist)
            {
                Name = name;
                Artist = artist;
            }

            public string Name { get; init; }
            public string Artist { get; init; }

            public long? userPlayCount { get; init; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }
    }
}
