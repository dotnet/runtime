// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed class WasmReproTest
    {
        [Fact]
        public static void TestFailingOnWasmOnly()
        {
            // Failing test adapted from ReferenceHandlerTests.ReadTestClassesWithExtensionOption

            var options = new JsonSerializerOptions 
            {
                ReferenceHandler = ReferenceHandler.Preserve 
            };

            string json = GetLargeJsonObject();

            // First five calls succeed without issue
            for (int i = 0; i < 5; i++)
            {
                object value = JsonSerializer.Deserialize<object>(json, options);
                Assert.IsType<JsonElement>(value);
            }

            // Fails on wasm release:
            // System.ObjectDisposedException : Cannot access a disposed object.
            // Object name: 'JsonDocument'.
            //    at System.Text.Json.ThrowHelper.ThrowObjectDisposedException_JsonDocument()
            //    at System.Text.Json.JsonDocument.CheckNotDisposed()
            //    at System.Text.Json.JsonDocument.TextEquals(Int32 index, ReadOnlySpan`1 otherUtf8Text, Boolean isPropertyName, Boolean shouldUnescape)
            //    at System.Text.Json.JsonElement.TextEqualsHelper(ReadOnlySpan`1 utf8Text, Boolean isPropertyName, Boolean shouldUnescape)
            //    at System.Text.Json.JsonProperty.EscapedNameEquals(ReadOnlySpan`1 utf8Text)
            //    at System.Text.Json.JsonSerializer.TryHandleReferenceFromJsonElement(Utf8JsonReader& reader, ReadStack& state, JsonElement element, Object& referenceValue)
            //    at System.Text.Json.Serialization.Converters.ObjectConverter.OnTryRead(Utf8JsonReader& reader, Type typeToConvert, JsonSerializerOptions options, Rea
            JsonSerializer.Deserialize<object>(json, options);
        }

        private static string GetLargeJsonObject()
        {
            var sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < 200; i++)
            {
                sb.Append($@"""Key{i}"":{i},");
            }

            sb.Length -= 1;
            sb.Append('}');
            return sb.ToString();
        }
    }
}
