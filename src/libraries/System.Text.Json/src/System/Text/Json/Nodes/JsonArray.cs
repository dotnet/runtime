// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;
using System.Threading;

namespace System.Text.Json.Nodes
{
    /// <summary>
    ///   Represents a mutable JSON array.
    /// </summary>
    /// <remarks>
    /// It is safe to perform multiple concurrent read operations on a <see cref="JsonArray"/>,
    /// but issues can occur if the collection is modified while it's being read.
    /// </remarks>
    [DebuggerDisplay("JsonArray[{List.Count}]")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed partial class JsonArray : JsonNode
    {
        private JsonElement? _jsonElement;
        private List<JsonNode?>? _list;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that is empty.
        /// </summary>
        /// <param name="options">Options to control the behavior.</param>
        public JsonArray(JsonNodeOptions? options = null) : base(options) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified params array.
        /// </summary>
        /// <param name="options">Options to control the behavior.</param>
        /// <param name="items">The items to add to the new <see cref="JsonArray"/>.</param>
        public JsonArray(JsonNodeOptions options, params JsonNode?[] items) : base(options)
        {
            InitializeFromArray(items);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified array.
        /// </summary>
        /// <param name="items">The items to add to the new <see cref="JsonArray"/>.</param>
        public JsonArray(params JsonNode?[] items) : base()
        {
            InitializeFromArray(items);
        }

        internal override JsonValueKind GetValueKindCore() => JsonValueKind.Array;

        internal override JsonNode DeepCloneCore()
        {
            GetUnderlyingRepresentation(out List<JsonNode?>? list, out JsonElement? jsonElement);

            if (list is null)
            {
                return jsonElement.HasValue
                    ? new JsonArray(jsonElement.Value.Clone(), Options)
                    : new JsonArray(Options);
            }

            var jsonArray = new JsonArray(Options)
            {
                _list = new List<JsonNode?>(list.Count)
            };

            for (int i = 0; i < list.Count; i++)
            {
                jsonArray.Add(list[i]?.DeepCloneCore());
            }

            return jsonArray;
        }

        internal override bool DeepEqualsCore(JsonNode? node)
        {
            switch (node)
            {
                case null or JsonObject:
                    return false;
                case JsonValue value:
                    // JsonValue instances have special comparison semantics, dispatch to their implementation.
                    return value.DeepEqualsCore(this);
                case JsonArray array:
                    List<JsonNode?> currentList = List;
                    List<JsonNode?> otherList = array.List;

                    if (currentList.Count != otherList.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < currentList.Count; i++)
                    {
                        if (!DeepEquals(currentList[i], otherList[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    Debug.Fail("Impossible case");
                    return false;
            }
        }

        internal int GetElementIndex(JsonNode? node)
        {
            return List.IndexOf(node);
        }

        /// <summary>
        /// Returns an enumerable that wraps calls to <see cref="JsonNode.GetValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the value to obtain from the <see cref="JsonValue"/>.</typeparam>
        /// <returns>An enumerable iterating over values of the array.</returns>
        public IEnumerable<T> GetValues<T>()
        {
            foreach (JsonNode? item in List)
            {
                yield return item is null ? (T)(object?)null! : item.GetValue<T>();
            }
        }

        private void InitializeFromArray(JsonNode?[] items)
        {
            var list = new List<JsonNode?>(items);

            for (int i = 0; i < items.Length; i++)
            {
                items[i]?.AssignParent(this);
            }

            _list = list;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonArray"/> class that contains items from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonArray"/> class that contains items from the specified <see cref="JsonElement"/>.
        /// </returns>
        /// <param name="element">The <see cref="JsonElement"/>.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="InvalidOperationException">
        ///   The <paramref name="element"/> is not a <see cref="JsonValueKind.Array"/>.
        /// </exception>
        public static JsonArray? Create(JsonElement element, JsonNodeOptions? options = null)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Array => new JsonArray(element, options),
                _ => throw new InvalidOperationException(SR.Format(SR.NodeElementWrongType, nameof(JsonValueKind.Array))),
            };
        }

        internal JsonArray(JsonElement element, JsonNodeOptions? options = null) : base(options)
        {
            Debug.Assert(element.ValueKind == JsonValueKind.Array);
            _jsonElement = element;
        }

        /// <summary>
        ///   Adds an object to the end of the <see cref="JsonArray"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to be added.</typeparam>
        /// <param name="value">
        ///   The object to be added to the end of the <see cref="JsonArray"/>.
        /// </param>
        [RequiresUnreferencedCode(JsonValue.CreateUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonValue.CreateDynamicCodeMessage)]
        public void Add<T>(T? value)
        {
            JsonNode? nodeToAdd = ConvertFromValue(value, Options);
            Add(nodeToAdd);
        }

        /// <summary>
        /// Gets or creates the underlying list containing the element nodes of the array.
        /// </summary>
        internal List<JsonNode?> List => _list is { } list ? list : InitializeList();

        internal JsonNode? GetItem(int index)
        {
            return List[index];
        }

        internal void SetItem(int index, JsonNode? value)
        {
            value?.AssignParent(this);
            DetachParent(List[index]);
            List[index] = value;
        }

        internal override void GetPath(ref ValueStringBuilder path, JsonNode? child)
        {
            Parent?.GetPath(ref path, this);

            if (child != null)
            {
                int index = List.IndexOf(child);
                Debug.Assert(index >= 0);

                path.Append('[');
#if NETCOREAPP
                Span<char> chars = stackalloc char[JsonConstants.MaximumFormatUInt32Length];
                bool formatted = ((uint)index).TryFormat(chars, out int charsWritten);
                Debug.Assert(formatted);
                path.Append(chars.Slice(0, charsWritten));
#else
                path.Append(index.ToString());
#endif
                path.Append(']');
            }
        }

        /// <inheritdoc/>
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            GetUnderlyingRepresentation(out List<JsonNode?>? list, out JsonElement? jsonElement);

            if (list is null && jsonElement.HasValue)
            {
                jsonElement.Value.WriteTo(writer);
            }
            else
            {
                writer.WriteStartArray();

                foreach (JsonNode? element in List)
                {
                    if (element is null)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        element.WriteTo(writer, options);
                    }
                }

                writer.WriteEndArray();
            }
        }

        private List<JsonNode?> InitializeList()
        {
            GetUnderlyingRepresentation(out List<JsonNode?>? list, out JsonElement? jsonElement);

            if (list is null)
            {
                if (jsonElement.HasValue)
                {
                    JsonElement jElement = jsonElement.Value;
                    Debug.Assert(jElement.ValueKind == JsonValueKind.Array);

                    list = new List<JsonNode?>(jElement.GetArrayLength());

                    foreach (JsonElement element in jElement.EnumerateArray())
                    {
                        JsonNode? node = JsonNodeConverter.Create(element, Options);
                        node?.AssignParent(this);
                        list.Add(node);
                    }
                }
                else
                {
                    list = new();
                }

                // Ensure _jsonElement is written to after _list
                _list = list;
                Interlocked.MemoryBarrier();
                _jsonElement = null;
            }

            return list;
        }

        /// <summary>
        /// Provides a coherent view of the underlying representation of the current node.
        /// The jsonElement value should be consumed if and only if the list value is null.
        /// </summary>
        private void GetUnderlyingRepresentation(out List<JsonNode?>? list, out JsonElement? jsonElement)
        {
            // Because JsonElement cannot be read atomically there might be torn reads,
            // however the order of read/write operations guarantees that that's only
            // possible if the value of _list is non-null.
            jsonElement = _jsonElement;
            Interlocked.MemoryBarrier();
            list = _list;
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        private sealed class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly JsonArray _node;

            public DebugView(JsonArray node)
            {
                _node = node;
            }

            public string Json => _node.ToJsonString();
            public string Path => _node.GetPath();

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private DebugViewItem[] Items
            {
                get
                {
                    DebugViewItem[] properties = new DebugViewItem[_node.List.Count];

                    for (int i = 0; i < _node.List.Count; i++)
                    {
                        properties[i].Value = _node.List[i];
                    }

                    return properties;
                }
            }

            [DebuggerDisplay("{Display,nq}")]
            private struct DebugViewItem
            {
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public JsonNode? Value;

                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                public string Display
                {
                    get
                    {
                        if (Value == null)
                        {
                            return $"null";
                        }

                        if (Value is JsonValue)
                        {
                            return Value.ToJsonString();
                        }

                        if (Value is JsonObject jsonObject)
                        {
                            return $"JsonObject[{jsonObject.Count}]";
                        }

                        JsonArray jsonArray = (JsonArray)Value;
                        return $"JsonArray[{jsonArray.List.Count}]";
                    }
                }
            }
        }
    }
}
