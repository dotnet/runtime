// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Node
{
    /// <summary>
    ///   Represents a mutable JSON object.
    /// </summary>
    [DebuggerDisplay("JsonObject[{Dictionary.Count}]")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed partial class JsonObject : JsonNode
    {
        private JsonElement? _jsonElement;
        private IDictionary<string, JsonNode?>? _dictionary;
        private string? _lastKey;
        private JsonNode? _lastValue;

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
        public JsonObject(IEnumerable<KeyValuePair<string, JsonNode?>> properties, JsonNodeOptions? options = null)
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
            if (propertyName == _lastKey)
            {
                // Optimize for repeating sections in code:
                // obj.Foo.Bar.One
                // obj.Foo.Bar.Two
                jsonNode = _lastValue;
                return true;
            }

            bool rc = Dictionary.TryGetValue(propertyName, out jsonNode);
            _lastKey = propertyName;
            _lastValue = jsonNode;
            return rc;
        }

        /// <inheritdoc/>
        public override void WriteTo(Utf8JsonWriter writer, JsonSerializerOptions? options = null)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (_jsonElement.HasValue)
            {
                _jsonElement.Value.WriteTo(writer);
            }
            else
            {
                options ??= JsonSerializerOptions.s_defaultOptions;

                writer.WriteStartObject();

                foreach (KeyValuePair<string, JsonNode?> kvp in Dictionary)
                {
                    writer.WritePropertyName(kvp.Key);
                    JsonNodeConverter.Instance.Write(writer, kvp.Value!, options);
                }

                writer.WriteEndObject();
            }
        }

        internal JsonObject(JsonElement element, JsonNodeOptions? options = null) : base(options)
        {
            Debug.Assert(element.ValueKind == JsonValueKind.Object);
            _jsonElement = element;
        }

        internal IDictionary<string, JsonNode?> Dictionary
        {
            get
            {
                CreateNodes();
                Debug.Assert(_dictionary != null);
                return _dictionary;
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
                bool found = false;

                foreach (KeyValuePair<string, JsonNode?> kvp in Dictionary)
                {
                    if (kvp.Value == child)
                    {
                        string propertyName = kvp.Key;
                        if (propertyName.IndexOfAny(ReadStack.SpecialCharacters) != -1)
                        {
                            path.Add($"['{propertyName}']");
                        }
                        else
                        {
                            path.Add($".{propertyName}");
                        }

                        found = true;
                        break;
                    }
                }

                Debug.Assert(found);
            }

            if (Parent != null)
            {
                Parent.GetPath(path, this);
            }
        }

        internal void SetItem(string propertyName, JsonNode? value)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // Do a Remove+Add instead of SetItem to unparent existing value (if any).
            Remove(propertyName);
            Add(propertyName, value);
        }

        private void CreateNodes()
        {
            if (_dictionary == null)
            {
                bool caseInsensitive = Options?.PropertyNameCaseInsensitive == true;

                var dictionary = new Dictionary<string, JsonNode?>(
                    caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                if (_jsonElement != null)
                {
                    JsonElement jElement = _jsonElement.Value;
                    foreach (JsonProperty property in jElement.EnumerateObject())
                    {
                        JsonNode? node = JsonNodeConverter.Create(property.Value, Options);
                        if (node != null)
                        {
                            node.Parent = this;
                        }

                        dictionary.Add(property.Name, node);
                    }

                    // Clear since no longer needed.
                    _jsonElement = null;
                }

                _dictionary = dictionary;
            }
        }

        [ExcludeFromCodeCoverage] // Justification = "Design-time"
        private class DebugView
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private JsonObject _node;

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
                    DebugViewProperty[] properties = new DebugViewProperty[_node.Dictionary.Count];

                    int i = 0;
                    foreach (KeyValuePair<string, JsonNode?> property in _node.Dictionary)
                    {
                        properties[i].Value = property.Value;
                        properties[i].PropertyName = property.Key;
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
                            return $"{PropertyName} = JsonObject[{jsonObject.Dictionary.Count}]";
                        }

                        JsonArray jsonArray = (JsonArray)Value;
                        return $"{PropertyName} = JsonArray[{jsonArray.List.Count}]";
                    }
                }

            }
        }
    }
}
