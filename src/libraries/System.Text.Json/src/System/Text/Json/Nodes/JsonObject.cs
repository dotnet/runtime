// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

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
            foreach (KeyValuePair<string, JsonNode?> node in properties)
            {
                Add(node.Key, node.Value);
            }
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
            if (element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                return new JsonObject(element, options);
            }

            throw new InvalidOperationException(SR.Format(SR.NodeElementWrongType, nameof(JsonValueKind.Object)));
        }

        internal JsonObject(JsonElement element, JsonNodeOptions? options = null) : this(options)
        {
            Debug.Assert(element.ValueKind == JsonValueKind.Object);
            _jsonElement = element;
        }

        internal override JsonNode InternalDeepClone()
        {
            if (_jsonElement.HasValue)
            {
                return new JsonObject(_jsonElement!.Value.Clone(), Options);
            }

            if (_dictionary is not null)
            {
                bool caseInsensitive = Options.HasValue ? Options.Value.PropertyNameCaseInsensitive : false;
                var jObject = new JsonObject(Options)
                {
                    _dictionary = new JsonPropertyDictionary<JsonNode?>(caseInsensitive, _dictionary.Count)
                };

                foreach (KeyValuePair<string, JsonNode?> item in _dictionary)
                {
                    if (item.Value is not null)
                    {
                        jObject.Add(item.Key, item.Value.DeepClone());
                    }
                    else
                    {
                        jObject.Add(item.Key, null);
                    }
                }
                return jObject;
            }

            return new JsonObject(Options);
        }

        internal string GetPropertyName(JsonNode? node)
        {
            InitializeIfRequired();
            Debug.Assert(_dictionary != null);
            KeyValuePair<string, JsonNode?>? item = _dictionary.FindValue(node);
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
        public bool TryGetPropertyValue(string propertyName, out JsonNode? jsonNode) =>
            ((IDictionary<string, JsonNode?>)this).TryGetValue(propertyName, out jsonNode);

        /// <inheritdoc/>
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            if (_jsonElement.HasValue)
            {
                // Write the element without converting to nodes.
                _jsonElement.Value.WriteTo(writer);
            }
            else
            {
                options ??= s_defaultOptions;

                writer.WriteStartObject();

                foreach (KeyValuePair<string, JsonNode?> item in this)
                {
                    writer.WritePropertyName(item.Key);
                    JsonNodeConverter.Instance.Write(writer, item.Value, options);
                }

                writer.WriteEndObject();
            }
        }

        internal override bool DeepEquals(JsonNode? node)
        {
            switch (node)
            {
                case null or JsonArray:
                    return false;
                case JsonValue value:
                    // JsonValueTrimmable/NonTrimmable can hold the object type so calling this method to continue the deep comparision.
                    return value.DeepEquals(this);
                case JsonObject jsonObject:
                    InitializeIfRequired();
                    jsonObject.InitializeIfRequired();
                    Debug.Assert(_dictionary is not null);
                    Debug.Assert(jsonObject._dictionary is not null);

                    if (_dictionary.Count != jsonObject._dictionary.Count)
                    {
                        return false;
                    }

                    foreach (KeyValuePair<string, JsonNode?> item in this)
                    {
                        JsonNode? jsonNode = jsonObject._dictionary[item.Key];

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
            if (TryGetPropertyValue(propertyName, out JsonNode? value))
            {
                return value;
            }

            // Return null for missing properties.
            return null;
        }

        internal override void GetPath(List<string> path, JsonNode? child)
        {
            if (child != null)
            {
                InitializeIfRequired();
                Debug.Assert(_dictionary != null);
                string propertyName = _dictionary.FindValue(child)!.Value.Key;
                if (propertyName.AsSpan().ContainsSpecialCharacters())
                {
                    path.Add($"['{propertyName}']");
                }
                else
                {
                    path.Add($".{propertyName}");
                }
            }

            Parent?.GetPath(path, this);
        }

        internal void SetItem(string propertyName, JsonNode? value)
        {
            InitializeIfRequired();
            Debug.Assert(_dictionary != null);
            JsonNode? existing = _dictionary.SetValue(propertyName, value, () => value?.AssignParent(this));
            DetachParent(existing);
        }

        private void DetachParent(JsonNode? item)
        {
            InitializeIfRequired();
            Debug.Assert(_dictionary != null);

            if (item != null)
            {
                item.Parent = null;
            }
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
