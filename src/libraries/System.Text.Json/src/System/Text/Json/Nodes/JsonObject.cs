// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Nodes
{
    /// <summary>
    ///   Represents a mutable JSON object.
    /// </summary>
    /// <remarks>
    /// It's safe to perform multiple concurrent read operations on a <see cref="JsonObject"/>,
    /// but issues can occur if the collection is modified while it's being read.
    /// </remarks>
    [DebuggerDisplay("JsonObject[{Count}]")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed partial class JsonObject : JsonNode
    {
        private JsonElement? _jsonElement;

        internal override JsonElement? UnderlyingElement => _jsonElement;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObject"/> class that is empty.
        /// </summary>
        /// <param name="options">Options to control the behavior.</param>
        public JsonObject(JsonNodeOptions? options = null) : base(options) { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObject"/> class that contains the specified <paramref name="properties"/>.
        /// </summary>
        /// <param name="properties">The properties to be added.</param>
        /// <param name="options">Options to control the behavior.</param>
        public JsonObject(IEnumerable<KeyValuePair<string, JsonNode?>> properties, JsonNodeOptions? options = null) : this(options)
        {
            int capacity = properties is ICollection<KeyValuePair<string, JsonNode?>> propertiesCollection ? propertiesCollection.Count : 0;
            OrderedDictionary<string, JsonNode?> dictionary = CreateDictionary(options, capacity);

            foreach (KeyValuePair<string, JsonNode?> node in properties)
            {
                dictionary.Add(node.Key, node.Value);
                node.Value?.AssignParent(this);
            }

            _dictionary = dictionary;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObject"/> class that contains properties from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <returns>
        ///   The new instance of the <see cref="JsonObject"/> class that contains properties from the specified <see cref="JsonElement"/>.
        /// </returns>
        /// <param name="element">The <see cref="JsonElement"/>.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>A <see cref="JsonObject"/>.</returns>
        public static JsonObject? Create(JsonElement element, JsonNodeOptions? options = null)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Object => new JsonObject(element, options),
                _ => throw new InvalidOperationException(SR.Format(SR.NodeElementWrongType, nameof(JsonValueKind.Object)))
            };
        }

        internal JsonObject(JsonElement element, JsonNodeOptions? options = null) : this(options)
        {
            Debug.Assert(element.ValueKind == JsonValueKind.Object);
            _jsonElement = element;
        }

        /// <summary>
        /// Gets or creates the underlying dictionary containing the properties of the object.
        /// </summary>
        private OrderedDictionary<string, JsonNode?> Dictionary => _dictionary ?? InitializeDictionary();

        private protected override JsonNode? GetItem(int index) => GetAt(index).Value;
        private protected override void SetItem(int index, JsonNode? value) => SetAt(index, value);

        internal override JsonNode DeepCloneCore()
        {
            GetUnderlyingRepresentation(out OrderedDictionary<string, JsonNode?>? dictionary, out JsonElement? jsonElement);

            if (dictionary is null)
            {
                return jsonElement.HasValue
                    ? new JsonObject(jsonElement.Value.Clone(), Options)
                    : new JsonObject(Options);
            }

            var jObject = new JsonObject(Options)
            {
                _dictionary = CreateDictionary(Options, Count)
            };

            foreach (KeyValuePair<string, JsonNode?> item in dictionary)
            {
                jObject.Add(item.Key, item.Value?.DeepCloneCore());
            }

            return jObject;
        }

        internal string GetPropertyName(JsonNode? node)
        {
            KeyValuePair<string, JsonNode?>? item = FindValue(node);
            return item.HasValue ? item.Value.Key : string.Empty;
        }

        /// <summary>
        ///   Returns the value of a property with the specified name.
        /// </summary>
        /// <param name="propertyName">The name of the property to return.</param>
        /// <param name="jsonNode">The JSON value of the property with the specified name.</param>
        /// <returns>
        ///   <see langword="true"/> if a property with the specified name was found; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetPropertyValue(string propertyName, out JsonNode? jsonNode)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            return Dictionary.TryGetValue(propertyName, out jsonNode);
        }

        /// <inheritdoc/>
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            GetUnderlyingRepresentation(out OrderedDictionary<string, JsonNode?>? dictionary, out JsonElement? jsonElement);

            if (dictionary is null && jsonElement.HasValue)
            {
                // Write the element without converting to nodes.
                jsonElement.Value.WriteTo(writer);
            }
            else
            {
                writer.WriteStartObject();

                foreach (KeyValuePair<string, JsonNode?> entry in Dictionary)
                {
                    writer.WritePropertyName(entry.Key);

                    if (entry.Value is null)
                    {
                        writer.WriteNullValue();
                    }
                    else
                    {
                        entry.Value.WriteTo(writer, options);
                    }
                }

                writer.WriteEndObject();
            }
        }

        private protected override JsonValueKind GetValueKindCore() => JsonValueKind.Object;

        internal override bool DeepEqualsCore(JsonNode node)
        {
            switch (node)
            {
                case JsonArray:
                    return false;
                case JsonValue value:
                    // JsonValue instances have special comparison semantics, dispatch to their implementation.
                    return value.DeepEqualsCore(this);
                case JsonObject jsonObject:
                    OrderedDictionary<string, JsonNode?> currentDict = Dictionary;
                    OrderedDictionary<string, JsonNode?> otherDict = jsonObject.Dictionary;

                    if (currentDict.Count != otherDict.Count)
                    {
                        return false;
                    }

                    foreach (KeyValuePair<string, JsonNode?> item in currentDict)
                    {
                        otherDict.TryGetValue(item.Key, out JsonNode? jsonNode);

                        if (!DeepEquals(item.Value, jsonNode))
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

        internal JsonNode? GetItem(string propertyName)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            if (TryGetPropertyValue(propertyName, out JsonNode? value))
            {
                return value;
            }

            // Return null for missing properties.
            return null;
        }

        internal override void GetPath(ref ValueStringBuilder path, JsonNode? child)
        {
            Parent?.GetPath(ref path, this);

            if (child != null)
            {
                string propertyName = FindValue(child)!.Value.Key;
                if (propertyName.AsSpan().ContainsSpecialCharacters())
                {
                    path.Append("['");
                    path.Append(propertyName);
                    path.Append("']");
                }
                else
                {
                    path.Append('.');
                    path.Append(propertyName);
                }
            }
        }

        internal void SetItem(string propertyName, JsonNode? value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            OrderedDictionary<string, JsonNode?> dict = Dictionary;

            if (dict.TryGetValue(propertyName, out JsonNode? replacedValue))
            {
                if (ReferenceEquals(value, replacedValue))
                {
                    return;
                }

                DetachParent(replacedValue);
            }

            dict[propertyName] = value;
            value?.AssignParent(this);
        }

        private void DetachParent(JsonNode? item)
        {
            Debug.Assert(_dictionary != null, "Cannot have detachable nodes without a materialized dictionary.");

            if (item != null)
            {
                item.Parent = null;
            }
        }

        private KeyValuePair<string, JsonNode?>? FindValue(JsonNode? value)
        {
            foreach (KeyValuePair<string, JsonNode?> item in Dictionary)
            {
                if (ReferenceEquals(item.Value, value))
                {
                    return item;
                }
            }

            return null;
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        private sealed class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly JsonObject _node;

            public DebugView(JsonObject node)
            {
                _node = node;
            }

            public string Json => _node.ToJsonString();
            public string Path => _node.GetPath();

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            private DebugViewProperty[] Items
            {
                get
                {
                    DebugViewProperty[] properties = new DebugViewProperty[_node.Count];

                    int i = 0;
                    foreach (KeyValuePair<string, JsonNode?> item in _node)
                    {
                        properties[i].PropertyName = item.Key;
                        properties[i].Value = item.Value;
                        i++;
                    }

                    return properties;
                }
            }

            [DebuggerDisplay("{Display,nq}")]
            private struct DebugViewProperty
            {
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public JsonNode? Value;

                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                public string PropertyName;

                [DebuggerBrowsable(DebuggerBrowsableState.Never)]
                public string Display
                {
                    get
                    {
                        if (Value == null)
                        {
                            return $"{PropertyName} = null";
                        }

                        if (Value is JsonValue)
                        {
                            return $"{PropertyName} = {Value.ToJsonString()}";
                        }

                        if (Value is JsonObject jsonObject)
                        {
                            return $"{PropertyName} = JsonObject[{jsonObject.Count}]";
                        }

                        JsonArray jsonArray = (JsonArray)Value;
                        return $"{PropertyName} = JsonArray[{jsonArray.Count}]";
                    }
                }

            }
        }
    }
}
