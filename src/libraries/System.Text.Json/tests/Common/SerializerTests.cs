// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class abstracting the serialization System Under Test.
    /// </summary>
    public abstract class SerializerTests
    {
        protected SerializerTests(JsonSerializerWrapper serializerUnderTest)
        {
            Serializer = serializerUnderTest;
            StreamingSerializer = serializerUnderTest as StreamingJsonSerializerWrapper;
        }

        /// <summary>
        /// The serialization System Under Test to be targeted by deriving test suites.
        /// </summary>
        protected JsonSerializerWrapper Serializer { get; }

        /// <summary>
        /// For Systems Under Test that support streaming, exposes the relevant API surface.
        /// </summary>
        protected StreamingJsonSerializerWrapper? StreamingSerializer { get; }

        [Flags]
        protected enum SerializedValueContext
        {
            None = 0,
            RootValue = 1,
            ObjectProperty = 2,
            CollectionElement = 4,
            DictionaryValue = 8,
            JsonNode = 16,
            All = RootValue | ObjectProperty | CollectionElement | DictionaryValue | JsonNode
        }

        /// <summary>
        /// Tests serialization of a given value within the context of multiple types:
        /// root values, object properties, collection elements, dictionary values, etc.
        /// </summary>
        protected async Task TestMultiContextSerialization<TValue>(
            TValue value,
            string expectedJson,
            Type? expectedExceptionType = null,
            SerializedValueContext contexts = SerializedValueContext.All,
            JsonSerializerOptions? options = null)
        {
            Assert.True((contexts & SerializedValueContext.All) != SerializedValueContext.None);

            string actualJson;

            if (contexts.HasFlag(SerializedValueContext.RootValue))
            {
                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.SerializeWrapper(value, options));
                }
                else
                {
                    actualJson = await Serializer.SerializeWrapper(value, options);
                    JsonTestHelper.AssertJsonEqual(expectedJson, actualJson);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.ObjectProperty))
            {
                var poco = new GenericPoco<TValue> { Property = value };

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.SerializeWrapper(poco, options));
                }
                else
                {
                    string propertyName = options?.PropertyNamingPolicy is JsonNamingPolicy policy
                        ? policy.ConvertName(nameof(GenericPoco<TValue>.Property))
                        : nameof(GenericPoco<TValue>.Property);

                    actualJson = await Serializer.SerializeWrapper(poco, options);
                    JsonTestHelper.AssertJsonEqual($@"{{ ""{propertyName}"" : {expectedJson} }}", actualJson);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.CollectionElement))
            {
                var list = new List<TValue> { value };

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.SerializeWrapper(list, options));
                }
                else
                {
                    actualJson = await Serializer.SerializeWrapper(list, options);
                    JsonTestHelper.AssertJsonEqual($"[{expectedJson}]", actualJson);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.DictionaryValue))
            {
                const string key = "key";
                var dictionary = new Dictionary<string, TValue> { [key] = value };

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.SerializeWrapper(dictionary, options));
                }
                else
                {
                    string jsonKey = options?.DictionaryKeyPolicy is JsonNamingPolicy policy
                        ? policy.ConvertName(key)
                        : key;

                    actualJson = await Serializer.SerializeWrapper(dictionary, options);
                    JsonTestHelper.AssertJsonEqual($@"{{ ""{jsonKey}"" : {expectedJson} }}", actualJson);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.JsonNode))
            {
                const string key = "key";
                var jsonObject = new JsonObject { [key] = JsonValue.Create<TValue>(value) };

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.SerializeWrapper(jsonObject, options));
                }
                else
                {
                    actualJson = await Serializer.SerializeWrapper(jsonObject, options);
                    JsonTestHelper.AssertJsonEqual($@"{{ ""{key}"" : {expectedJson} }}", actualJson);
                }
            }
        }

        /// <summary>
        /// Tests serialization of a given list of values within the context of multiple types:
        /// root values, object properties, collection elements, dictionary values, etc.
        /// </summary>
        protected async Task TestMultiContextSerialization<TValue>(
            IEnumerable<(TValue Value, string ExpectedJson)> inputs,
            SerializedValueContext contexts = SerializedValueContext.All,
            JsonSerializerOptions? options = null)
        {
            inputs = inputs.ToList();
            string expectedJson = $"[{string.Join(", ", inputs.Select(x => x.ExpectedJson))}]";
            List<TValue> values = inputs.Select(x => x.Value).ToList();
            await TestMultiContextSerialization(
                values,
                expectedJson,
                expectedExceptionType: null,
                contexts,
                options);
        }

        /// <summary>
        /// Tests deserialization of a given value within the context of multiple types:
        /// root values, object properties, collection elements, dictionary values, etc.
        /// </summary>
        protected async Task TestMultiContextDeserialization<TValue>(
            string json,
            TValue? expectedValue = default,
            Type? expectedExceptionType = null,
            SerializedValueContext contexts = SerializedValueContext.All,
            JsonSerializerOptions? options = null,
            IEqualityComparer<TValue>? equalityComparer = null)
        {
            Assert.True((contexts & SerializedValueContext.All) != SerializedValueContext.None);

            string wrappedJson;
            equalityComparer ??= expectedValue is IEquatable<TValue>
                ? EqualityComparer<TValue>.Default
                : new JsonEqualityComparer<TValue>();

            if (contexts.HasFlag(SerializedValueContext.RootValue))
            {
                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.DeserializeWrapper<TValue>(json, options));
                }
                else
                {
                    TValue value = await Serializer.DeserializeWrapper<TValue>(json, options);
                    Assert.Equal(expectedValue, value, equalityComparer);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.ObjectProperty))
            {
                string propertyName =
                    options?.PropertyNamingPolicy is JsonNamingPolicy policy
                    ? policy.ConvertName(nameof(GenericPoco<TValue>.Property))
                    : nameof(GenericPoco<TValue>.Property);

                wrappedJson = $@"{{ ""{propertyName}"" : {json} }}";

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.DeserializeWrapper<GenericPoco<TValue>>(wrappedJson, options));
                }
                else
                {

                    GenericPoco<TValue> poco = await Serializer.DeserializeWrapper<GenericPoco<TValue>>(wrappedJson, options);
                    Assert.Equal(expectedValue, poco.Property, equalityComparer);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.CollectionElement))
            {
                wrappedJson = $@"[{json}]";

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.DeserializeWrapper<List<TValue>>(wrappedJson, options));
                }
                else
                {
                    List<TValue> list = await Serializer.DeserializeWrapper<List<TValue>>(wrappedJson, options);
                    Assert.Equal(1, list.Count);
                    Assert.Equal(expectedValue, list[0], equalityComparer);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.DictionaryValue))
            {
                const string key = "key";
                wrappedJson = $@"{{ ""{key}"" : {json} }}";

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType, () => Serializer.DeserializeWrapper<Dictionary<string, TValue>>(wrappedJson, options));
                }
                else
                {
                    Dictionary<string, TValue> dictionary = await Serializer.DeserializeWrapper<Dictionary<string, TValue>>(wrappedJson, options);
                    Assert.Equal(1, dictionary.Count);
                    Assert.True(dictionary.ContainsKey(key));
                    Assert.Equal(expectedValue, dictionary[key], equalityComparer);
                }
            }

            if (contexts.HasFlag(SerializedValueContext.JsonNode))
            {
                const string key = "key";
                wrappedJson = $@"{{ ""{key}"" : {json} }}";

                if (expectedExceptionType != null)
                {
                    await Assert.ThrowsAsync(expectedExceptionType,
                        async () =>
                        {
                            JsonNode jsonNode = await Serializer.DeserializeWrapper<JsonNode>(wrappedJson, options);
                            JsonSerializer.Deserialize<TValue>(jsonNode[key], options);
                        });
                }
                else
                {
                    JsonNode jsonNode = await Serializer.DeserializeWrapper<JsonNode>(wrappedJson, options);
                    TValue value = JsonSerializer.Deserialize<TValue>(jsonNode[key], options);
                    Assert.Equal(expectedValue, value, equalityComparer);
                }
            }
        }

        /// <summary>
        /// Tests deserialization of a given list of values within the context of multiple types:
        /// root values, object properties, collection elements, dictionary values, etc.
        /// </summary>
        protected async Task TestMultiContextDeserialization<TValue>(
            IEnumerable<(string Json, TValue ExpectedValue)> inputs,
            SerializedValueContext contexts = SerializedValueContext.All,
            JsonSerializerOptions? options = null,
            IEqualityComparer<TValue>? equalityComparer = null)
        {
            inputs = inputs.ToList();
            List<TValue> expectedValues = inputs.Select(x => x.ExpectedValue).ToList();
            string json = $"[{string.Join(",", inputs.Select(x => x.Json))}]";
            var listEqualityComparer = new ListAssertionEqualityComparer<TValue>(equalityComparer);
            await TestMultiContextDeserialization<List<TValue>>(json, expectedValues, expectedExceptionType: null, contexts, options, listEqualityComparer);
        }

        private class GenericPoco<T>
        {
            public T Property { get; set; }
        }

        private class JsonEqualityComparer<TValue> : IEqualityComparer<TValue>
        {
            public bool Equals(TValue? x, TValue? y) => JsonSerializer.Serialize(x) == JsonSerializer.Serialize(y);
            public int GetHashCode([DisallowNull] TValue obj) => JsonSerializer.Serialize(obj).GetHashCode();
        }

        private class ListAssertionEqualityComparer<TValue> : IEqualityComparer<IList<TValue>>
        {
            private readonly IEqualityComparer<TValue>? _elementComparer;

            public ListAssertionEqualityComparer(IEqualityComparer<TValue>? elementComparer)
            {
                _elementComparer = elementComparer ?? EqualityComparer<TValue>.Default;
            }

            public bool Equals(IList<TValue>? x, IList<TValue>? y)
            {
                Assert.Equal(x, y, _elementComparer);
                return true;
            }

            public int GetHashCode([DisallowNull] IList<TValue> obj) => throw new NotImplementedException();
        }
    }
}
