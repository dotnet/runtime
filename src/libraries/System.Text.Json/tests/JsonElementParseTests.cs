// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Collections.Generic;

namespace System.Text.Json.Tests
{
    public static class JsonElementParseTests
    {
        public static IEnumerable<object[]> ElementParseCases
        {
            get
            {
                yield return new object[] { "null", JsonValueKind.Null };
                yield return new object[] { "true", JsonValueKind.True };
                yield return new object[] { "false", JsonValueKind.False };
                yield return new object[] { "\"MyString\"", JsonValueKind.String };
                yield return new object[] { "{}", JsonValueKind.Object };
                yield return new object[] { "[]", JsonValueKind.Array };
            }
        }

        [Theory]
        [MemberData(nameof(ElementParseCases))]
        public static void ParseValue(string json, JsonValueKind kind)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            JsonElement? element = JsonElement.ParseValue(ref reader);
            Assert.Equal(kind, element!.Value.ValueKind);
        }

        [Theory]
        [MemberData(nameof(ElementParseCases))]
        public static void TryParseValue(string json, JsonValueKind kind)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            bool success = JsonElement.TryParseValue(ref reader, out JsonElement? element);
            Assert.True(success);
            Assert.Equal(kind, element!.Value.ValueKind);
        }

        public static IEnumerable<object[]> ElementParsePartialDataCases
        {
            get
            {
                yield return new object[] { "\"MyString"};
                yield return new object[] { "{" };
                yield return new object[] { "[" };
            }
        }

        [Theory]
        [MemberData(nameof(ElementParsePartialDataCases))]
        public static void ParseValuePartialDataFail(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            try
            {
                JsonElement.ParseValue(ref reader);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }
        }

        [Theory]
        [MemberData(nameof(ElementParsePartialDataCases))]
        public static void TryParseValuePartialDataFail(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            try
            {
                JsonElement.TryParseValue(ref reader, out JsonElement? element);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }
        }

        [Theory]
        [MemberData(nameof(ElementParsePartialDataCases))]
        public static void ParseValueOutOfData(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), isFinalBlock: false, new JsonReaderState());

            try
            {
                JsonElement.ParseValue(ref reader);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }
        }

        [Theory]
        [MemberData(nameof(ElementParsePartialDataCases))]
        public static void TryParseValueOutOfData(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), isFinalBlock: false, new JsonReaderState());
            Assert.False(JsonElement.TryParseValue(ref reader, out JsonElement? element));
        }

        public static IEnumerable<object[]> ElementParseInvalidDataCases
        {
            get
            {
                yield return new object[] { "nul" };
                yield return new object[] { "{]" };
            }
        }

        [Theory]
        [MemberData(nameof(ElementParseInvalidDataCases))]
        public static void ParseValueInvalidDataFail(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            try
            {
                JsonElement.ParseValue(ref reader);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }
        }

        [Theory]
        [MemberData(nameof(ElementParseInvalidDataCases))]
        public static void TryParseValueInvalidDataFail(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

            try
            {
                JsonElement.TryParseValue(ref reader, out JsonElement? element);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }
        }
    }
}
